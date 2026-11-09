using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class PanelActivationCoverageTests
{
    private static int Lot(IsolatedDatabase iso, string lot, string vendor = "VX", string? productLine = "PL") =>
        iso.Db.AddPanel("Panel " + lot, lot, vendor, 3, null, false, isActive: false, productLine: productLine,
            startTypedNegative: true);

    [Fact]
    public void NewLotThatDropsTyping_IsReported_WithAcsAntigensNamed()
    {
        using var iso = new IsolatedDatabase();
        Lot(iso, "OLD-1");
        var next = Lot(iso, "NEW-1");
        var cells = iso.Db.GetPanelCells(next);
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "Fya", "NT");
        foreach (var c in cells) iso.Db.UpdatePanelCellAntigen(c.Id, "Lua", "NT");

        var coverage = PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(next)!)!;
        Assert.Equal("lot OLD-1", coverage.PriorLabel);
        Assert.Equal(new[] { "Fya (2 of 3 cells)", "Lua (0 of 3 cells)" }, coverage.Gaps);
        Assert.Equal(new[] { "Fya" }, coverage.AcsAntigensAffected);
        Assert.StartsWith("Compared with the previous VX lot (lot OLD-1), this lot does not type every cell for: Fya (2 of 3 cells), Lua (0 of 3 cells).",
            coverage.Message);
        Assert.Contains("anti-Fya is on the ACS list, so fewer cells will be available to rule it out.", coverage.Message);
        Assert.EndsWith("Untyped cells stay NT and are never used as antigen-negative.", coverage.Message);
    }

    [Fact]
    public void ActivatingAnyway_RecordsTheAcknowledgedGaps_InTheAudit()
    {
        using var iso = new IsolatedDatabase();
        var old = Lot(iso, "OLD-2");
        var next = Lot(iso, "NEW-2");
        foreach (var c in iso.Db.GetPanelCells(next)) iso.Db.UpdatePanelCellAntigen(c.Id, "Jkb", "NT");
        var coverage = PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(next)!)!;

        iso.Db.SetPanelActive(next, true, coverage);

        Assert.True(iso.Db.GetPanel(next)!.IsActive);
        var audit = Assert.Single(iso.Db.GetAuditEventsForEntity("panel", next.ToString()), e => e.Action == "activate_panel");
        Assert.Equal("Activated with coverage gaps against lot OLD-2", audit.Reason);
        Assert.Contains($"\"priorPanelId\":{old}", audit.AfterJson);
        Assert.Contains("Jkb (0 of 3 cells)", audit.AfterJson);
    }

    [Fact]
    public void NoReport_WhenCoverageMatches_OrThereIsNoComparableLot()
    {
        using var iso = new IsolatedDatabase();
        Lot(iso, "OLD-3");
        var same = Lot(iso, "NEW-3");
        Assert.Null(PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(same)!));

        var other = Lot(iso, "SOLO", vendor: "OtherVendor");
        Assert.Null(PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(other)!));

        var noVendor = iso.Db.AddPanel("Hand-made", "HM-1", null, 3, null, false, startTypedNegative: true);
        Assert.Null(PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(noVendor)!));

        // A plain activation keeps the original audit shape.
        iso.Db.SetPanelActive(same, true);
        var audit = Assert.Single(iso.Db.GetAuditEventsForEntity("panel", same.ToString()), e => e.Action == "activate_panel");
        Assert.Null(audit.Reason);
        Assert.Equal("{\"active\":true}", audit.AfterJson);
    }

    [Fact]
    public void AnAddedAntigen_IsNotAGap()
    {
        using var iso = new IsolatedDatabase();
        var old = Lot(iso, "OLD-4");
        foreach (var c in iso.Db.GetPanelCells(old)) iso.Db.UpdatePanelCellAntigen(c.Id, "Lub", "NT");
        var next = Lot(iso, "NEW-4");
        Assert.Null(PanelActivationCoverage.Check(iso.Db, iso.Db.GetPanel(next)!));
    }
}
