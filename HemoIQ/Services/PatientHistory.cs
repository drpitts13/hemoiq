using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>An antibody confirmed on another specimen with the same MRN.</summary>
    public sealed class PriorConfirmedAntibody
    {
        public string Antibody { get; init; } = string.Empty;
        public string Accession { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;

        public string Explanation =>
            $"{Antibody} was confirmed on accession {Accession} ({Date}) for this MRN. " +
            "Antibodies can fall below detection; treat this as historical evidence, not a current identification.";
    }

    /// <summary>
    /// Collects the same patient's other specimens by MRN. Only confirmed
    /// final calls are carried forward; suspected lists are not.
    /// </summary>
    public static class PatientHistory
    {
        public static string? NormalizeMrn(string? mrn)
        {
            var t = (mrn ?? string.Empty).Trim();
            return t.Length == 0 ? null : t.ToUpperInvariant();
        }

        public static List<Specimen> OtherSpecimens(DatabaseService db, Specimen current)
        {
            var mrn = NormalizeMrn(current.Mrn);
            if (mrn == null) return new List<Specimen>();
            return db.GetSpecimensByMrn(mrn)
                .Where(s => !string.Equals(s.AccessionNumber, current.AccessionNumber, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.CreatedDate, StringComparer.Ordinal)
                .ToList();
        }

        public static List<PriorConfirmedAntibody> ConfirmedAntibodies(IEnumerable<Specimen> others)
        {
            var list = new List<PriorConfirmedAntibody>();
            // Antigen names are case-sensitive (anti-E and anti-e differ); only the prefix is not.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            // Oldest first so each antibody cites its earliest confirmation.
            foreach (var s in others.Where(o => o.HasFinalCall).OrderBy(o => o.CreatedDate, StringComparer.Ordinal))
            {
                foreach (var ab in FinalAntibodyParser.Split(s.FinalAntibodies))
                {
                    if (AntibodyKey(ab) is not string key) continue;
                    if (!seen.Add(key)) continue;
                    list.Add(new PriorConfirmedAntibody
                    {
                        Antibody = key,
                        Accession = s.AccessionNumber,
                        Date = string.IsNullOrEmpty(s.IdentifiedAt) ? s.CreatedDate : s.IdentifiedAt!.Split('T', ' ')[0],
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// "anti-" plus the antigen with its case kept, or null for text that is not an antibody.
        /// "Anti-E" and "anti-E" share a key; "anti-E" and "anti-e" do not.
        /// </summary>
        public static string? AntibodyKey(string? antibody)
        {
            var t = (antibody ?? string.Empty).Trim();
            if (!t.StartsWith("anti-", StringComparison.OrdinalIgnoreCase)) return null;
            var antigen = t[5..].Trim();
            return antigen.Length == 0 ? null : "anti-" + antigen;
        }

        /// <summary>
        /// One line per other specimen, newest first, for the detail pane and reports: accession,
        /// date and encounter, the confirmed identification, DAT, transfusion, and the phenotype and
        /// genotype recorded on that specimen when there is one (an earlier phenotype can be the
        /// pre-transfusion typing when the current one is unreliable).
        /// </summary>
        public static List<string> Describe(IEnumerable<Specimen> others) =>
            others.Select(s =>
            {
                var final = s.HasFinalCall ? $"confirmed {s.FinalAntibodies}" : "no confirmed identification";
                var when = s.EncounterNumber == null ? s.CreatedDate : $"{s.CreatedDate}, encounter {s.EncounterNumber}";
                var line = $"{s.AccessionNumber} ({when}): {final}; DAT {s.DatDisplay}; {s.TransfusionDisplay}";
                if (!string.IsNullOrWhiteSpace(s.Phenotype)) line += $"; phenotype {s.Phenotype!.Trim()}";
                if (!string.IsNullOrWhiteSpace(s.Genotype)) line += $"; genotype {s.Genotype!.Trim()}";
                return line;
            }).ToList();
    }
}
