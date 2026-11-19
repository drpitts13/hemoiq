using System;
using System.Collections.Generic;
using System.Linq;
using AntibodyPanels.Models;

namespace AntibodyPanels.Services
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

        public static Rule? FindAntibodyRule(string antibody, IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
            {
                if (string.Equals(rule.Antibody, antibody, StringComparison.OrdinalIgnoreCase))
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
                if (!string.Equals(rule.Antibody, $"anti-{antigen}", StringComparison.Ordinal))
                    continue;
                if (string.IsNullOrWhiteSpace(rule.ExceptionAntigen) ||
                    string.Equals(rule.ExceptionAntigen, antigen, StringComparison.Ordinal))
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

        /// <summary>
        /// Names counted cells whose AHG is 0 and whose check cell is 0.
        /// Empty when every counted cell has a reacting check cell.
        /// </summary>
        public static string FormatFailedCheckCells(IEnumerable<RuleoutDetail> cells)
        {
            var failed = cells
                .Where(c => !string.IsNullOrWhiteSpace(c.CellNumber)
                    && ReactionGrade.IsNegative(c.AHG)
                    && ReactionGrade.IsNegative(c.CC))
                .Select(c => c.CellNumber)
                .Distinct()
                .ToList();
            if (failed.Count == 0) return string.Empty;
            if (failed.Count == 1) return $"cell {failed[0]} check cell did not react";
            return $"cells {string.Join(", ", failed)} check cell did not react";
        }

        public static string FormatCellList(IEnumerable<RuleoutDetail> cells)
        {
            var listed = cells
                .Where(c => !string.IsNullOrWhiteSpace(c.CellNumber))
                .ToList();
            var numbers = listed
                .Select(c => c.CellNumber)
                .Distinct()
                .ToList();
            if (numbers.Count == 0) return string.Empty;
            var head = numbers.Count == 1
                ? $"cell {numbers[0]}"
                : $"cells {string.Join(", ", numbers)}";
            var failedText = FormatFailedCheckCells(listed);
            if (string.IsNullOrEmpty(failedText)) return $"({head})";
            if (numbers.Count == 1)
                failedText = "check cell did not react";
            return $"({head}; {failedText})";
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
