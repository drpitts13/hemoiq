using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>
    /// Warnings a technologist chose to confirm an identification past. Confirm ID warns when the
    /// analysis is missing or out of date, when the analysis left testing open (antibodies not yet
    /// ruled out, ID-rule or separation shortfalls), and when the multiple-antibody separation
    /// criterion is not met. Confirming anyway is allowed; it is recorded with the confirmation
    /// (in the confirm_final_call audit entry) and shown on the Timeline, the worksheet and the
    /// supervisor-review Worklist item, so a reviewer sees what was overridden.
    /// </summary>
    public static class ConfirmationOverrides
    {
        public static string? StaleAnalysis(string? staleReason, bool neverAnalyzed) =>
            neverAnalyzed ? "Confirmed without an analysis."
            : staleReason != null ? "Confirmed with an out-of-date analysis: " + staleReason
            : null;

        /// <summary>Confirm ID prompt when the latest analysis left testing open; null when it left none.</summary>
        public static string? OpenTestingPrompt(IReadOnlyList<string>? needs) =>
            needs is { Count: > 0 }
                ? "The latest analysis left testing open:\n\n" + string.Join("\n", needs.Select(n => "• " + n)) +
                  "\n\nConfirm this identification anyway? The open items are recorded with the confirmation."
                : null;

        public static string OpenTesting(IReadOnlyList<string> needs) =>
            "Confirmed with testing still open: " + string.Join(" ", needs);

        public static string SeparationShortfall(IEnumerable<string> antibodies) =>
            "Confirmed although the multiple-antibody separation criterion was not met for " + string.Join(", ", antibodies) + ".";

        /// <summary>
        /// Overrides recorded with the specimen's current confirmation, oldest first. Empty when the
        /// specimen has no confirmed identification or it was confirmed without overriding a warning.
        /// </summary>
        public static List<string> ForCurrentConfirmation(DatabaseService db, string accession)
        {
            var s = db.GetSpecimen(accession);
            if (s == null || !s.HasFinalCall) return new List<string>();
            var latest = db.GetAuditEventsForEntity("specimen", accession)
                .Where(e => e.Action == "confirm_final_call")
                .OrderBy(e => e.Id)
                .LastOrDefault();
            return Read(latest?.AfterJson);
        }

        public static List<string> Read(string? confirmJson)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(confirmJson)) return list;
            try
            {
                using var doc = JsonDocument.Parse(confirmJson);
                if (doc.RootElement.TryGetProperty("overrides", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var item in arr.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is string text && text.Length > 0)
                            list.Add(text);
            }
            catch (JsonException) { }
            return list;
        }
    }
}
