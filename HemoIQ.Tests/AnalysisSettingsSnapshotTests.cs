using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Every Preferences value the analysis reads must be in the clinical snapshot, so that changing it is audited, marks
/// open analyses out of date, and is recorded with each analysis. Adding a value to the snapshot must not mark
/// analyses saved before it existed as stale.
/// </summary>
[Collection("PersistentDatabase")]
public class AnalysisSettingsSnapshotTests
{
    [Fact]
    public void Snapshot_IncludesTheDatingAndRhigWindowsAnalysisReads()
    {
        var json = new LabSettings().ClinicalSettingsJson();
        Assert.Contains("\"DefaultSpecimenDatingDays\"", json);
        Assert.Contains("\"DatingTransfusionLookbackDays\"", json);
        Assert.Contains("\"RhigPassiveWindowDays\"", json);
    }

    [Fact]
    public void OlderSnapshot_WithoutTheNewSettings_IsNotStale_ButAChangedSharedSettingIs()
    {
        var settings = new LabSettings { ProbabilityThreshold = 0.6 };
        var current = settings.ClinicalSettingsJson();
        // An analysis saved before the dating and RhIG windows were recorded.
        var older = $"{{\"ProbabilityThreshold\":0.6,\"IdentificationCellCount\":{settings.IdentificationCellCount}}}";

        Assert.False(LabSettings.ClinicalSettingsDiffer(older, current));
        Assert.True(LabSettings.ClinicalSettingsDiffer(older.Replace("0.6", "0.7"), current));
        Assert.False(LabSettings.ClinicalSettingsDiffer(null, current));
        Assert.True(LabSettings.ClinicalSettingsDiffer("not json", current));
    }

    [Fact]
    public void ChangingTheRhigWindow_AfterAnalysis_MarksItStale_AndIsAudited()
    {
        var previous = AppSettings.Current.RhigPassiveWindowDays;
        try
        {
            using var iso = new IsolatedDatabase();
            iso.Db.AddSpecimen("RH-1", "serum", null);
            var panel = iso.Db.AddPanel("P", "L-RH", "V", 2, null, false, startTypedNegative: true);
            iso.Db.LinkSpecimenPanel("RH-1", panel);
            iso.Db.SaveReaction("RH-1", panel, "1", "0", "0", "0", "2+");
            iso.Analyzer.AnalyzeSpecimen("RH-1", updateDb: true);
            Assert.False(iso.Db.IsSpecimenAnalysisStale("RH-1"));

            // Through the Preferences import path, which audits exactly as the dialog does.
            var json = PreferenceTransfer.Export(AppSettings.Current, DateTime.Now)
                .Replace($"\"rhigPassiveWindowDays\": {previous}", $"\"rhigPassiveWindowDays\": {previous + 30}");
            var preview = PreferenceTransfer.Preview(json, AppSettings.Current);
            Assert.Contains(preview.Changes, c => c.Name == "rhigPassiveWindowDays");
            PreferenceTransfer.Apply(iso.Db, AppSettings.Current, preview, "test.json");

            Assert.True(iso.Db.IsSpecimenAnalysisStale("RH-1"));
            var audit = iso.Db.GetAuditEventsForEntity("settings", "lab").Last(e => e.Action == "update_settings");
            Assert.Contains($"\"RhigPassiveWindowDays\":{previous}", audit.BeforeJson);
            Assert.Contains($"\"RhigPassiveWindowDays\":{previous + 30}", audit.AfterJson);
        }
        finally
        {
            AppSettings.Current.RhigPassiveWindowDays = previous;
        }
    }
}
