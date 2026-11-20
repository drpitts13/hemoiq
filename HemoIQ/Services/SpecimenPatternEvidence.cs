using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Specimen-screen wording for panreactive and autoantibody evidence.
    /// Ordinary alloantibody scores stay in the suspected grid.
    /// </summary>
    public static class SpecimenPatternEvidence
    {
        public static string Format(AnalysisResult result)
        {
            var lines = PatternLines(result).ToList();
            if (lines.Count == 0)
            {
                if (result.Suspected == null || result.Suspected.Count == 0)
                    return "No suspected specificity and no panreactive or autoantibody pattern note. Not a diagnosis.";
                return "";
            }

            var exclusions = (result.AdsorptionScoreExclusions ?? new List<string>())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();
            var comparisons = (result.AbsorptionConclusions ?? new List<AbsorptionConclusion>())
                .Select(c => c.Explanation)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();
            if (exclusions.Count > 0)
                lines.InsertRange(0, exclusions);
            if (comparisons.Count > 0)
                lines.InsertRange(exclusions.Count, comparisons);
            return string.Join(Environment.NewLine + Environment.NewLine, lines);
        }

        /// <summary>
        /// Suspected rows for the Specimens grid. These follow the current
        /// analysis, not an older saved list.
        /// </summary>
        public static List<SpecimenAntibody> SuspectedRows(AnalysisResult result)
        {
            return (result.Suspected ?? new Dictionary<string, double>())
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new SpecimenAntibody { Antibody = kv.Key, Probability = kv.Value })
                .ToList();
        }

        /// <summary>
        /// Ruled-out rows for the Specimens grid. A cell whose partner antigen
        /// is NT does not rule an antibody out, so an older saved count is not
        /// shown in its place.
        /// </summary>
        public static List<SpecimenRuleout> RuleoutRows(AnalysisResult result)
        {
            var evaluations = result.RuleoutEvaluations ?? new List<RuleoutEvaluation>();
            return (result.RuledOut ?? new Dictionary<string, int>())
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new SpecimenRuleout
                {
                    Antibody = kv.Key,
                    RuleoutCount = kv.Value,
                    Note = FailedCheckCellNote(evaluations, kv.Key),
                })
                .ToList();
        }

        /// <summary>
        /// Panreactive, autoantibody, and related phase notes. Empty when the
        /// result has no panreactive, autoantibody, or high-prevalence pattern.
        /// Absorption notes stay with the absorption section.
        /// </summary>
        public static IReadOnlyList<string> PatternLines(AnalysisResult result)
        {
            var patterns = result.ReactionPatterns ?? new List<ReactionPatternNote>();
            if (!patterns.Any(p => IsPanFamily(p.Kind)))
                return Array.Empty<string>();

            return patterns
                .Where(note => Include(note.Kind) && !string.IsNullOrWhiteSpace(note.Explanation))
                .Select(note => note.Explanation.Trim())
                .ToList();
        }

        private static string FailedCheckCellNote(List<RuleoutEvaluation> evaluations, string antibody)
        {
            var evaluation = evaluations.FirstOrDefault(e =>
                string.Equals(e.Antibody, antibody, StringComparison.Ordinal));
            if (evaluation == null) return string.Empty;
            return RuleoutPolicy.FormatFailedCheckCells(evaluation.Cells);
        }

        private static bool IsPanFamily(string kind) =>
            kind is ReactionPatternClassifier.Panreactive
                or ReactionPatternClassifier.Autoantibody
                or ReactionPatternClassifier.HighPrevalence;

        private static bool Include(string kind) =>
            IsPanFamily(kind)
            || kind is ReactionPatternClassifier.Cold
                or ReactionPatternClassifier.Warm
                or ReactionPatternClassifier.MixedPhase
                or ReactionPatternClassifier.Htla
                or ReactionPatternClassifier.LowFrequency;
    }
}
