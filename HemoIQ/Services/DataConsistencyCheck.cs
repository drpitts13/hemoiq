using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>One stored reaction row with the facts needed to check it against the specimen and panel.</summary>
    public sealed record ReactionRecord(
        string SpecimenId,
        int PanelId,
        string PanelName,
        string CellNumber,
        IReadOnlyDictionary<string, string?> Grades,
        bool PanelLinked,
        bool CellOnPanel);

    public sealed record ConsistencyFinding(string AccessionNumber, int PanelId, string Title, string Detail, string Action,
        string TargetTab = "Reactions");

    /// <summary>
    /// Stored reactions that foreign keys cannot catch but that make the record and the analysis
    /// disagree: grades on a panel no longer attached to the specimen (analysis still reads them,
    /// the Specimens tab no longer lists the panel), grades for cell numbers the panel no longer
    /// has (they cannot be matched to typing), and grade values outside the recognized set.
    /// Findings are listed for review; nothing is changed automatically.
    /// </summary>
    public static class DataConsistencyCheck
    {
        public static bool IsGraded(string? v) =>
            !string.IsNullOrWhiteSpace(v) && !string.Equals(v.Trim(), ReactionGrade.NotTested, StringComparison.OrdinalIgnoreCase);

        public static bool IsRecognized(string? v) =>
            string.IsNullOrWhiteSpace(v) || ReactionGrade.All.Contains(v.Trim(), StringComparer.Ordinal);

        public static List<ConsistencyFinding> Find(IEnumerable<ReactionRecord> rows)
        {
            var findings = new List<ConsistencyFinding>();
            foreach (var group in rows.GroupBy(r => (r.SpecimenId, r.PanelId))
                         .OrderBy(g => g.Key.SpecimenId, StringComparer.Ordinal).ThenBy(g => g.Key.PanelId))
            {
                var (accession, panelId) = group.Key;
                var panel = group.First().PanelName;
                var graded = group.Where(r => r.Grades.Values.Any(IsGraded)).ToList();

                if (graded.Count > 0 && !group.First().PanelLinked)
                    findings.Add(new ConsistencyFinding(accession, panelId,
                        $"{accession}: reactions on detached panel",
                        $"{panel} has {graded.Count} graded cell(s) for {accession} but is no longer attached to the specimen. " +
                        "Analysis still uses these reactions, though the Specimens tab does not list the panel.",
                        "Re-attach the panel on the Specimens tab, then review or clear its reactions with a reason.",
                        TargetTab: "Specimens"));

                var missingCells = graded.Where(r => !r.CellOnPanel).Select(r => r.CellNumber)
                    .Distinct(StringComparer.Ordinal).OrderBy(CellOrder).ToList();
                if (missingCells.Count > 0)
                    findings.Add(new ConsistencyFinding(accession, panelId,
                        $"{accession}: grades for cells the panel lacks",
                        $"{panel} has grades for cell(s) {string.Join(", ", missingCells)} for {accession}, but the panel no longer has " +
                        "those cells, so the grades cannot be matched to antigen typing.",
                        "Check the panel's cells against the vendor antigram, or clear those reactions with a reason."));

                var unrecognized = group
                    .SelectMany(r => r.Grades.Where(g => !IsRecognized(g.Value))
                        .Select(g => $"cell {r.CellNumber} {g.Key} \"{g.Value!.Trim()}\""))
                    .ToList();
                if (unrecognized.Count > 0)
                    findings.Add(new ConsistencyFinding(accession, panelId,
                        $"{accession}: unrecognized grades",
                        $"{panel} for {accession} stores grade(s) HemoIQ does not recognize: {string.Join("; ", unrecognized.Take(5))}" +
                        (unrecognized.Count > 5 ? $"; and {unrecognized.Count - 5} more" : "") + ". They are not read as reactive or nonreactive.",
                        "Re-enter those grades on the Reactions tab."));
            }
            return findings;
        }

        private static (int, string) CellOrder(string cell) =>
            (int.TryParse(cell, out var n) ? n : int.MaxValue, cell);
    }
}
