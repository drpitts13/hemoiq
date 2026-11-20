using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Checks the antibody a lab rule applies to. Rules are matched to antibodies with the
    /// antigen's case kept (<see cref="RuleoutPolicy.SameAntibody"/>), and the first matching
    /// rule wins, so a rule must name a real antigen in its exact case, and each antibody may
    /// have only one rule. Otherwise a rule silently never applies, or which of two rules
    /// applies depends on their names.
    /// </summary>
    public static class RuleValidation
    {
        /// <summary>
        /// Returns "anti-" plus the antigen to store, or throws <see cref="ArgumentException"/>
        /// with a message naming the problem.
        /// </summary>
        public static string NormalizeAntibody(string? antibody, IEnumerable<string> knownAntigens,
            IEnumerable<Rule> existingRules, int? editingRuleId)
        {
            var key = PatientHistory.AntibodyKey(antibody)
                      ?? throw new ArgumentException($"'{antibody}' is not an antibody. Enter it as anti- followed by the antigen, for example anti-K or anti-Fya.");
            var antigen = key[5..];
            var known = knownAntigens.Distinct(StringComparer.Ordinal).ToList();
            if (!known.Contains(antigen, StringComparer.Ordinal))
            {
                var near = known.Where(k => string.Equals(k, antigen, StringComparison.OrdinalIgnoreCase)).ToList();
                throw new ArgumentException(near.Count > 0
                    ? $"{antigen} is not an antigen; antigen names are case-sensitive. Did you mean {string.Join(" or ", near.Select(n => "anti-" + n))}?"
                    : $"{antigen} is not a known antigen. Choose an antibody from the list, or add the antigen to a panel first.");
            }
            var clash = existingRules.FirstOrDefault(r => r.RuleId != editingRuleId && RuleoutPolicy.SameAntibody(r.Antibody, key));
            if (clash != null)
                throw new ArgumentException($"Rule '{clash.Name}' already applies to {key}. Edit that rule instead; only one rule per antibody is used.");
            return key;
        }

        /// <summary>
        /// Problems in the stored rules: antibodies that are not known antigens in their exact
        /// case, and antibodies with more than one rule. Each entry is (rule names, description).
        /// </summary>
        /// <summary>
        /// The rule-out cell count a rule stores. Analysis clamps to this range, so a value outside it would be shown
        /// on the Rules tab but not used; it is refused instead, as configuration import already does.
        /// </summary>
        public static int RequireRuleoutCount(int count)
        {
            if (count < RuleoutPolicy.MinAllowedCount || count > RuleoutPolicy.MaxAllowedCount)
                throw new ArgumentException(
                    $"Rule-out cells must be a whole number from {RuleoutPolicy.MinAllowedCount} to {RuleoutPolicy.MaxAllowedCount}; {count} was entered.");
            return count;
        }

        public static List<(string Title, string Detail)> Problems(IEnumerable<Rule> rules, IEnumerable<string> knownAntigens)
        {
            var list = new List<(string, string)>();
            var known = new HashSet<string>(knownAntigens, StringComparer.Ordinal);
            var all = rules.ToList();
            foreach (var r in all)
            {
                var key = PatientHistory.AntibodyKey(r.Antibody);
                if (key == null || !known.Contains(key[5..]))
                    list.Add(($"Rule '{r.Name}'",
                        $"Antibody '{r.Antibody}' is not a known antigen in its exact case, so this rule never applies. " +
                        "Edit the rule and choose the antibody from the list."));
                if (r.MinRuleoutCount < RuleoutPolicy.MinAllowedCount || r.MinRuleoutCount > RuleoutPolicy.MaxAllowedCount)
                    list.Add(($"Rule '{r.Name}'",
                        $"Rule-out cells is stored as {r.MinRuleoutCount}, outside {RuleoutPolicy.MinAllowedCount} to " +
                        $"{RuleoutPolicy.MaxAllowedCount}, so analysis uses {RuleoutPolicy.ClampRequiredCount(r.MinRuleoutCount)}. " +
                        "Edit the rule and enter the count you intend."));
            }
            foreach (var group in all.Where(r => PatientHistory.AntibodyKey(r.Antibody) != null)
                         .GroupBy(r => PatientHistory.AntibodyKey(r.Antibody)!, StringComparer.Ordinal)
                         .Where(g => g.Count() > 1))
            {
                var ordered = group.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.RuleId).ToList();
                list.Add(($"Rules for {group.Key}",
                    $"{ordered.Count} rules apply to {group.Key}: {string.Join(", ", ordered.Select(r => $"'{r.Name}'"))}. " +
                    $"Only '{ordered[0].Name}' is used for rule-out counts and separation. Merge them into one rule and delete the others."));
            }
            return list;
        }
    }
}
