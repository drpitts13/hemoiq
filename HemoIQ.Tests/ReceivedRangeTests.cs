using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// Analytics figures described as "received in the range" count investigations by their recorded receipt time,
/// falling back to the entry date only when no receipt time is recorded.
/// </summary>
public class ReceivedRangeTests
{
    private static readonly DateTime From = new(2026, 11, 1);
    private static readonly DateTime To = new(2026, 11, 30);

    // Entered in November but received in October: outside a November range.
    private static Specimen ReceivedBefore() => new()
    {
        AccessionNumber = "R-OCT", CreatedDate = "2026-11-02", ReceivedAt = "2026-10-28 08:00", OrderingLocation = "ICU",
        FinalAntibodies = "anti-K", IdentifiedAt = "2026-11-03 10:00",
    };

    // Entered in October but received in November: inside a November range.
    private static Specimen ReceivedInside() => new()
    {
        AccessionNumber = "R-NOV", CreatedDate = "2026-10-30", ReceivedAt = "2026-11-02 08:00", OrderingLocation = "ICU",
        FinalAntibodies = "anti-E", IdentifiedAt = "2026-11-04 08:00",
    };

    // No receipt time recorded: the entry date decides.
    private static Specimen EntryDateOnly() => new()
    {
        AccessionNumber = "R-ENTRY", CreatedDate = "2026-11-10", OrderingLocation = "ICU",
    };

    [Fact]
    public void LocationWorkload_CountsByReceiptDate()
    {
        var rows = WorkloadService.ComputeByLocation(new[] { ReceivedBefore(), ReceivedInside(), EntryDateOnly() }, From, To);

        var icu = Assert.Single(rows);
        Assert.Equal(2, icu.Received);
        Assert.Equal(1, icu.Confirmed);
        var cases = WorkloadService.CasesForLocation(new[] { ReceivedBefore(), ReceivedInside(), EntryDateOnly() }, From, To, "ICU");
        Assert.Equal(new[] { "R-ENTRY", "R-NOV" }, cases.Select(c => c.Accession).OrderBy(a => a, StringComparer.Ordinal));
    }

    [Fact]
    public void Turnaround_CountsByReceiptDate()
    {
        var t = TurnaroundService.Compute(new[] { ReceivedBefore(), ReceivedInside() }, From, To, 3, new DateTime(2026, 11, 20));

        Assert.Equal(1, t.ReceivedToConfirmed.Count);
        Assert.Equal(2.0, t.ReceivedToConfirmed.Median!.Value, 3);
    }
}
