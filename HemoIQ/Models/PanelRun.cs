using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HemoIQ.Models
{
    /// <summary>
    /// Represents one testing run of a reagent panel against a specific specimen.
    /// A specimen may have multiple runs of the same panel (untreated, ficin-treated,
    /// DTT-treated, absorbed, etc.), each producing an independent reaction set.
    /// </summary>
    public class PanelRun
    {
        public int RunId { get; set; }
        public string SpecimenId { get; set; } = string.Empty;
        public int PanelId { get; set; }
        public CellTreatment CellTreatment { get; set; } = CellTreatment.None;
        public SerumTreatment SerumTreatment { get; set; } = SerumTreatment.None;

        /// <summary>
        /// Optional free-text override label; if blank the display label is auto-generated.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        public string CreatedDate { get; set; } = string.Empty;

        // Navigation — populated by DatabaseService when needed
        public string PanelName { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable label: <see cref="Label"/> when set, otherwise generated from the cell / serum treatment
        /// combination. A custom label that does not name the run's treatments gets them in parentheses
        /// ("Repeat (Ficin-treated)"): the treatment decides how the run's negatives can be read, so it is never
        /// hidden behind a free-text name on screen or in a report.
        /// </summary>
        public string DisplayLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Label))
                {
                    var missing = TreatmentNames().Where(n => !LabelNames(n.Keyword)).Select(n => n.Name).ToList();
                    return missing.Count == 0 ? Label : $"{Label} ({string.Join(" + ", missing)})";
                }
                var parts = new List<string>();
                parts.Add(AntigenTreatmentEffects.GetDisplayName(CellTreatment));
                var serumLabel = AntigenTreatmentEffects.GetDisplayName(SerumTreatment);
                if (!string.IsNullOrEmpty(serumLabel)) parts.Add(serumLabel);
                return string.Join(" + ", parts);
            }
        }

        /// <summary>The run's treatments with the keyword that shows a label already names each.</summary>
        private IEnumerable<(string Name, string Keyword)> TreatmentNames()
        {
            if (CellTreatment != CellTreatment.None)
                yield return (AntigenTreatmentEffects.GetDisplayName(CellTreatment), CellTreatment switch
                {
                    CellTreatment.Ficin => "ficin",
                    CellTreatment.Papain => "papain",
                    CellTreatment.DTT => "dtt",
                    _ => CellTreatment.ToString(),
                });
            if (SerumTreatment != SerumTreatment.None)
                yield return (AntigenTreatmentEffects.GetDisplayName(SerumTreatment), SerumTreatment switch
                {
                    SerumTreatment.Prewarmed => "prewarm",
                    SerumTreatment.AlloAdsorptionR1R1 => "r1r1",
                    SerumTreatment.AlloAdsorptionR2R2 => "r2r2",
                    SerumTreatment.AlloAdsorptionRr => "rr",
                    SerumTreatment.AutoAdsorption => "auto",
                    _ => SerumTreatment.ToString(),
                });
        }

        /// <summary>True when the custom label already names the treatment (whole-word match, ignoring case).</summary>
        private bool LabelNames(string keyword) =>
            Regex.IsMatch(Label, $@"(?<![A-Za-z0-9]){Regex.Escape(keyword)}", RegexOptions.IgnoreCase);

        public bool IsUntreated =>
            CellTreatment == CellTreatment.None && SerumTreatment == SerumTreatment.None;

        public override string ToString() => DisplayLabel;
    }
}
