using System.Globalization;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// A rule or Preferences change after an analysis must mark it stale whatever the clock did: the change is
/// ordered by its audit id, not by comparing local times.
/// </summary>
[Collection("PersistentDatabase")]
public class ConfigurationChangeOrderTests
{
    private static void Raw(string dbPath, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static IsolatedDatabase Analyzed(string acc)
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Cfg", "CFG-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale(acc));
        return iso;
    }

    private static bool OnWorklistAsStale(IsolatedDatabase iso, string acc) =>
        iso.Db.GetWorklistItems().Any(i => i.Kind == WorklistKind.StaleAnalysis && i.Title == acc);

    [Fact]
    public void RuleChangeInTheSameSecondAsTheAnalysis_MarksItStale()
    {
        using var iso = Analyzed("CFG-1");
        iso.Db.AddRule("Same-second rule", null, "anti-K", null, false, 2);

        Assert.True(iso.Db.IsSpecimenAnalysisStale("CFG-1"));
        Assert.True(OnWorklistAsStale(iso, "CFG-1"));
        var reason = AnalysisCurrency.StaleReason(iso.Db.GetSpecimen("CFG-1")!, iso.Db.LastAnalysisConfigurationChange());
        Assert.Contains("Analysis rules or Preferences changed", reason);
    }

    [Fact]
    public void RuleChangeWhoseClockTimeLooksEarlier_StillMarksTheAnalysisStale()
    {
        using var iso = Analyzed("CFG-2");
        iso.Db.AddRule("Repeated-hour rule", null, "anti-K", null, false, 2);
        // The change happened after the analysis, but the clock had gone back an hour.
        var earlier = DateTime.UtcNow.AddHours(-1).ToString("o", CultureInfo.InvariantCulture);
        Raw(iso.DbPath, $"UPDATE audit_events SET occurred_at_utc = '{earlier}' WHERE action = 'add_rule'");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("CFG-2"));
        Assert.True(OnWorklistAsStale(iso, "CFG-2"));
    }

    [Fact]
    public void AnalysisAfterTheChange_IsCurrent_EvenWhenItsClockTimeLooksEarlier()
    {
        using var iso = Analyzed("CFG-3");
        iso.Db.AddRule("Earlier rule", null, "anti-K", null, false, 2);
        iso.Analyzer.AnalyzeSpecimen("CFG-3", updateDb: true);
        var earlier = DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        Raw(iso.DbPath, $"UPDATE specimens SET last_analyzed_at = '{earlier}', reactions_updated_at = '{earlier}' WHERE accession_number = 'CFG-3'");

        Assert.False(iso.Db.IsSpecimenAnalysisStale("CFG-3"));
        Assert.False(OnWorklistAsStale(iso, "CFG-3"));
    }

    [Fact]
    public void AnalysisSavedBeforeTheAuditIdWasRecorded_ComparesTimes()
    {
        using var iso = Analyzed("CFG-4");
        Raw(iso.DbPath, "UPDATE specimens SET analysis_config_seq = NULL WHERE accession_number = 'CFG-4'");
        Assert.Null(iso.Db.GetSpecimen("CFG-4")!.AnalysisConfigSeq);
        iso.Db.AddRule("Later rule", null, "anti-K", null, false, 2);
        var later = DateTime.UtcNow.AddHours(1).ToString("o", CultureInfo.InvariantCulture);
        Raw(iso.DbPath, $"UPDATE audit_events SET occurred_at_utc = '{later}' WHERE action = 'add_rule'");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("CFG-4"));
    }

    [Fact]
    public void ConfirmedIdentification_IsNotReopenedByALaterChange()
    {
        using var iso = Analyzed("CFG-5");
        iso.Db.SetSpecimenFinalCall("CFG-5", "NAD", null, "TE");
        iso.Db.AddRule("After confirmation", null, "anti-K", null, false, 2);

        Assert.False(iso.Db.IsSpecimenAnalysisStale("CFG-5"));
        Assert.False(OnWorklistAsStale(iso, "CFG-5"));
    }

    [Fact]
    public void BareLocalTime_FromOlderCallers_StillComparesByTime()
    {
        var s = new Specimen { AccessionNumber = "X", LastAnalyzedAt = "2026-11-01T10:00:00", AnalysisConfigSeq = 99 };
        Assert.True(AnalysisCurrency.ConfigChangedAfter(s, "2026-11-01T11:00:00"));
        Assert.False(AnalysisCurrency.ConfigChangedAfter(s, "2026-11-01T09:00:00"));
        Assert.True(AnalysisCurrency.ConfigChangedAfter(s, new AnalysisConfigurationChange(100, "2026-11-01T09:00:00")));
        Assert.False(AnalysisCurrency.ConfigChangedAfter(s, new AnalysisConfigurationChange(99, "2026-11-01T11:00:00")));
    }
}
