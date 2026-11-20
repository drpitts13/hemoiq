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
    public sealed class PreferenceChange
    {
        public string Name { get; init; } = string.Empty;
        public string Before { get; init; } = string.Empty;
        public string After { get; init; } = string.Empty;
        internal Action<LabSettings> Set { get; init; } = _ => { };
    }

    public sealed class PreferencePreview
    {
        public List<PreferenceChange> Changes { get; } = new();
        /// <summary>Settings in the file that are out of range or the wrong type, with the reason. They are not applied.</summary>
        public List<string> Refused { get; } = new();
        /// <summary>Names in the file this version does not transfer (for example per-workstation display settings).</summary>
        public List<string> Ignored { get; } = new();
        public int Unchanged { get; set; }
    }

    /// <summary>
    /// Copies the laboratory-wide Preferences (lab name, analysis rule settings, specimen and
    /// transfusion windows, review and turnaround settings, extra phases) between
    /// installations as JSON. Per-workstation settings (Worklist filters, default initials,
    /// display toggles, database size) are not transferred. Import is preview first, then an
    /// explicit apply. Out-of-range values are refused, never clamped silently.
    /// </summary>
    public static class PreferenceTransfer
    {
        public const string Format = "hemoiq-preferences";
        public const int Version = 1;

        private sealed record Spec(string Name, Func<LabSettings, object> Get, Func<JsonNode, LabSettings, string?> TrySet);

        private static Spec Int(string name, int min, int max, Func<LabSettings, int> get, Action<LabSettings, int> set) =>
            new(name, s => get(s), (node, s) =>
            {
                if (node is not JsonValue v || !v.TryGetValue<int>(out var n)) return "must be a whole number";
                if (n < min || n > max) return $"{n} is outside {min}–{max}";
                set(s, n);
                return null;
            });

        private static Spec Bool(string name, Func<LabSettings, bool> get, Action<LabSettings, bool> set) =>
            new(name, s => get(s), (node, s) =>
            {
                if (node is not JsonValue v || !v.TryGetValue<bool>(out var b)) return "must be true or false";
                set(s, b);
                return null;
            });

        private static Spec Text(string name, int maxLength, Func<LabSettings, string> get, Action<LabSettings, string> set,
            Func<string, string?>? check = null) =>
            new(name, s => get(s), (node, s) =>
            {
                if (node is not JsonValue v || !v.TryGetValue<string>(out var t)) return "must be text";
                t = t.Trim();
                if (t.Length > maxLength) return $"is longer than {maxLength} characters";
                if (check?.Invoke(t) is string problem) return problem;
                set(s, t);
                return null;
            });

        private static readonly Spec[] Specs =
        {
            Text("labName", 120, s => s.LabName, (s, v) => s.LabName = v, v => v.Length == 0 ? "cannot be blank" : null),
            Text("department", 120, s => s.Department, (s, v) => s.Department = v),
            Text("facilityName", LabSettings.FacilityNameMaxLength, s => s.FacilityName, (s, v) => s.FacilityName = v),
            Text("facilityAddress", LabSettings.FacilityAddressMaxLength, s => s.FacilityAddress,
                (s, v) => s.FacilityAddress = LabSettings.NormalizeMultiline(v)),
            Text("facilityContact", LabSettings.FacilityContactMaxLength, s => s.FacilityContact,
                (s, v) => s.FacilityContact = LabSettings.NormalizeMultiline(v)),
            new("probabilityThreshold", s => s.ProbabilityThreshold, (node, s) =>
            {
                if (node is not JsonValue v || !v.TryGetValue<double>(out var d)) return "must be a number";
                if (d < 0.3 || d > 0.95) return $"{d.ToString(CultureInfo.InvariantCulture)} is outside 0.3–0.95";
                s.ProbabilityThreshold = d;
                return null;
            }),
            Int("identificationCellCount", 1, 3, s => s.IdentificationCellCount, (s, v) => s.IdentificationCellCount = v),
            Int("acsRuleoutCount", 1, 5, s => s.AcsRuleoutCount, (s, v) => s.AcsRuleoutCount = v),
            Text("acsAdditionalAntigens", 400, s => s.AcsAdditionalAntigens, (s, v) =>
            {
                AntigenConstants.TryParseAcsAdditions(v, out var normalized, out _);
                s.AcsAdditionalAntigens = normalized;
            }, v => AntigenConstants.TryParseAcsAdditions(v, out _, out var problem) ? null : problem),
            Int("separationCellCount", 0, 5, s => s.SeparationCellCount, (s, v) => s.SeparationCellCount = v),
            Int("defaultMinRuleoutCount", 1, 5, s => s.DefaultMinRuleoutCount, (s, v) => s.DefaultMinRuleoutCount = v),
            Text("defaultSpecimenType", 40,s => s.DefaultSpecimenType, (s, v) => s.DefaultSpecimenType = v,
                v => v is "serum" or "plasma" or "eluate" ? null : "must be serum, plasma or eluate"),
            Int("expirationWarningDays", 1, 90, s => s.ExpirationWarningDays, (s, v) => s.ExpirationWarningDays = v),
            Int("defaultSpecimenDatingDays", 0, 14, s => s.DefaultSpecimenDatingDays, (s, v) => s.DefaultSpecimenDatingDays = v),
            Int("recentTransfusionWindowDays", 1, 365, s => s.RecentTransfusionWindowDays, (s, v) => s.RecentTransfusionWindowDays = v),
            Bool("requireSupervisorReview", s => s.RequireSupervisorReview, (s, v) => s.RequireSupervisorReview = v),
            Int("turnaroundThresholdDays", 1, 60, s => s.TurnaroundThresholdDays, (s, v) => s.TurnaroundThresholdDays = v),
            Int("datingTransfusionLookbackDays", 1, 365, s => s.DatingTransfusionLookbackDays, (s, v) => s.DatingTransfusionLookbackDays = v),
            Int("rhigPassiveWindowDays", 1, 365, s => s.RhigPassiveWindowDays, (s, v) => s.RhigPassiveWindowDays = v),
            Text("extraPhases", 400, s => s.ExtraPhases, (s, v) => s.ExtraPhases = ExtraPhaseParser.NormalizeList(v)),
        };

        public static string Export(LabSettings settings, DateTime now)
        {
            var values = new JsonObject();
            foreach (var spec in Specs)
                values[spec.Name] = spec.Get(settings) switch
                {
                    int i => JsonValue.Create(i),
                    double d => JsonValue.Create(d),
                    bool b => JsonValue.Create(b),
                    var o => JsonValue.Create(Convert.ToString(o, CultureInfo.InvariantCulture)),
                };
            var root = new JsonObject
            {
                ["format"] = Format,
                ["version"] = Version,
                ["exportedAt"] = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["settings"] = values,
            };
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>Reads a Preferences file and lists the changes against <paramref name="current"/>. Writes nothing.</summary>
        public static PreferencePreview Preview(string json, LabSettings current)
        {
            JsonNode? root;
            try { root = JsonNode.Parse(json); }
            catch (JsonException ex) { throw new ArgumentException("The file is not valid JSON: " + ex.Message); }
            if (root is not JsonObject obj || (string?)obj["format"] != Format)
                throw new ArgumentException($"The file is not a HemoIQ Preferences export (expected format \"{Format}\").");
            if (obj["version"] is JsonValue ver && ver.TryGetValue<int>(out var version) && version > Version)
                throw new ArgumentException($"The file is Preferences export version {version}; this version of HemoIQ reads version {Version} or earlier.");
            if (obj["settings"] is not JsonObject values)
                throw new ArgumentException("The file has no settings.");

            var preview = new PreferencePreview();
            foreach (var (name, node) in values)
            {
                var spec = Specs.FirstOrDefault(s => s.Name == name);
                if (spec == null) { preview.Ignored.Add(name); continue; }
                var trial = Copy(current);
                var problem = node == null ? "has no value" : spec.TrySet(node, trial);
                if (problem != null)
                {
                    preview.Refused.Add($"{name}: {problem}.");
                    continue;
                }
                var before = Show(spec.Get(current));
                var after = Show(spec.Get(trial));
                if (before == after) { preview.Unchanged++; continue; }
                var captured = node.DeepClone();
                preview.Changes.Add(new PreferenceChange
                {
                    Name = name, Before = before, After = after,
                    Set = s => spec.TrySet(captured, s),
                });
            }
            return preview;
        }

        /// <summary>
        /// Applies the previewed changes to <paramref name="settings"/> and audits them. Analysis
        /// settings that change are audited as update_settings, so open analyses become stale exactly
        /// as after a Preferences edit; every import is also audited as import_preferences with all
        /// changed values. Needs the Preferences permission once users are registered.
        /// </summary>
        public static void Apply(DatabaseService db, LabSettings settings, PreferencePreview preview, string? fileName)
        {
            AccessControl.Demand(db, Permission.ConfigurePreferences);
            if (preview.Changes.Count == 0) return;
            var clinicalBefore = JsonSerializer.Serialize(settings.ClinicalSnapshot());
            var before = preview.Changes.ToDictionary(c => c.Name, c => c.Before);
            foreach (var change in preview.Changes) change.Set(settings);
            var clinicalAfter = JsonSerializer.Serialize(settings.ClinicalSnapshot());
            if (clinicalBefore != clinicalAfter)
                db.AppendAudit("update_settings", "settings", "lab", "Imported from " + (fileName ?? "a Preferences file"),
                    clinicalBefore, clinicalAfter);
            db.AppendAudit("import_preferences", "settings", "lab", fileName,
                JsonSerializer.Serialize(before), JsonSerializer.Serialize(preview.Changes.ToDictionary(c => c.Name, c => c.After)));
        }

        public static string Summarize(PreferencePreview p)
        {
            var lines = new List<string>
            {
                $"{p.Changes.Count} to change, {p.Unchanged} unchanged, {p.Refused.Count} refused, {p.Ignored.Count} not transferred.",
            };
            lines.AddRange(p.Changes.Select(c => $"Change {c.Name}: {c.Before} → {c.After}"));
            lines.AddRange(p.Refused.Select(r => "Refused " + r));
            if (p.Ignored.Count > 0) lines.Add("Not transferred: " + string.Join(", ", p.Ignored));
            return string.Join(Environment.NewLine, lines);
        }

        private static string Show(object value) => value switch
        {
            double d => d.ToString("0.###", CultureInfo.InvariantCulture),
            bool b => b ? "yes" : "no",
            string s => s.Length == 0 ? "(blank)" : s,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
        };

        private static LabSettings Copy(LabSettings s) =>
            JsonSerializer.Deserialize<LabSettings>(JsonSerializer.Serialize(s))!;
    }
}
