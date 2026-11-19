using AntibodyPanels.Data;
using AntibodyPanels.Models;
using AntibodyPanels.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace AntibodyPanels.Tests;

public class DatabaseArchiveTests
{
    [Fact]
    public void InspectArchive_ListsPurgedSpecimensAndFlagsLiveOnes()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out var panelId, out _);
        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            var inspection = iso.Db.InspectArchive(archivePath);

            Assert.Equal(1, inspection.SpecimenCount);
            Assert.Equal(1, inspection.RestorableCount);
            Assert.Equal(0, inspection.AlreadyInLiveCount);
            Assert.Contains(inspection.Specimens, s => s.AccessionNumber == "OLD-SPEC" && !s.ExistsInLive);
            Assert.Equal("2020-01-15", inspection.EarliestCreatedDate);
            Assert.True(inspection.PanelCount >= 1);
            Assert.True(inspection.FileBytes > 0);
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void InspectArchive_MarksAccessionsAlreadyInLive()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out _, out _);
        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            iso.Db.RestoreArchive(archivePath);
            var inspection = iso.Db.InspectArchive(archivePath);

            Assert.Equal(1, inspection.AlreadyInLiveCount);
            Assert.Equal(0, inspection.RestorableCount);
            Assert.True(inspection.Specimens[0].ExistsInLive);
            Assert.Equal("Already in database", inspection.Specimens[0].Status);
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void InspectArchive_ShowsStoredAntibodiesDatAndNotes()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Warm Panel", "L1", "V", 1, null, false);
        iso.Db.AddSpecimen("E2E-WARM-AUTO", "serum", null, null, null, null, null, "Positive", "2020-01-15");
        iso.Db.AddSpecimen("E2E-WARM-K", "serum", null, null, null, null, null, null, "2020-01-16");
        iso.Db.AddSpecimen("E2E-WARM-AUTO-ALLOS", "serum", null, null,
            "Warm autoantibody with possible underlying alloantibodies.", null, null, "Positive", "2020-01-17");
        iso.Db.LinkSpecimenPanel("E2E-WARM-AUTO", panelId);
        iso.Db.AddSpecimenAntibody("E2E-WARM-K", "anti-K", 0.8);
        iso.Db.AddSpecimenAntibody("E2E-WARM-AUTO-ALLOS", "anti-E", 0.7);
        iso.Db.AddSpecimenAntibody("E2E-WARM-AUTO-ALLOS", "anti-K", 0.6);
        iso.Db.SetSpecimenFinalCall("E2E-WARM-K", "anti-K", null, "DP");

        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            var inspection = iso.Db.InspectArchive(archivePath);
            var warm = inspection.Specimens.Single(s => s.AccessionNumber == "E2E-WARM-AUTO");
            var kell = inspection.Specimens.Single(s => s.AccessionNumber == "E2E-WARM-K");
            var allos = inspection.Specimens.Single(s => s.AccessionNumber == "E2E-WARM-AUTO-ALLOS");
            Assert.Equal("DAT Positive", warm.Recorded);
            Assert.Equal("anti-K", kell.Recorded);
            Assert.Contains("anti-E; anti-K", allos.Recorded);
            Assert.Contains("DAT Positive", allos.Recorded);
            Assert.Contains("underlying alloantibodies", allos.Recorded);
            Assert.DoesNotContain("anti-E", kell.Recorded);
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void InspectArchive_RefusesLiveDatabasePath()
    {
        using var iso = new IsolatedDatabase();
        Assert.Throws<InvalidOperationException>(() => iso.Db.InspectArchive(iso.Db.DbPath));
    }

    [Fact]
    public void InspectArchive_RejectsFileWithoutSpecimensTable()
    {
        using var iso = new IsolatedDatabase();
        var path = Path.Combine(Path.GetTempPath(), $"not_archive_{Guid.NewGuid():N}.db");
        try
        {
            using (var conn = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE foo (id INTEGER)";
                cmd.ExecuteNonQuery();
            }

            var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.InspectArchive(path));
            Assert.Contains("archive", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void RestoreArchive_BringsBackPurgedSpecimenAndReactions()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out var panelId, out _);
        iso.Db.SaveReaction("OLD-SPEC", panelId, "1", "0", "0", "2+", "2+");
        iso.Db.AddSpecimenAntibody("OLD-SPEC", "K", 0.9);

        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            Assert.Null(iso.Db.GetSpecimen("OLD-SPEC"));

            var result = iso.Db.RestoreArchive(archivePath);
            Assert.Equal(1, result.SpecimensRestored);
            Assert.Equal(0, result.SpecimensSkipped);
            Assert.NotNull(iso.Db.GetSpecimen("OLD-SPEC"));
            Assert.Equal("2020-01-15", iso.Db.GetSpecimen("OLD-SPEC")!.CreatedDate);
            Assert.NotEmpty(iso.Db.GetAllSpecimenReactions("OLD-SPEC"));
            Assert.NotEmpty(iso.Db.GetReactions("OLD-SPEC", panelId));
            Assert.Contains(iso.Db.GetSpecimenAntibodies("OLD-SPEC"), a => a.Antibody == "K");
            Assert.Contains(iso.Db.GetSpecimenPanels("OLD-SPEC"), p => p.PanelId == panelId);
            Assert.NotNull(iso.Db.GetSpecimen("NEW-SPEC"));
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void RestoreArchive_SkipsAccessionsAlreadyPresent()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out _, out _);
        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            iso.Db.RestoreArchive(archivePath);

            var second = iso.Db.RestoreArchive(archivePath);
            Assert.Equal(0, second.SpecimensRestored);
            Assert.Equal(1, second.SpecimensSkipped);
            Assert.Single(iso.Db.GetAllSpecimens().Where(s => s.AccessionNumber == "OLD-SPEC"));
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void RestoreArchive_RestoresPanelDeletedFromLive()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out var usedPanelId, out var unusedPanelId);
        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            iso.Db.DeletePanel(usedPanelId);
            Assert.Null(iso.Db.GetPanel(usedPanelId));
            Assert.NotNull(iso.Db.GetPanel(unusedPanelId));

            var result = iso.Db.RestoreArchive(archivePath);
            Assert.Equal(1, result.SpecimensRestored);
            Assert.True(result.PanelsRestored >= 1);
            Assert.NotNull(iso.Db.GetPanel(usedPanelId));
            Assert.NotNull(iso.Db.GetSpecimen("OLD-SPEC"));
            Assert.Contains(iso.Db.GetSpecimenPanels("OLD-SPEC"), p => p.PanelId == usedPanelId);
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void RestoreArchive_RestoresAnalysisSnapshot()
    {
        using var iso = new IsolatedDatabase();
        Seed(iso, out _, out _);
        const string settings =
            "{\"ProbabilityThreshold\":0.5,\"IdentificationCellCount\":3,\"AcsRuleoutCount\":3,\"DefaultMinRuleoutCount\":1,\"ExtraPhases\":\"\"}";
        iso.Db.SaveAnalysisSnapshot(
            "OLD-SPEC", "2.0.0", settings, "fp-old",
            "[\"anti-E\"]", "{\"anti-K\":0.9}", null,
            "v1", "[]", "DP", "{\"note\":\"stored\"}");
        iso.Db.SaveAnalysisSnapshot(
            "NEW-SPEC", "2.0.0", settings, "fp-new",
            null, null, null);

        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            Assert.Null(iso.Db.GetLatestAnalysisSnapshot("OLD-SPEC"));
            Assert.Equal("fp-new", iso.Db.GetLatestAnalysisSnapshot("NEW-SPEC")!.InputFingerprint);

            var result = iso.Db.RestoreArchive(archivePath);
            Assert.Equal(1, result.SpecimensRestored);
            var snap = iso.Db.GetLatestAnalysisSnapshot("OLD-SPEC");
            Assert.NotNull(snap);
            Assert.Equal(settings, snap!.SettingsJson);
            Assert.Equal("fp-old", snap.InputFingerprint);
            Assert.Equal("[\"anti-E\"]", snap.RuledOutJson);
            Assert.Contains("anti-K", snap.SuspectedJson);
            Assert.Equal("v1", snap.RuleEngineVersion);
            Assert.Equal("DP", snap.AnalyzedBy);
            Assert.Contains("stored", snap.AbsorptionJson);
            Assert.Equal("fp-new", iso.Db.GetLatestAnalysisSnapshot("NEW-SPEC")!.InputFingerprint);

            var id = snap.Id;
            iso.Db.RestoreArchive(archivePath);
            Assert.Equal(id, iso.Db.GetLatestAnalysisSnapshot("OLD-SPEC")!.Id);
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void RestoreArchive_RestoresSavedAntigenOrder_AndLeavesLiveOrder()
    {
        using var iso = new IsolatedDatabase();
        var restoredId = iso.Db.AddPanel("Order Restore", "L1", "V", 1, null, false);
        var liveId = iso.Db.AddPanel("Order Live", "L2", "V", 1, null, false);
        var reversed = AntigenConstants.Antigens.Reverse().ToList();
        var forward = AntigenConstants.Antigens.ToList();
        iso.Db.SetPanelAntigenOrder(restoredId, reversed);
        iso.Db.SetPanelAntigenOrder(liveId, reversed);
        iso.Db.AddSpecimen("OLD-SPEC", "serum", null, createdDate: "2020-01-15");
        iso.Db.AddSpecimen("NEW-SPEC", "serum", null, createdDate: "2026-06-01");
        iso.Db.LinkSpecimenPanel("OLD-SPEC", restoredId);
        iso.Db.LinkSpecimenPanel("OLD-SPEC", liveId);
        iso.Db.LinkSpecimenPanel("NEW-SPEC", liveId);

        var archivePath = NewArchivePath();
        try
        {
            iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archivePath);
            iso.Db.DeletePanel(restoredId);
            iso.Db.SetPanelAntigenOrder(liveId, forward);
            Assert.Null(iso.Db.GetPanel(restoredId));

            iso.Db.RestoreArchive(archivePath);
            Assert.Equal(reversed, iso.Db.GetPanelAntigenOrder(restoredId));
            Assert.Equal(forward, iso.Db.GetPanelAntigenOrder(liveId));

            iso.Db.RestoreArchive(archivePath);
            Assert.Equal(reversed, iso.Db.GetPanelAntigenOrder(restoredId));
            Assert.Equal(forward, iso.Db.GetPanelAntigenOrder(liveId));
        }
        finally
        {
            TryDelete(archivePath);
        }
    }

    [Fact]
    public void RestoreArchive_RefusesLiveDatabasePath()
    {
        using var iso = new IsolatedDatabase();
        Assert.Throws<InvalidOperationException>(() => iso.Db.RestoreArchive(iso.Db.DbPath));
    }

    private static void Seed(IsolatedDatabase iso, out int usedPanelId, out int unusedPanelId)
    {
        usedPanelId = iso.Db.AddPanel("Used Panel", "L1", "V", 1, null, false);
        unusedPanelId = iso.Db.AddPanel("Unused Panel", "L2", "V", 1, null, false);
        iso.Db.AddSpecimen("OLD-SPEC", "serum", null, createdDate: "2020-01-15");
        iso.Db.AddSpecimen("NEW-SPEC", "serum", null, createdDate: "2026-06-01");
        iso.Db.LinkSpecimenPanel("OLD-SPEC", usedPanelId);
        iso.Db.LinkSpecimenPanel("NEW-SPEC", usedPanelId);
    }

    private static string NewArchivePath() =>
        Path.Combine(Path.GetTempPath(), $"abpanels_archive_{Guid.NewGuid():N}.db");

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* ignore */ }
    }
}
