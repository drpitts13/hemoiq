using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Resolves the lab's configured rule-out policy and formats reviewable
    /// explanations. This is decision support: the sentence states why a
    /// criterion was or was not met; it is not a clinical diagnosis.
    /// </summary>
    public static class RuleoutPolicy
    {
        public const int MinAllowedCount = 1;
        public const int MaxAllowedCount = 5;
        public const int DefaultLabCount = 1;

        public static int ClampRequiredCount(int count)
        {
            if (count < MinAllowedCount) return MinAllowedCount;
            if (count > MaxAllowedCount) return MaxAllowedCount;
            return count;
        }

        /// <summary>
        /// True when both name the same antibody. "Anti-" and "anti-" match, but the antigen is
        /// case-sensitive: anti-K is not anti-k, anti-C is not anti-c, anti-E is not anti-e.
        /// </summary>
        public static bool SameAntibody(string? a, string? b) =>
            PatientHistory.AntibodyKey(a) is string ka && string.Equals(ka, PatientHistory.AntibodyKey(b), StringComparison.Ordinal);

        public static Rule? FindAntibodyRule(string antibody, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
            {
                if (SameAntibody(rule.Antibody, antibody))
                    return rule;
            }
            return null;
        }

        public static int ResolveRequiredCount(string antibody, IEnumerable<Rule> rules, int labDefault)
        {
            var rule = FindAntibodyRule(antibody, rules);
            return ClampRequiredCount(rule?.MinRuleoutCount ?? labDefault);
        }

        public static bool HeterozygousAllowed(string antigen, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
            {
                if (!rule.HeterozygousOk) continue;
                if (rule.ExceptionAntigen == antigen) return true;
                if (string.IsNullOrEmpty(rule.ExceptionAntigen) && SameAntibody(rule.Antibody, $"anti-{antigen}"))
                    return true;
            }
            return false;
        }

        public static string DisplayAntibody(string antibody)
        {
            if (string.IsNullOrWhiteSpace(antibody)) return antibody;
            if (antibody.StartsWith("anti-", StringComparison.OrdinalIgnoreCase))
                return "Anti-" + antibody[5..];
            return antibody;
        }

        public static string FormatCellList(IEnumerable<RuleoutDetail> cells)
        {
            var numbers = cells
                .Select(c => c.CellNumber)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();
            if (numbers.Count == 0) return string.Empty;
            if (numbers.Count == 1) return $"(cell {numbers[0]})";
            return $"(cells {string.Join(", ", numbers)})";
        }

        public static string FormatExplanation(RuleoutEvaluation evaluation)
        {
            var name = DisplayAntibody(evaluation.Antibody);
            var het = evaluation.HeterozygousAllowed
                ? "heterozygous cells allowed"
                : "heterozygous cells not allowed";
            var cellRef = FormatCellList(evaluation.Cells);
            var cellSuffix = string.IsNullOrEmpty(cellRef) ? "" : $" {cellRef}";

            if (evaluation.MeetsCriteria)
            {
                var observed = evaluation.ObservedCount == 1
                    ? "1 antigen-positive nonreactive cell was observed"
                    : $"{evaluation.ObservedCount} antigen-positive nonreactive cells were observed";
                return $"{name} currently meets configured rule-out criteria because {observed}{cellSuffix} (required {evaluation.RequiredCount}; {het}).";
            }

            var zygosityWord = evaluation.HeterozygousAllowed
                ? "antigen-positive"
                : "homozygous antigen-positive";
            return $"{name} does not yet meet configured rule-out criteria ({evaluation.ObservedCount} of {evaluation.RequiredCount} {zygosityWord} nonreactive cells).";
        }
    }
}
