using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed record ReceiptItem(string Name, bool Done, bool Required, string Why);

    /// <summary>
    /// What should be recorded when a specimen is received, before testing starts. Required items
    /// change how the analysis is interpreted (patient history and requirements need the MRN; the
    /// transfusion window and specimen dating need the collection time and transfusion status).
    /// Advisory items complete the record (receipt time for turnaround, hemolysis, pregnancy, DAT).
    /// The checklist only reports; it never blocks testing.
    /// </summary>
    public static class ReceiptChecklist
    {
        public static List<ReceiptItem> Evaluate(Specimen s)
        {
            var transfusion = TransfusionStatus.Normalize(s.TransfusionStatus);
            var items = new List<ReceiptItem>
            {
                new("Patient MRN", !string.IsNullOrWhiteSpace(s.Mrn), true,
                    "links patient history, earlier antibodies and transfusion requirements"),
                new("Collection date/time", !string.IsNullOrWhiteSpace(s.CollectedAt), true,
                    "the recent-transfusion window and specimen dating count from collection"),
                new("Transfusion status", transfusion != TransfusionStatus.Unknown, true,
                    "decides whether the serologic phenotype can be trusted"),
            };
            if (transfusion == TransfusionStatus.Transfused)
                items.Add(new("Last transfusion date", !string.IsNullOrWhiteSpace(s.LastTransfusionDate), true,
                    "without it the phenotype is treated as unreliable and earlier phenotypes cannot be used"));
            items.Add(new("Received date/time", !string.IsNullOrWhiteSpace(s.ReceivedAt), false,
                "turnaround is otherwise counted from when the record was created"));
            items.Add(new("Visual hemolysis", SpecimenHemolysis.Normalize(s.Hemolysis) != SpecimenHemolysis.NotRecorded, false,
                "hemolysis in the specimen can be mistaken for a reaction"));
            items.Add(new("Pregnancy status", PregnancyStatus.Normalize(s.PregnancyStatus) != PregnancyStatus.Unknown, false,
                "pregnancy and RhIG history explain passive or newly formed antibodies"));
            items.Add(new("DAT", !string.IsNullOrWhiteSpace(s.DatResult) || !string.IsNullOrWhiteSpace(s.DatIgg) ||
                                 !string.IsNullOrWhiteSpace(s.DatC3), false,
                "a positive DAT changes how a positive autocontrol and panreactivity are read"));
            return items;
        }

        public static bool RequiredComplete(IEnumerable<ReceiptItem> items) => items.Where(i => i.Required).All(i => i.Done);

        /// <summary>One line for the detail pane and report: complete, or what is missing (required first).</summary>
        public static string Summary(IReadOnlyList<ReceiptItem> items)
        {
            var missingRequired = items.Where(i => i.Required && !i.Done).Select(i => i.Name).ToList();
            var missingAdvisory = items.Where(i => !i.Required && !i.Done).Select(i => i.Name).ToList();
            var done = items.Count(i => i.Done);
            if (missingRequired.Count == 0 && missingAdvisory.Count == 0)
                return $"Complete ({done} of {items.Count}).";
            var parts = new List<string>();
            if (missingRequired.Count > 0) parts.Add("missing " + string.Join(", ", missingRequired));
            if (missingAdvisory.Count > 0) parts.Add("not recorded " + string.Join(", ", missingAdvisory));
            return $"{done} of {items.Count}: {string.Join("; ", parts)}.";
        }

        /// <summary>Each missing item with its reason, for a tooltip.</summary>
        public static string Detail(IEnumerable<ReceiptItem> items) =>
            string.Join(System.Environment.NewLine, items.Where(i => !i.Done)
                .Select(i => $"{(i.Required ? "Required" : "Advisory")}: {i.Name} — {i.Why}."));
    }
}
