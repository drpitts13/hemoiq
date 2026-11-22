using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Demo specimens and lots look like real ones, so Load Demo Data must never mix them into a laboratory database,
/// and a load that fails part-way must leave nothing behind.
/// </summary>
[Collection("PersistentDatabase")]
public class DemoDataGuardTests
{
    [Fact]
    public void EmptyDatabase_CanLoad_AndThenReload()
    {
        using var iso = new IsolatedDatabase();
        Assert.Null(DemoDataGuard.RefusalReason(iso.Db));

        DemoDataGuard.Load(iso.Db, iso.Analyzer);
        Assert.NotNull(iso.Db.GetSpecimen(ClinicalDataSeeder.MarkerAccession));
        Assert.All(iso.Db.GetAllSpecimens(), s => Assert.Contains(s.AccessionNumber, DemoDataGuard.SeededAccessions));

        // A demo-only database may be loaded again to fill in any missing scenario.
        Assert.Null(DemoDataGuard.RefusalReason(iso.Db));
    }

    [Fact]
    public void DatabaseWithRealSpecimens_IsRefused_AndNothingIsAdded()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("LAB-24-0001", "serum", null);
        var panelsBefore = iso.Db.GetAllPanels().Count;

        var reason = DemoDataGuard.RefusalReason(iso.Db);
        Assert.NotNull(reason);
        Assert.Contains("1 specimen(s) that are not demo records (for example LAB-24-0001)", reason);
        Assert.Contains("HEMOIQ_DB", reason);

        Assert.Throws<InvalidOperationException>(() => DemoDataGuard.Load(iso.Db, iso.Analyzer));
        Assert.Single(iso.Db.GetAllSpecimens());
        Assert.Equal(panelsBefore, iso.Db.GetAllPanels().Count);
    }

    [Fact]
    public void DatabaseWithOnlyLabLots_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddPanel("Lab lot", "REAL-1", "Vendor", 3, null, false);
        Assert.Contains("1 panel lot(s) and no demo specimens", DemoDataGuard.RefusalReason(iso.Db));
    }

    [Fact]
    public void LoadThatFailsPartWay_LeavesNothingBehind()
    {
        using var iso = new IsolatedDatabase();
        // A record that happens to use a demo accession: the guard cannot tell, so the seeding itself fails
        // on the duplicate. The transaction must roll back every specimen and lot added before it.
        iso.Db.AddSpecimen("2026-005", "serum", null);

        Assert.ThrowsAny<Exception>(() => DemoDataGuard.Load(iso.Db, iso.Analyzer));

        Assert.Equal(new[] { "2026-005" }, iso.Db.GetAllSpecimens().Select(s => s.AccessionNumber));
        Assert.Empty(iso.Db.GetAllPanels());
    }

    [Fact]
    public void LoadDemoDataCommand_GoesThroughTheGuard()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "MainWindow.xaml.cs"));
        var method = src[src.IndexOf("private void LoadDemoData()", StringComparison.Ordinal)..];
        method = method[..method.IndexOf("private void ShowAuditLog()", StringComparison.Ordinal)];
        Assert.Contains("DemoDataGuard.RefusalReason(", method);
        Assert.Contains("DemoDataGuard.Load(", method);
        Assert.DoesNotContain("ClinicalDataSeeder.SeedIfNeeded(", method);
    }
}
