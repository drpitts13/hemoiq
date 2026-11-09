using System;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Worklist item for an investigation that is awaiting additional testing and has been
    /// open longer than the Preferences turnaround threshold. Only overdue investigations are
    /// listed, so the queue holds the cases that are stuck rather than every open workup.
    /// The item names the testing the last analysis left open. It is a reminder only.
    /// </summary>
    public static class OverdueTesting
    {
        public static WorklistItem? Item(DatabaseService db, Specimen s, int thresholdDays, DateTime now)
        {
            if (s.HasFinalCall || s.IsCancelled || !s.IsActive) return null;
            var received = TurnaroundService.ReceivedTime(s);
            if (received == DateTime.MinValue) return null;
            var days = (now - received).TotalDays;
            if (days <= thresholdDays) return null;

            var status = InvestigationStatusService.Compute(db, s);
            if (status.Label != InvestigationStatusService.AwaitingAdditionalTesting) return null;
            var needs = db.GetLatestAnalysisSnapshot(s.AccessionNumber)?.AdditionalTesting;
            var still = needs is { Count: > 0 } ? " Still needed: " + string.Join(" ", needs) : "";
            return new WorklistItem
            {
                Kind = WorklistKind.OverdueAdditionalTesting,
                KindLabel = "Overdue testing",
                Title = s.AccessionNumber,
                Detail = $"Received {received:yyyy-MM-dd} ({(int)Math.Floor(days)} day(s) ago), past the {thresholdDays}-day turnaround threshold, " +
                         "and still awaiting additional testing." + still,
                UrgencyLabel = "Test or record ID",
                SortOrder = 2,
                AccessionNumber = s.AccessionNumber,
                TargetTab = "Analysis",
            };
        }
    }
}
