using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// What an analysis left unresolved that more testing could settle: clinically significant
    /// antibodies not yet ruled out, suspected antibodies short of the identification rule, and
    /// multiple-antibody separation shortfalls, with the selected cells recommended for them.
    /// Saved with each analysis so the investigation status can say "Awaiting additional
    /// testing" and name the next test. Suggestions only; nothing is ordered.
    /// </summary>
    public static class AdditionalTestingNeeds
    {
        public static List<string> Compute(AnalysisResult result)
        {
            var needs = new List<string>();

            var short_ = result.SuspectedStatistics
                .Where(kv => !kv.Value.MeetsIdentificationRule)
                .OrderByDescending(kv => result.Suspected.TryGetValue(kv.Key, out var p) ? p : 0)
                .Select(kv => kv.Key)
                .ToList();
            if (short_.Count > 0)
                needs.Add($"Identification rule not met for {Names(short_)}.");

            // Every clinically significant antigen the panels type: an antibody to it that is neither
            // ruled out nor suspected is still open, including ones with no antigen-positive
            // nonreactive cell at all (they have no rule-out evaluation).
            var identified = result.SuspectedStatistics.Where(kv => kv.Value.MeetsIdentificationRule).Select(kv => kv.Key);
            // Reliable patient typing (or a genotype) positive for the antigen argues against the
            // alloantibody, so it is not left open for more cell testing.
            var patientPositive = result.PatientTypingConsiderations
                .Where(c => c.Kind == PatientTypingKind.Against ||
                            (c.Kind == PatientTypingKind.Predicted && c.PatientValue == "+"))
                .Select(c => c.Antibody)
                .ToHashSet(System.StringComparer.Ordinal);
            var notRuledOut = AntigenConstants.ClinicallySignificantAntigens
                .Where(ag => !result.UntypedClinicallySignificant.Contains(ag))
                .Select(ag => "anti-" + ag)
                .Where(ab => !result.RuledOut.ContainsKey(ab) && !identified.Contains(ab) && !short_.Contains(ab) &&
                             !patientPositive.Contains(ab))
                .ToList();
            if (notRuledOut.Count > 0)
                needs.Add($"Not yet ruled out: {Names(notRuledOut)}.");

            var separation = result.SeparationEvaluations.Where(e => !e.MeetsCriterion).Select(e => e.Antibody).ToList();
            if (separation.Count > 0)
                needs.Add($"Multiple-antibody separation not met for {Names(separation)}.");

            var cells = result.SelectedCellRecommendations.Where(r => !r.IsExpired).Take(3)
                .Select(r => $"{r.PanelName} cell {r.CellNumber}").ToList();
            if (needs.Count > 0 && cells.Count > 0)
                needs.Add("Selected cells: " + string.Join(", ", cells) + ".");
            return needs;
        }

        private static string Names(IReadOnlyList<string> antibodies) =>
            string.Join(", ", antibodies.Take(8)) + (antibodies.Count > 8 ? $" and {antibodies.Count - 8} more" : "");
    }
}
