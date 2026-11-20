using System;
using System.Globalization;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed record TransfusionHistoryFinding(
        string SourceAccession,
        DateTime TransfusedOn,
        int DaysBeforeCollection,
        string CurrentStatus,
        bool MakesPhenotypeUnreliable,
        string Note);

    /// <summary>
    /// Cross-checks a specimen's transfusion record against the patient's other specimens (same MRN).
    /// An earlier specimen that records a red-cell transfusion within the recent-transfusion window
    /// before this collection contradicts "Not transfused" and fills in "Unknown": in both cases the
    /// serologic phenotype is treated as unreliable, with the source named. A "Transfused" record
    /// whose last date is older than the one on file gets a note to update it. Transfusions recorded
    /// only as non-red-cell products (platelets, plasma, cryo) are not used.
    /// </summary>
    public static class TransfusionHistoryCheck
    {
        public static TransfusionHistoryFinding? Find(DatabaseService db, Specimen current, int windowDays)
        {
            if (!TryDate(current.CollectedAt ?? current.CreatedDate, out var collected)) return null;
            var latest = PatientHistory.OtherSpecimens(db, current)
                .Where(s => TransfusionStatus.Normalize(s.TransfusionStatus) == TransfusionStatus.Transfused)
                .Where(s => !NonRedCellOnly(s.TransfusionProducts))
                .Select(s => (Specimen: s, Ok: TryDate(s.LastTransfusionDate, out var d), On: d.Date))
                .Where(x => x.Ok && x.On <= collected.Date)
                .OrderByDescending(x => x.On)
                .FirstOrDefault();
            if (latest.Specimen == null) return null;
            var days = (collected.Date - latest.On).Days;
            if (days > windowDays) return null;

            var status = TransfusionStatus.Normalize(current.TransfusionStatus);
            var on = latest.On.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var source = $"{latest.Specimen.AccessionNumber} records a red-cell transfusion on {on}, {days} day(s) before this collection " +
                         $"(within the {windowDays}-day window)";
            if (status == TransfusionStatus.NotTransfused)
                return new(latest.Specimen.AccessionNumber, latest.On, days, status, true,
                    $"REVIEW: this specimen is recorded as Not transfused, but {source}. Patient typing is treated as unreliable " +
                    "until the transfusion status is corrected or confirmed.");
            if (status == TransfusionStatus.Unknown)
                return new(latest.Specimen.AccessionNumber, latest.On, days, status, true,
                    $"Transfusion status is Unknown, and {source}. Patient typing is treated as unreliable; " +
                    $"record Transfused with last transfusion {on} (or later) on this specimen.");
            if (TryDate(current.LastTransfusionDate, out var recorded) && recorded.Date < latest.On)
                return new(latest.Specimen.AccessionNumber, latest.On, days, status, false,
                    $"This specimen's last transfusion date ({recorded:yyyy-MM-dd}) is earlier than the transfusion on {on} " +
                    $"recorded on {latest.Specimen.AccessionNumber}. Update the date so the transfusion window is counted correctly.");
            return null;
        }

        /// <summary>Applies a finding to the assessment: unreliable phenotype with the reason appended.</summary>
        public static TransfusionAssessment Apply(TransfusionAssessment transfusion, TransfusionHistoryFinding? finding)
        {
            if (finding == null || !finding.MakesPhenotypeUnreliable) return transfusion;
            return new TransfusionAssessment
            {
                PhenotypeUnreliable = true,
                GenotypeMayBeDonor = transfusion.GenotypeMayBeDonor,
                Explanation = $"{transfusion.Explanation} {finding.Note}".Trim(),
                Source = "history",
                Status = transfusion.Status,
            };
        }

        private static bool NonRedCellOnly(string? products)
        {
            var codes = TransfusionProducts.Parse(products);
            return codes.Count > 0 && !codes.Any(TransfusionProducts.ContainRedCells.Contains);
        }

        private static bool TryDate(string? text, out DateTime date)
        {
            date = default;
            return !string.IsNullOrWhiteSpace(text) &&
                   DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
