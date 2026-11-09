using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public enum RuleImportAction { Add, Update, Unchanged, Error }

    public sealed class RuleImportLine
    {
        public RuleImportAction Action { get; init; }
        public string Antibody { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        /// <summary>What will happen, or why the entry is refused.</summary>
        public string Detail { get; init; } = string.Empty;
        internal Rule? Incoming { get; init; }
        internal int? ExistingRuleId { get; init; }
    }

    /// <summary>
    /// Copies lab rules between installations as JSON. Import is two steps: <see cref="Preview"/>
    /// reads the file and lists what would be added, updated, left alone or refused, without
    /// writing anything; <see cref="Apply"/> writes the previewed adds and updates in one
    /// transaction through the normal audited rule methods. Rules match by antibody (one rule
    /// per antibody). Import never deletes a rule that is missing from the file.
    /// </summary>
    public static class RuleTransfer
    {
        public const string Format = "hemoiq-rules";
        public const int Version = 1;

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private sealed class RuleFile
        {
            public string? Format { get; set; }
            public int Version { get; set; }
            public string? ExportedAt { get; set; }
            public List<RuleEntry>? Rules { get; set; }
        }

        private sealed class RuleEntry
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Antibody { get; set; }
            public string? ExceptionAntigen { get; set; }
            public bool HeterozygousOk { get; set; }
            public int? MinRuleoutCount { get; set; }
            public bool SeparationExempt { get; set; }
            public string? SeparationExemptFrom { get; set; }
        }

        public static string Export(IEnumerable<Rule> rules, DateTime now) =>
            JsonSerializer.Serialize(new RuleFile
            {
                Format = Format,
                Version = Version,
                ExportedAt = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Rules = rules.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Select(r => new RuleEntry
                {
                    Name = r.Name,
                    Description = r.Description,
                    Antibody = r.Antibody,
                    ExceptionAntigen = r.ExceptionAntigen,
                    HeterozygousOk = r.HeterozygousOk,
                    MinRuleoutCount = r.MinRuleoutCount,
                    SeparationExempt = r.SeparationExempt,
                    SeparationExemptFrom = r.SeparationExemptFrom,
                }).ToList(),
            }, Options);

        /// <summary>Reads a rule file and plans the import. Throws <see cref="ArgumentException"/> when the file itself is unusable.</summary>
        public static List<RuleImportLine> Preview(string json, IReadOnlyList<Rule> existing, IEnumerable<string> knownAntigens)
        {
            RuleFile? file;
            try { file = JsonSerializer.Deserialize<RuleFile>(json, Options); }
            catch (JsonException ex) { throw new ArgumentException("The file is not valid JSON: " + ex.Message); }
            if (file == null || !string.Equals(file.Format, Format, StringComparison.Ordinal))
                throw new ArgumentException($"The file is not a HemoIQ rule export (expected format \"{Format}\").");
            if (file.Version > Version)
                throw new ArgumentException($"The file is rule export version {file.Version}; this version of HemoIQ reads version {Version} or earlier.");

            var known = knownAntigens.ToList();
            var lines = new List<RuleImportLine>();
            var claimed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in file.Rules ?? new List<RuleEntry>())
            {
                var name = (e.Name ?? string.Empty).Trim();
                var raw = (e.Antibody ?? string.Empty).Trim();
                string key;
                try
                {
                    if (name.Length == 0) throw new ArgumentException("The rule has no name.");
                    // Validate the antibody only; duplicates against stored rules become updates.
                    key = RuleValidation.NormalizeAntibody(raw, known, Array.Empty<Rule>(), null);
                    if (!claimed.Add(key)) throw new ArgumentException($"The file has more than one rule for {key}.");
                    if (e.MinRuleoutCount is int n && (n < RuleoutPolicy.MinAllowedCount || n > RuleoutPolicy.MaxAllowedCount))
                        throw new ArgumentException($"Min rule-out count {n} is outside {RuleoutPolicy.MinAllowedCount}–{RuleoutPolicy.MaxAllowedCount}.");
                    if (!string.IsNullOrWhiteSpace(e.ExceptionAntigen) && !known.Contains(e.ExceptionAntigen.Trim(), StringComparer.Ordinal))
                        throw new ArgumentException($"Exception antigen {e.ExceptionAntigen.Trim()} is not a known antigen.");
                }
                catch (ArgumentException ex)
                {
                    lines.Add(new RuleImportLine { Action = RuleImportAction.Error, Antibody = raw, Name = name, Detail = ex.Message });
                    continue;
                }

                var incoming = new Rule
                {
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(e.Description) ? null : e.Description.Trim(),
                    Antibody = key,
                    ExceptionAntigen = string.IsNullOrWhiteSpace(e.ExceptionAntigen) ? null : e.ExceptionAntigen.Trim(),
                    HeterozygousOk = e.HeterozygousOk,
                    MinRuleoutCount = e.MinRuleoutCount ?? RuleoutPolicy.DefaultLabCount,
                    SeparationExempt = e.SeparationExempt,
                    SeparationExemptFrom = e.SeparationExempt && !string.IsNullOrWhiteSpace(e.SeparationExemptFrom)
                        ? e.SeparationExemptFrom.Trim() : null,
                };
                var match = existing.FirstOrDefault(r => RuleoutPolicy.SameAntibody(r.Antibody, key));
                if (match == null)
                {
                    lines.Add(new RuleImportLine
                    {
                        Action = RuleImportAction.Add, Antibody = key, Name = name, Incoming = incoming,
                        Detail = $"New rule: {Describe(incoming)}.",
                    });
                    continue;
                }
                var changes = Differences(match, incoming);
                lines.Add(new RuleImportLine
                {
                    Action = changes.Count == 0 ? RuleImportAction.Unchanged : RuleImportAction.Update,
                    Antibody = key, Name = name, Incoming = incoming, ExistingRuleId = match.RuleId,
                    Detail = changes.Count == 0 ? "Same as the stored rule." : $"Updates rule '{match.Name}': {string.Join("; ", changes)}.",
                });
            }
            return lines;
        }

        /// <summary>
        /// Writes the previewed adds and updates in one transaction (all or nothing). Each write
        /// is the normal audited, permission-checked rule method. Returns (added, updated).
        /// </summary>
        public static (int Added, int Updated) Apply(DatabaseService db, IEnumerable<RuleImportLine> preview)
        {
            int added = 0, updated = 0;
            var todo = preview.Where(l => l.Action is RuleImportAction.Add or RuleImportAction.Update).ToList();
            db.RunInTransaction(() =>
            {
                foreach (var l in todo)
                {
                    var r = l.Incoming!;
                    if (l.Action == RuleImportAction.Add)
                    {
                        db.AddRule(r.Name, r.Description, r.Antibody, r.ExceptionAntigen, r.HeterozygousOk,
                            r.MinRuleoutCount, r.SeparationExempt, r.SeparationExemptFrom);
                        added++;
                    }
                    else
                    {
                        db.UpdateRule(l.ExistingRuleId!.Value, r.Name, r.Description, r.Antibody, r.ExceptionAntigen,
                            r.HeterozygousOk, r.MinRuleoutCount, r.SeparationExempt, r.SeparationExemptFrom);
                        updated++;
                    }
                }
            });
            return (added, updated);
        }

        /// <summary>One line per planned change, for the confirmation prompt.</summary>
        public static string Summarize(IReadOnlyList<RuleImportLine> lines)
        {
            int Count(RuleImportAction a) => lines.Count(l => l.Action == a);
            var head = $"{Count(RuleImportAction.Add)} to add, {Count(RuleImportAction.Update)} to update, " +
                       $"{Count(RuleImportAction.Unchanged)} unchanged, {Count(RuleImportAction.Error)} refused.";
            var body = lines.Where(l => l.Action != RuleImportAction.Unchanged)
                .Select(l => $"{l.Action}: {(l.Antibody.Length > 0 ? l.Antibody : "(no antibody)")} '{l.Name}' — {l.Detail}");
            return head + Environment.NewLine + string.Join(Environment.NewLine, body);
        }

        private static string Describe(Rule r) =>
            $"{r.Antibody}, {r.MinRuleoutCount} rule-out cell(s), heterozygous {(r.HeterozygousOk ? "allowed" : "not allowed")}" +
            (r.ExceptionAntigen == null ? "" : $", exception antigen {r.ExceptionAntigen}") +
            (r.SeparationExempt ? $", separation exempt from {r.SeparationExemptFrom ?? "all partners"}" : "");

        private static List<string> Differences(Rule a, Rule b)
        {
            var d = new List<string>();
            if (a.Name != b.Name) d.Add($"name '{a.Name}' → '{b.Name}'");
            if ((a.Description ?? "") != (b.Description ?? "")) d.Add("description");
            if ((a.ExceptionAntigen ?? "") != (b.ExceptionAntigen ?? "")) d.Add($"exception antigen {a.ExceptionAntigen ?? "none"} → {b.ExceptionAntigen ?? "none"}");
            if (a.HeterozygousOk != b.HeterozygousOk) d.Add($"heterozygous {(b.HeterozygousOk ? "allowed" : "not allowed")}");
            if (a.MinRuleoutCount != b.MinRuleoutCount) d.Add($"rule-out cells {a.MinRuleoutCount} → {b.MinRuleoutCount}");
            if (a.SeparationExempt != b.SeparationExempt || (a.SeparationExemptFrom ?? "") != (b.SeparationExemptFrom ?? ""))
                d.Add($"separation exemption {(b.SeparationExempt ? b.SeparationExemptFrom ?? "all partners" : "off")}");
            return d;
        }
    }
}
