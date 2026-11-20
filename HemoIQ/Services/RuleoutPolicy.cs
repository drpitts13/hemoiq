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
                if (!SameAntibody(rule.Antibody, $"anti-{antigen}"))
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
            var dosage = DescribeDosageExcluded(evaluation);
            if (evaluation.ObservedCount == 0 && dosage != null)
                return $"{name} cannot currently be ruled out because the only nonreactive {evaluation.Antigen}-positive cells " +
                       $"{dosage}, and heterozygous cells are not allowed for this antibody (dosage). " +
                       $"Test {evaluation.RequiredCount} homozygous {evaluation.Antigen}-positive cell(s), or allow heterozygous cells with a rule on the Rules tab.";
            return $"{name} does not yet meet configured rule-out criteria ({evaluation.ObservedCount} of {evaluation.RequiredCount} {zygosityWord} nonreactive cells)." +
                   (dosage == null ? "" : $" Not counted for dosage: nonreactive {evaluation.Antigen}-positive cells that {dosage}.");
        }

        /// <summary>"are heterozygous (cells 3, 5)" / "are of unknown zygosity because Jkb is not typed (cell 4)", or both; null when none.</summary>
        private static string? DescribeDosageExcluded(RuleoutEvaluation evaluation)
        {
            var cells = evaluation.DosageExcludedCells;
            if (cells.Count == 0) return null;
            var het = cells.Where(c => c.AntitheticalValue != null).ToList();
            var unknown = cells.Where(c => c.AntitheticalValue == null).ToList();
            var parts = new List<string>();
            if (het.Count > 0) parts.Add($"are heterozygous {FormatCellList(het)}");
            if (unknown.Count > 0)
            {
                var partner = unknown[0].Antithetical ?? "the antithetical antigen";
                parts.Add($"are of unknown zygosity because {partner} is not typed {FormatCellList(unknown)}");
            }
            return string.Join(" or ", parts);
        }
    }
}
