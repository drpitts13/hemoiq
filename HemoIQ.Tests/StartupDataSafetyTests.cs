using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// What runs when the main window loads must neither fail for a user who may not record results nor
/// change laboratory typing without a record.
/// </summary>
[Collection("PersistentDatabase")]
public class StartupDataSafetyTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static void Raw(string dbPath, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string SourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "HemoIQ");
    }

    private static IsolatedDatabase SeededDemoWithUsers()
    {
        var iso = new IsolatedDatabase();
        DemoDataSeeder.Seed(iso.Db);
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Role = UserRole.Technologist });
            iso.Db.SaveUser(new AppUser { Login = "view", Initials = "VW", Role = UserRole.ReadOnly });
        });
        return iso;
    }

    [Fact]
    public void StartupDemoAnalysis_ForAReadOnlyUser_DoesNothingAndDoesNotThrow()
    {
        using var iso = SeededDemoWithUsers();
        As("view", () => Assert.Equal(0, DemoDataSeeder.AnalyzeIfNeverAnalyzed(iso.Db, iso.Analyzer)));
        As("someone-else", () => Assert.Equal(0, DemoDataSeeder.AnalyzeIfNeverAnalyzed(iso.Db, iso.Analyzer)));
        Assert.All(DemoDataSeeder.ScenarioIds, id => Assert.Null(iso.Db.GetSpecimen(id)?.LastAnalyzedAt));
    }

    [Fact]
    public void StartupDemoAnalysis_ForATechnologist_RecordsTheNeverAnalyzedScenarios()
    {
        using var iso = SeededDemoWithUsers();
        int analyzed = 0;
        As("tech", () => analyzed = DemoDataSeeder.AnalyzeIfNeverAnalyzed(iso.Db, iso.Analyzer));
        Assert.True(analyzed > 0);
        As("tech", () => Assert.Equal(0, DemoDataSeeder.AnalyzeIfNeverAnalyzed(iso.Db, iso.Analyzer)));
    }

    [Fact]
    public void BlanketNegativeRepair_LeavesALaboratoryPanelNamedE2EAlone()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Lab panel", "LAB-2026-01", "E2E", 3, null, false, startTypedNegative: true);
        E2EWorkflowSeeder.RepairBlanketNegatives(iso.Db);
        // Kpa negative on every cell is ordinary typing on a real panel; it must stay typed negative.
        Assert.All(iso.Db.GetPanelCells(panel), c => Assert.Equal("-", c.GetTypedValue("Kpa")));
    }

    [Fact]
    public void BlanketNegativeRepair_SkipsASeededLotUsedForAConfirmedIdentification()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);
        var panel = iso.Db.GetAllPanels().Single(p => p.LotNumber == E2EWorkflowSeeder.WarmK);
        foreach (var cell in iso.Db.GetPanelCells(panel.PanelId))
            iso.Db.UpdatePanelCellAntigen(cell.Id, "D", "-");
        Raw(iso.DbPath, $"UPDATE specimens SET final_antibodies = 'anti-K' WHERE accession_number = '{E2EWorkflowSeeder.WarmK}'");

        E2EWorkflowSeeder.RepairBlanketNegatives(iso.Db);

        Assert.All(iso.Db.GetPanelCells(panel.PanelId), c => Assert.Equal("-", c.Antigens["D"]));
    }

    [Fact]
    public void MainWindowLoaded_DoesNotRepairPanelTyping()
    {
        var code = File.ReadAllText(Path.Combine(SourceRoot(), "MainWindow.xaml.cs"));
        Assert.DoesNotContain("RepairBlanketNegatives", code);
    }
}
