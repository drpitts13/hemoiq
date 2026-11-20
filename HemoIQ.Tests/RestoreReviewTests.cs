using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class RestoreReviewTests
{
    private static void Raw(string dbPath, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>OLD-SPEC (2020) tested on a 2-cell panel, purged to an archive.</summary>
    private static (IsolatedDatabase Iso, int Panel, string Archive) Archived()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Restore Panel", "RP-1", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("OLD-SPEC", "serum", null, createdDate: "2020-01-15");
        iso.Db.LinkSpecimenPanel("OLD-SPEC", panel);
        iso.Db.SaveReaction("OLD-SPEC", panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction("OLD-SPEC", panel, "2", "0", "0", "0", "2+");
        var archive = Path.Combine(Path.GetTempPath(), $"hemoiq_restore_{Guid.NewGuid():N}.db");
        iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archive);
        SqliteConnection.ClearAllPools();
        return (iso, panel, archive);
    }

    [Fact]
    public void CleanArchive_RestoresWithNoReviewFindings()
    {
        var (iso, _, archive) = Archived();
        using var _ = iso;
        try
        {
            var result = iso.Db.RestoreArchive(archive);
            Assert.Equal(1, result.SpecimensRestored);
            Assert.Empty(result.ReviewFindings);
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(archive); }
    }

    [Fact]
    public void UnrecognizedGrade_InTheArchive_IsRestoredAsIs_AndReported()
    {
        var (iso, panel, archive) = Archived();
        using var _ = iso;
        try
        {
            Raw(archive, "UPDATE reactions SET AHG = 'pos' WHERE cell_number = '1'");

            var result = iso.Db.RestoreArchive(archive);

            var finding = Assert.Single(result.ReviewFindings);
            Assert.StartsWith("OLD-SPEC: unrecognized grades: Restore Panel for OLD-SPEC stores grade(s) HemoIQ does not recognize: cell 1 AHG \"pos\"", finding);
            Assert.Equal("pos", iso.Db.GetReactions("OLD-SPEC", panel).Single(r => r.CellNumber == "1").AHG);
            var audit = iso.Db.GetAuditEvents().Last(e => e.Action == "restore");
            Assert.Contains("reviewFindings", audit.AfterJson);
            Assert.Contains("unrecognized grades", audit.AfterJson);
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(archive); }
    }

    [Fact]
    public void UnreadableTyping_OnARestoredPanel_IsReported()
    {
        var (iso, panel, archive) = Archived();
        using var _ = iso;
        try
        {
            // The panel is gone from the live database, so restore brings it back from the archive.
            Raw(iso.DbPath, $"DELETE FROM panels WHERE panel_id = {panel}");
            Raw(archive, "UPDATE panel_cells SET ag_Jka = '0' WHERE cell_number = '2'");

            var result = iso.Db.RestoreArchive(archive);

            Assert.Equal(1, result.PanelsRestored);
            Assert.Contains(result.ReviewFindings, f =>
                f.StartsWith("Restore Panel (lot RP-1): unreadable typing cell 2 Jka \"0\"") &&
                f.EndsWith("Correct them with Edit on the Panels tab."));
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(archive); }
    }
}
