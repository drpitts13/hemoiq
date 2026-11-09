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
            var kinds = result.ReactionPatterns.Select(p => p.Kind).ToHashSet(System.StringComparer.Ordinal);

            // Panreactive or autoantibody-like reactivity cannot be separated by single cells: every
            // cell reacts, so a list of unmet identification rules would point the wrong way.
            if (kinds.Contains(ReactionPatternClassifier.Panreactive) || kinds.Contains(ReactionPatternClassifier.Autoantibody))
            {
                needs.Add("Panreactive or autoantibody-like pattern: autocontrol, DAT and adsorption (then testing the adsorbed " +
                          "serum) are the next steps; selected cells alone will not separate the specificities.");
                return needs;
            }
            if (kinds.Contains(ReactionPatternClassifier.HighPrevalence) || kinds.Contains(ReactionPatternClassifier.Htla))
            {
                needs.Add("High-prevalence or HTLA pattern: test rare antigen-negative cells, and titration or neutralization " +
                          "where suggested, before attributing the reactivity to common antibodies.");
                var rare = result.SelectedCellRecommendations.Where(r => !r.IsExpired).Take(3)
                    .Select(r => $"{r.PanelName} cell {r.CellNumber}").ToList();
                if (rare.Count > 0) needs.Add("Selected cells: " + string.Join(", ", rare) + ".");
                return needs;
            }

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
