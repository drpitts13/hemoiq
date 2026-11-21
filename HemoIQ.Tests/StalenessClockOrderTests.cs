using System.Globalization;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// Staleness compares local timestamps to the second. A grade changed after an analysis must make it stale
/// even within the same second or after the clock went back (end of daylight saving time, a correction).
/// </summary>
// Staleness compares each analysis with the global settings, so run apart from tests that change them.
[Collection("PersistentDatabase")]
public class StalenessClockOrderTests
{
    private static void Raw(string dbPath, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string Stamp(DateTime t) => t.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static (IsolatedDatabase Iso, int Panel) Analyzed(string acc)
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Clock", "CLK-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale(acc));
        return (iso, panel);
    }

    [Fact]
    public void GradeChangedAfterTheClockWentBack_StillMakesTheAnalysisStale()
    {
        var (iso, panel) = Analyzed("CLK-1");
        using var _ = iso;
        // The analysis was stamped an hour "later" than now, as after the fall-back hour repeats.
        var analyzedAt = Stamp(DateTime.Now.AddHours(1));
        Raw(iso.DbPath, $"UPDATE specimens SET last_analyzed_at = '{analyzedAt}', reactions_updated_at = '{analyzedAt}' WHERE accession_number = 'CLK-1'");

        iso.Db.SaveReaction("CLK-1", panel, "2", "0", "0", "1+", "NT");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("CLK-1"));
        var s = iso.Db.GetSpecimen("CLK-1")!;
        Assert.Equal(Stamp(DateTime.ParseExact(analyzedAt, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).AddSeconds(1)),
            s.ReactionsUpdatedAt);
    }

    [Fact]
    public void GradeChangedInTheSameSecondAsTheAnalysis_MakesItStale()
    {
        var (iso, panel) = Analyzed("CLK-2");
        using var _ = iso;
        var now = Stamp(DateTime.Now);
        Raw(iso.DbPath, $"UPDATE specimens SET last_analyzed_at = '{now}', reactions_updated_at = '{now}' WHERE accession_number = 'CLK-2'");

        iso.Db.SaveReaction("CLK-2", panel, "2", "0", "0", "1+", "NT");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("CLK-2"));
    }

    [Fact]
    public void AnalysisAfterTheClockWentBack_CoversTheGradesItRead()
    {
        var (iso, panel) = Analyzed("CLK-3");
        using var _ = iso;
        var later = Stamp(DateTime.Now.AddHours(1));
        Raw(iso.DbPath, $"UPDATE specimens SET reactions_updated_at = '{later}' WHERE accession_number = 'CLK-3'");
        Assert.True(iso.Db.IsSpecimenAnalysisStale("CLK-3"));

        iso.Analyzer.AnalyzeSpecimen("CLK-3", updateDb: true);

        Assert.False(iso.Db.IsSpecimenAnalysisStale("CLK-3"));
        Assert.Equal(later, iso.Db.GetSpecimen("CLK-3")!.LastAnalyzedAt);
    }

    [Fact]
    public void OrdinaryOrder_UsesTheCurrentTime()
    {
        var (iso, panel) = Analyzed("CLK-4");
        using var _ = iso;
        var earlier = Stamp(DateTime.Now.AddHours(-1));
        Raw(iso.DbPath, $"UPDATE specimens SET last_analyzed_at = '{earlier}', reactions_updated_at = '{earlier}' WHERE accession_number = 'CLK-4'");
        var before = Stamp(DateTime.Now);

        iso.Db.SaveReaction("CLK-4", panel, "2", "0", "0", "1+", "NT");

        var stamped = iso.Db.GetSpecimen("CLK-4")!.ReactionsUpdatedAt!;
        Assert.True(string.CompareOrdinal(stamped, before) >= 0);
        Assert.True(iso.Db.IsSpecimenAnalysisStale("CLK-4"));
    }
}
