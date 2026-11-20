using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Identifications confirmed past one or more Confirm ID warnings (stale or missing analysis,
    /// testing still open, separation shortfall), for the Analytics "manual overrides" metric.
    /// Counts each such confirmation event in the date range by when it was confirmed, including
    /// ones later cleared (an override that happened is still an override); the detail says when
    /// the confirmation is no longer current. Read from the audit log only.
    /// </summary>
    public static class OverrideAnalytics
    {
        /// <param name="login">When set, only overrides by this Windows login (as in Activity by user).</param>
        public static List<AnalyticsCase> Cases(DatabaseService db, DateTime? from, DateTime? to, string? login = null) =>
            Cases(db.GetAuditEventsByAction(new[] { "confirm_final_call", "save_override" }), from, to,
                acc => db.GetSpecimen(acc), login);

        public static List<AnalyticsCase> Cases(IEnumerable<AuditEvent> confirmations, DateTime? from, DateTime? to,
            Func<string, Specimen?> specimen, string? login = null)
        {
            var all = confirmations.ToList();
            bool ByLogin(AuditEvent e) =>
                login == null || string.Equals((e.Operator ?? "").Trim(), login.Trim(), StringComparison.OrdinalIgnoreCase);
            var events = all.Where(e => e.Action == "confirm_final_call" && e.EntityId != null).ToList();
            var latestByAccession = events
                .GroupBy(e => e.EntityId!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Max(e => e.Id), StringComparer.Ordinal);
            var cases = new List<AnalyticsCase>();
            // "Current" is decided over everyone's confirmations; the login filter applies after.
            foreach (var e in events.Where(ByLogin).OrderByDescending(e => e.Id))
            {
                var overrides = ConfirmationOverrides.Read(e.AfterJson);
                if (overrides.Count == 0 || !UserActivityService.InRange(e.OccurredAtUtc, from, to)) continue;
                var s = specimen(e.EntityId!);
                var current = s != null && s.HasFinalCall && latestByAccession[e.EntityId!] == e.Id;
                cases.Add(new AnalyticsCase
                {
                    Accession = e.EntityId!,
                    Date = LocalDate(e.OccurredAtUtc),
                    FinalAntibodies = Prop(e.AfterJson, "antibodies") ?? string.Empty,
                    Detail = (current ? "" : "No longer the current identification. ") +
                             $"{overrides.Count} warning(s) overridden by {Prop(e.AfterJson, "identifiedBy") ?? "—"}: " +
                             string.Join(" ", overrides),
                });
            }
            // Specimen saves past a data-quality warning (encounter on another patient, pregnancy for a male patient).
            foreach (var e in all.Where(e => e.Action == "save_override" && e.EntityId != null && !string.IsNullOrWhiteSpace(e.Reason) && ByLogin(e)))
            {
                if (!UserActivityService.InRange(e.OccurredAtUtc, from, to)) continue;
                cases.Add(new AnalyticsCase
                {
                    Accession = e.EntityId!,
                    Date = LocalDate(e.OccurredAtUtc),
                    FinalAntibodies = specimen(e.EntityId!)?.FinalAntibodies ?? string.Empty,
                    Detail = $"Specimen saved past a warning by {(string.IsNullOrWhiteSpace(e.Operator) ? "—" : e.Operator)}: {e.Reason}",
                });
            }
            return cases.OrderByDescending(c => c.Date, StringComparer.Ordinal).ToList();
        }

        private static string LocalDate(string utcText) =>
            DateTime.TryParse(utcText, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var t)
                ? (t.Kind == DateTimeKind.Utc ? t.ToLocalTime() : t).ToString("yyyy-MM-dd")
                : utcText;

        private static string? Prop(string? json, string name)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            }
            catch (JsonException) { return null; }
        }
    }
}
