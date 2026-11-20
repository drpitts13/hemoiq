using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class OverdueTestingTests
{
    /// <summary>One reactive K+ cell and one nonreactive k+ cell: anti-K is short of the 3 + 3 rule, so testing is left open.</summary>
    private static void SeedAwaitingTesting(IsolatedDatabase iso, string acc, int receivedDaysAgo)
    {
        iso.Db.AddSpecimen(acc, "serum", null);
        var received = DateTime.Now.AddDays(-receivedDaysAgo).ToString("yyyy-MM-dd HH:mm");
        iso.Db.UpdateSpecimenCollection(acc, received, received);
        var panel = iso.Db.AddPanel("OT " + acc, "OT-" + acc, "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "K", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "k", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "k", "+");
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction(acc, panel, "2", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
    }

    [Fact]
    public void PastThreshold_AndAwaitingTesting_IsListedWithNeeds_OnTheWorklist()
    {
        using var iso = new IsolatedDatabase();
        SeedAwaitingTesting(iso, "OT-1", receivedDaysAgo: 10);
        var s = iso.Db.GetSpecimen("OT-1")!;
        Assert.Equal(InvestigationStatusService.AwaitingAdditionalTesting, InvestigationStatusService.Compute(iso.Db, s).Label);

        var item = OverdueTesting.Item(iso.Db, s, 3, DateTime.Now);
        Assert.NotNull(item);
        Assert.Equal(WorklistKind.OverdueAdditionalTesting, item!.Kind);
        Assert.Equal("Analysis", item.TargetTab);
        Assert.Contains("(10 day(s) ago), past the 3-day turnaround threshold", item.Detail);
        Assert.Contains("Still needed: ", item.Detail);
        Assert.Contains("anti-K", item.Detail);

        Assert.Contains(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.OverdueAdditionalTesting && i.Title == "OT-1");
    }

    [Fact]
    public void WithinThreshold_IsNotListed()
    {
        using var iso = new IsolatedDatabase();
        SeedAwaitingTesting(iso, "OT-2", receivedDaysAgo: 1);
        var s = iso.Db.GetSpecimen("OT-2")!;
        Assert.Null(OverdueTesting.Item(iso.Db, s, 3, DateTime.Now));
        Assert.DoesNotContain(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.OverdueAdditionalTesting);
    }

    [Fact]
    public void OtherStatuses_ConfirmedCancelledOrInactive_AreNotListed()
    {
        using var iso = new IsolatedDatabase();
        // Overdue but still testing in progress: a panel with no grades.
        iso.Db.AddSpecimen("OT-3", "serum", null);
        iso.Db.UpdateSpecimenCollection("OT-3", null, DateTime.Now.AddDays(-9).ToString("yyyy-MM-dd HH:mm"));
        var panel = iso.Db.AddPanel("OT P3", "OT-P3", "V", 2, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel("OT-3", panel);
        Assert.Null(OverdueTesting.Item(iso.Db, iso.Db.GetSpecimen("OT-3")!, 3, DateTime.Now));

        SeedAwaitingTesting(iso, "OT-4", receivedDaysAgo: 10);
        iso.Db.SetSpecimenFinalCall("OT-4", "anti-K", null, "TE");
        Assert.Null(OverdueTesting.Item(iso.Db, iso.Db.GetSpecimen("OT-4")!, 3, DateTime.Now));

        SeedAwaitingTesting(iso, "OT-5", receivedDaysAgo: 10);
        var five = iso.Db.GetSpecimen("OT-5")!;
        five.IsActive = false;
        Assert.Null(OverdueTesting.Item(iso.Db, five, 3, DateTime.Now));
        five.IsActive = true;
        five.CancelledAt = "2026-01-01 00:00";
        Assert.Null(OverdueTesting.Item(iso.Db, five, 3, DateTime.Now));
    }

    [Fact]
    public void ThresholdIsHonored_AndReceivedFallsBackToCreatedDate()
    {
        using var iso = new IsolatedDatabase();
        SeedAwaitingTesting(iso, "OT-6", receivedDaysAgo: 5);
        var s = iso.Db.GetSpecimen("OT-6")!;
        Assert.NotNull(OverdueTesting.Item(iso.Db, s, 4, DateTime.Now));
        Assert.Null(OverdueTesting.Item(iso.Db, s, 7, DateTime.Now));

        // No received time recorded: the created date is used, so a later "now" makes it overdue.
        s.ReceivedAt = null;
        Assert.Null(OverdueTesting.Item(iso.Db, s, 3, DateTime.Now));
        Assert.NotNull(OverdueTesting.Item(iso.Db, s, 3, DateTime.Now.AddDays(4)));
    }
}
