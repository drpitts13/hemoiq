using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed class InvestigationSearchRow
    {
        public string Accession { get; init; } = string.Empty;
        public string Mrn { get; init; } = string.Empty;
        public string Patient { get; init; } = string.Empty;
        public string DateOfBirth { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string FinalId { get; init; } = string.Empty;
        public string IdentifiedBy { get; init; } = string.Empty;
        public string Encounter { get; init; } = string.Empty;
        /// <summary>Panels tested, e.g. "Resolve A (Ortho, lot 1234); Selected (lot 77)".</summary>
        public string Panels { get; init; } = string.Empty;
    }

    /// <summary>
    /// Finds investigations by accession, MRN, patient name or DOB, final
    /// antibodies, technologist, previous antibodies, collector, locations, encounter,
    /// ordering provider, or the name, manufacturer or lot of any panel tested. Every word of the
    /// query must match at least one field (case-insensitive substring).
    /// </summary>
    public static class InvestigationSearch
    {
        public static List<InvestigationSearchRow> Search(DatabaseService db, string? query,
            DateTime? from = null, DateTime? to = null)
        {
            var words = (query ?? string.Empty)
                .Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var patients = db.GetAllPatients().ToDictionary(p => p.Mrn, StringComparer.OrdinalIgnoreCase);
            var panelsBySpecimen = db.GetPanelsBySpecimen();
            var fromText = from?.ToString("yyyy-MM-dd");
            var toText = to?.ToString("yyyy-MM-dd");

            var rows = new List<InvestigationSearchRow>();
            foreach (var s in db.GetAllSpecimens())
            {
                var created = s.CreatedDate.Length >= 10 ? s.CreatedDate[..10] : s.CreatedDate;
                if (fromText != null && string.CompareOrdinal(created, fromText) < 0) continue;
                if (toText != null && string.CompareOrdinal(created, toText) > 0) continue;

                Patient? p = null;
                if (s.Mrn != null) patients.TryGetValue(s.Mrn, out p);
                panelsBySpecimen.TryGetValue(s.AccessionNumber, out var panels);
                var panelText = panels == null ? string.Empty : DescribePanels(panels);
                var fields = new[]
                {
                    panelText,
                    s.AccessionNumber, s.Mrn, p?.Name, p?.DateOfBirth, s.FinalAntibodies,
                    s.IdentifiedBy, s.PreviousAntibodies, s.FinalComment,
                    s.Collector, s.CollectionLocation, s.OrderingLocation, s.OrderingProvider, s.EncounterNumber,
                };
                if (!words.All(w => TextFilter.Matches(w, fields))) continue;

                rows.Add(new InvestigationSearchRow
                {
                    Accession = s.AccessionNumber,
                    Mrn = s.Mrn ?? string.Empty,
                    Patient = p?.Name ?? string.Empty,
                    DateOfBirth = p?.DateOfBirth ?? string.Empty,
                    Date = created,
                    Status = InvestigationStatusService.Compute(db, s).Label,
                    FinalId = s.FinalAntibodies ?? string.Empty,
                    IdentifiedBy = s.IdentifiedBy ?? string.Empty,
                    Encounter = s.EncounterNumber ?? string.Empty,
                    Panels = panelText,
                });
            }
            return rows
                .OrderByDescending(r => r.Date, StringComparer.Ordinal)
                .ThenBy(r => r.Accession, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string DescribePanels(IEnumerable<(string Name, string? Vendor, string? Lot)> panels) =>
            string.Join("; ", panels.Select(x =>
            {
                var extra = new List<string>();
                if (!string.IsNullOrWhiteSpace(x.Vendor)) extra.Add(x.Vendor!.Trim());
                if (!string.IsNullOrWhiteSpace(x.Lot)) extra.Add("lot " + x.Lot!.Trim());
                return extra.Count == 0 ? x.Name : $"{x.Name} ({string.Join(", ", extra)})";
            }));
    }
}
