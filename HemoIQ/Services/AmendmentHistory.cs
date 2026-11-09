using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>A confirmed identification that was later cleared, from the audit log.</summary>
    public sealed record AmendmentEntry(
        string ClearedAt,
        string ClearedBy,
        string Reason,
        string PreviousIdentification,
        string? PreviouslyConfirmedBy,
        string? PreviouslyConfirmedAt);

    /// <summary>
    /// The amendment record for a specimen's clinical report: every confirmed identification that
    /// was cleared, with who cleared it, when and why, oldest first. Built from
    /// <c>clear_final_call</c> audit entries, which hold the identification as it stood before the
    /// clear, so the report can say what the current identification replaced.
    /// </summary>
    public static class AmendmentHistory
    {
        public const string AmendedBanner = "*** AMENDED REPORT: this identification replaces an earlier one (see Amendment history) ***";
        public const string WithdrawnBanner = "*** PREVIOUS IDENTIFICATION WITHDRAWN: no identification is currently confirmed (see Amendment history) ***";

        public static List<AmendmentEntry> For(DatabaseService db, string accession) =>
            db.GetAuditEventsForEntity("specimen", accession)
                .Where(e => e.Action == "clear_final_call")
                .OrderBy(e => e.Id)
                .Select(Read)
                .ToList();

        /// <summary>The banner for the report header, or null when the identification was never cleared.</summary>
        public static string? Banner(bool hasFinalCall, IReadOnlyCollection<AmendmentEntry> history) =>
            history.Count == 0 ? null : hasFinalCall ? AmendedBanner : WithdrawnBanner;

        public static IEnumerable<string> Lines(IReadOnlyList<AmendmentEntry> history)
        {
            for (var i = 0; i < history.Count; i++)
            {
                var a = history[i];
                var confirmed = a.PreviouslyConfirmedBy == null && a.PreviouslyConfirmedAt == null
                    ? ""
                    : $" (confirmed by {a.PreviouslyConfirmedBy ?? "—"} on {a.PreviouslyConfirmedAt ?? "—"})";
                yield return $"  {i + 1}. {a.ClearedAt}: \"{a.PreviousIdentification}\"{confirmed} cleared by {a.ClearedBy}.";
                yield return $"     Reason: {a.Reason}";
            }
        }

        private static AmendmentEntry Read(Models.AuditEvent e)
        {
            string? previous = null, by = null, at = null;
            if (!string.IsNullOrWhiteSpace(e.BeforeJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(e.BeforeJson);
                    string? Get(string name) =>
                        doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                    previous = Get("FinalAntibodies");
                    by = Get("IdentifiedBy");
                    at = Get("IdentifiedAt");
                }
                catch (JsonException) { /* older entries without a readable snapshot */ }
            }
            return new AmendmentEntry(
                LocalTime(e.OccurredAtUtc),
                string.IsNullOrWhiteSpace(e.Operator) ? "unknown" : e.Operator,
                string.IsNullOrWhiteSpace(e.Reason) ? "not recorded" : e.Reason!,
                string.IsNullOrWhiteSpace(previous) ? "not recorded" : previous!,
                by, at);
        }

        private static string LocalTime(string utc) =>
            DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var t)
                ? t.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : utc;
    }
}
