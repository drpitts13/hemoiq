using System.Collections.Generic;

namespace AntibodyPanels.Models
{
    public class PanelCell
    {
        public int Id { get; set; }
        public int PanelId { get; set; }
        public string CellNumber { get; set; } = string.Empty;
        public string? DonorId { get; set; }
        public string? RhPhenotype { get; set; }
        public string? SpecialTypes { get; set; }

        // Antigen values keyed by antigen name (e.g. "D", "C", "c", ...)
        public Dictionary<string, string> Antigens { get; set; } = new();

        public string GetAntigen(string antigen) =>
            TryGetTyped(antigen, out var v)
                ? (AntigenConstants.IsExplicitHomozygousValue(v) ? "+" : v)
                : "-";

        /// <summary>
        /// Value for a printed antigram. An untyped or NT cell prints NT.
        /// A typed minus stays minus. Explicit ++ still prints as +.
        /// </summary>
        public string PrintedAntigen(string antigen) =>
            HasTypedAntigen(antigen) ? GetAntigen(antigen) : AntigenConstants.AntigenNotTested;

        public string? GetTypedValue(string antigen) =>
            TryGetTyped(antigen, out var v) ? v : null;

        /// <summary>
        /// True when this cell has a typed +/−/++ value for the antigen.
        /// Missing, blank, or NT is not the same as antigen-negative.
        /// </summary>
        public bool HasTypedAntigen(string antigen) => TryGetTyped(antigen, out _);

        public bool IsAntigenPositive(string antigen) =>
            TryGetTyped(antigen, out var v) && AntigenConstants.IsAntigenPositiveValue(v);

        public bool IsExplicitlyHomozygous(string antigen) =>
            TryGetTyped(antigen, out var v) && AntigenConstants.IsExplicitHomozygousValue(v);

        /// <summary>
        /// Homozygous when the source marked ++, or when the antithetical partner
        /// is typed negative. An untyped partner is not treated as negative.
        /// A typed partner + overrides a ++ mark (conflict — not homozygous).
        /// </summary>
        public bool IsHomozygousFor(string antigen)
        {
            if (!IsAntigenPositive(antigen)) return false;
            AntigenConstants.AntitheticalPairs.TryGetValue(antigen, out var partner);
            if (partner != null && HasTypedAntigen(partner) && GetAntigen(partner) == "+")
                return false;
            if (IsExplicitlyHomozygous(antigen)) return true;
            return partner != null && HasTypedAntigen(partner) && GetAntigen(partner) == "-";
        }

        public void SetAntigen(string antigen, string value) =>
            Antigens[antigen] = value;

        /// <summary>
        /// Antigens to show when an all-NT column should be omitted.
        /// A column stays when any cell is typed +, ++, or −. Blank and NT
        /// do not keep it. An empty cell list keeps the full order.
        /// The stored panel order is not changed.
        /// </summary>
        public static IReadOnlyList<string> AntigensTypedOnAnyCell(
            IReadOnlyList<string> order,
            IReadOnlyList<PanelCell> cells)
        {
            if (cells.Count == 0)
                return new List<string>(order);
            var typed = new List<string>();
            foreach (var ag in order)
            {
                foreach (var cell in cells)
                {
                    if (!cell.HasTypedAntigen(ag)) continue;
                    typed.Add(ag);
                    break;
                }
            }
            return typed;
        }

        private bool TryGetTyped(string antigen, out string value)
        {
            if (Antigens.TryGetValue(antigen, out var v) && AntigenConstants.IsTypedAntigenValue(v))
            {
                value = v;
                return true;
            }
            value = "";
            return false;
        }
    }
}
