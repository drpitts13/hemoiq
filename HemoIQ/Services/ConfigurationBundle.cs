using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed class ConfigurationPreview
    {
        public List<RuleImportLine> Rules { get; init; } = new();
        public PreferencePreview Preferences { get; init; } = new();
        /// <summary>Stored rules with no counterpart in the file. Kept unless the import is told to remove them.</summary>
        public List<Rule> LocalOnlyRules { get; init; } = new();

        /// <summary>Refused rules or Preferences: the bundle would not reproduce the source configuration, so it is not applied.</summary>
        public bool HasProblems =>
            Rules.Any(l => l.Action == RuleImportAction.Error) || Preferences.Refused.Count > 0;

        public bool HasChanges =>
            Rules.Any(l => l.Action is RuleImportAction.Add or RuleImportAction.Update) || Preferences.Changes.Count > 0;
    }

    public sealed record ConfigurationImportResult(int RulesAdded, int RulesUpdated, int RulesRemoved, int PreferencesChanged);

    /// <summary>
    /// The laboratory's analysis configuration (rules and laboratory-wide Preferences) as one
    /// file, so a second workstation or a restored installation reaches the same rule-outs.
    /// The sections use the same formats as the separate rule and Preferences exports. Import
    /// previews both sections and the stored rules the file lacks, refuses a file with any
    /// refused entry (a partial copy would not match the source), and applies everything in
    /// one transaction through the normal audited methods, plus one import_configuration audit.
    /// </summary>
    public static class ConfigurationBundle
    {
        public const string Format = "hemoiq-configuration";
        public const int Version = 1;

        public static string Export(IEnumerable<Rule> rules, LabSettings settings, DateTime now)
        {
            var root = new JsonObject
            {
                ["format"] = Format,
                ["version"] = Version,
                ["exportedAt"] = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["rules"] = JsonNode.Parse(RuleTransfer.Export(rules, now)),
                ["preferences"] = JsonNode.Parse(PreferenceTransfer.Export(settings, now)),
            };
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>Reads a configuration file and plans the import. Writes nothing. Throws <see cref="ArgumentException"/> when the file is unusable.</summary>
        public static ConfigurationPreview Preview(string json, IReadOnlyList<Rule> existingRules,
            IEnumerable<string> knownAntigens, LabSettings current)
        {
            JsonNode? root;
            try { root = JsonNode.Parse(json); }
            catch (JsonException ex) { throw new ArgumentException("The file is not valid JSON: " + ex.Message); }
            if (root is not JsonObject obj || (string?)obj["format"] != Format)
                throw new ArgumentException($"The file is not a HemoIQ configuration export (expected format \"{Format}\").");
            if (obj["version"] is JsonValue ver && ver.TryGetValue<int>(out var version) && version > Version)
                throw new ArgumentException($"The file is configuration export version {version}; this version of HemoIQ reads version {Version} or earlier.");
            if (obj["rules"] is not JsonObject rules) throw new ArgumentException("The file has no rules section.");
            if (obj["preferences"] is not JsonObject prefs) throw new ArgumentException("The file has no preferences section.");

            var ruleLines = RuleTransfer.Preview(rules.ToJsonString(), existingRules, knownAntigens);
            var inFile = ruleLines.Where(l => l.Action != RuleImportAction.Error).Select(l => l.Antibody).ToList();
            return new ConfigurationPreview
            {
                Rules = ruleLines,
                Preferences = PreferenceTransfer.Preview(prefs.ToJsonString(), current),
                LocalOnlyRules = existingRules
                    .Where(r => !inFile.Any(a => RuleoutPolicy.SameAntibody(a, r.Antibody)))
                    .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            };
        }

        /// <summary>
        /// Applies the preview in one transaction (all or nothing). <paramref name="settings"/> changes
        /// only after the database writes commit. With <paramref name="removeLocalOnlyRules"/>, stored
        /// rules missing from the file are deleted (audited), so the rules match the source exactly.
        /// </summary>
        public static ConfigurationImportResult Apply(DatabaseService db, LabSettings settings, ConfigurationPreview preview,
            bool removeLocalOnlyRules, string? fileName)
        {
            if (preview.HasProblems)
                throw new ArgumentException("The file has refused entries, so applying it would not reproduce its configuration. " +
                                            "Correct the file, or import its rules and Preferences separately.");
            var removing = removeLocalOnlyRules ? preview.LocalOnlyRules : new List<Rule>();
            if (preview.Rules.Any(l => l.Action is RuleImportAction.Add or RuleImportAction.Update) || removing.Count > 0)
                AccessControl.Demand(db, Permission.ConfigureRules);
            if (preview.Preferences.Changes.Count > 0)
                AccessControl.Demand(db, Permission.ConfigurePreferences);
            if (!preview.HasChanges && removing.Count == 0) return new ConfigurationImportResult(0, 0, 0, 0);

            // Preferences are applied to a copy inside the transaction (for the audit) and to the
            // live settings only once everything has committed.
            var trial = JsonSerializer.Deserialize<LabSettings>(JsonSerializer.Serialize(settings))!;
            var (added, updated) = (0, 0);
            db.RunInTransaction(() =>
            {
                (added, updated) = RuleTransfer.Apply(db, preview.Rules);
                foreach (var r in removing) db.DeleteRule(r.RuleId);
                if (preview.Preferences.Changes.Count > 0)
                    PreferenceTransfer.Apply(db, trial, preview.Preferences, fileName);
                db.AppendAudit("import_configuration", "settings", "lab", fileName, null, JsonSerializer.Serialize(new
                {
                    rulesAdded = added,
                    rulesUpdated = updated,
                    rulesRemoved = removing.Select(r => r.Antibody).ToList(),
                    rulesKept = removeLocalOnlyRules ? new List<string>() : preview.LocalOnlyRules.Select(r => r.Antibody).ToList(),
                    preferencesChanged = preview.Preferences.Changes.Select(c => c.Name).ToList(),
                }));
            });
            foreach (var change in preview.Preferences.Changes) change.Set(settings);
            return new ConfigurationImportResult(added, updated, removing.Count, preview.Preferences.Changes.Count);
        }

        /// <summary>Both sections and the stored rules the file lacks, for the confirmation prompt.</summary>
        public static string Summarize(ConfigurationPreview p)
        {
            var lines = new List<string> { "RULES", RuleTransfer.Summarize(p.Rules).TrimEnd() };
            if (p.LocalOnlyRules.Count > 0)
                lines.Add($"Stored rule(s) not in the file: {string.Join(", ", p.LocalOnlyRules.Select(r => $"{r.Antibody} '{r.Name}'"))}");
            lines.Add("");
            lines.Add("PREFERENCES");
            lines.Add(PreferenceTransfer.Summarize(p.Preferences));
            return string.Join(Environment.NewLine, lines);
        }
    }
}
