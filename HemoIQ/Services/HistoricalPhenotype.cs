using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// When the current specimen's phenotype cannot be used (recent transfusion) or was not
    /// entered, the patient's earlier phenotype, typed before that transfusion, still reflects
    /// the patient's own antigens. This finds the most recent such phenotype on another specimen
    /// with the same MRN and turns it into patient-typing evidence, labelled with its source.
    /// Conservative: the earlier specimen must have been collected before the current specimen's
    /// last transfusion (or before the current specimen when it is not transfused), its own
    /// typing must have been reliable when collected, and patients with an allogeneic HPC
    /// transplant are skipped because their red-cell antigens may have changed.
    /// </summary>
    public static class HistoricalPhenotype
    {
        public sealed class Source
        {
            public Specimen Specimen { get; init; } = new();
            public string CollectedOn { get; init; } = string.Empty;
            public List<PatientTypingConsideration> Considerations { get; init; } = new();
        }

        public static Source? Find(DatabaseService db, Specimen current, PatientTyping currentTyping, int windowDays)
        {
            if (!currentTyping.PhenotypeUnreliable && currentTyping.Antigens.Count > 0) return null;
            if (string.IsNullOrWhiteSpace(current.Mrn)) return null;
            if (HpcTransplant.Normalize(db.GetPatient(current.Mrn)?.HpcTransplant) == HpcTransplant.Allogeneic) return null;

            DateTime cutoff;
            if (TransfusionStatus.Normalize(current.TransfusionStatus) == TransfusionStatus.Transfused)
            {
                if (!TryDate(current.LastTransfusionDate, out cutoff)) return null; // cannot show the typing predates it
            }
            else if (!TryDate(current.CollectedAt ?? current.CreatedDate, out cutoff)) return null;

            var candidates = PatientHistory.OtherSpecimens(db, current)
                .Where(s => !string.IsNullOrWhiteSpace(s.Phenotype))
                .Select(s => (Specimen: s, Ok: TryDate(s.CollectedAt ?? s.CreatedDate, out var d), On: d))
                .Where(x => x.Ok && x.On.Date < cutoff.Date)
                .OrderByDescending(x => x.On);
            foreach (var (s, _, on) in candidates)
            {
                var theirs = TransfusionContext.Assess(s.TransfusionStatus, s.LastTransfusionDate, s.CollectedAt ?? s.CreatedDate,
                    windowDays, s.Phenotype, s.Notes, s.TransfusionProducts);
                if (theirs.PhenotypeUnreliable) continue;
                var typing = PatientTypingParser.Parse(s.Phenotype, null, null, null, theirs);
                if (typing.PhenotypeUnreliable || typing.Antigens.Count == 0) continue;
                var onText = on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var label = $"Earlier phenotype from {s.AccessionNumber} ({onText}, before the current transfusion context)";
                var considerations = PatientTypingParser.Evaluate(typing)
                    .Where(c => c.Kind is PatientTypingKind.Supporting or PatientTypingKind.Against)
                    .Select(c => new PatientTypingConsideration
                    {
                        Antibody = c.Antibody,
                        Antigen = c.Antigen,
                        Kind = c.Kind,
                        PatientValue = c.PatientValue,
                        Explanation = $"{label}: {c.Explanation}",
                    })
                    .ToList();
                if (considerations.Count == 0) continue;
                return new Source { Specimen = s, CollectedOn = onText, Considerations = considerations };
            }
            return null;
        }

        private static bool TryDate(string? text, out DateTime date)
        {
            date = default;
            return !string.IsNullOrWhiteSpace(text) &&
                   DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
