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
    /// query must match at least one field (case-insensitive substring). From/To and the Date column use the
    /// received date (received time when recorded, otherwise the entry date).
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

            var rows = new List<InvestigationSearchRow>();
            foreach (var s in db.GetAllSpecimens())
            {
                // The range is the received date, as the help states and as Analytics counts it (the received time
                // when recorded, otherwise the date the specimen was entered).
                if ((from != null || to != null) && !WorkloadService.ReceivedInRange(s, from, to)) continue;
                var received = TurnaroundService.ReceivedTime(s);
                var date = received == DateTime.MinValue
                    ? (s.CreatedDate.Length >= 10 ? s.CreatedDate[..10] : s.CreatedDate)
                    : received.ToString("yyyy-MM-dd");

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
                    Date = date,
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
