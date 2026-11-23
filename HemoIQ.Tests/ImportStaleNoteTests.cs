using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Rule and configuration imports say open analyses are now stale only when the import changed something analysis
/// reads (the analysis-configuration audit marker moved); renames and display Preferences leave analyses current.
/// </summary>
[Collection("PersistentDatabase")]
public class ImportStaleNoteTests
{
    private static string RulesFile(string name, string? description, int count)
    {
        using var source = new IsolatedDatabase();
        source.Db.AddRule(name, description, "anti-K", null, false, count);
        return RuleTransfer.Export(source.Db.GetAllRules(), new DateTime(2026, 11, 9));
    }

    [Fact]
    public void RuleImport_ThatOnlyRenames_DoesNotMoveTheMarker()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("Kell three", "policy", "anti-K", null, false, 3);
        var before = iso.Db.LastAnalysisConfigurationChange();
        var preview = RuleTransfer.Preview(RulesFile("Kell - three", "policy", 3), iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());
        var (_, updated) = RuleTransfer.Apply(iso.Db, preview);

        Assert.Equal(1, updated);
        Assert.False(AnalysisConfigurationMarker.Moved(before, iso.Db.LastAnalysisConfigurationChange()));
    }

    [Fact]
    public void RuleImport_ThatChangesACount_MovesTheMarker()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("Kell three", "policy", "anti-K", null, false, 3);
        var before = iso.Db.LastAnalysisConfigurationChange();
        var preview = RuleTransfer.Preview(RulesFile("Kell three", "policy", 2), iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());
        RuleTransfer.Apply(iso.Db, preview);
        Assert.True(AnalysisConfigurationMarker.Moved(before, iso.Db.LastAnalysisConfigurationChange()));
    }

    [Fact]
    public void ConfigurationImport_OfDisplayPreferencesOnly_DoesNotMoveTheMarker()
    {
        using var source = new IsolatedDatabase();
        var json = ConfigurationBundle.Export(source.Db.GetAllRules(), new LabSettings { LabName = "Reference Lab" }, new DateTime(2026, 11, 9));
        using var target = new IsolatedDatabase();
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, target.Db.GetAllRules(), target.Db.GetKnownAntigens(), settings);
        var before = target.Db.LastAnalysisConfigurationChange();
        var result = ConfigurationBundle.Apply(target.Db, settings, preview, false, "f.json");

        Assert.True(result.PreferencesChanged > 0);
        var moved = AnalysisConfigurationMarker.Moved(before, target.Db.LastAnalysisConfigurationChange());
        Assert.False(moved);
        Assert.Contains("analyses stay current", ConfigurationBundle.ImportStatus(result, moved));
    }

    [Fact]
    public void StatusWording_FollowsTheMarker()
    {
        Assert.Contains("Stale analysis", RulesViewModel.ImportStatus(1, 0, analysisAffected: true));
        Assert.Contains("analyses are not affected", RulesViewModel.ImportStatus(0, 2, analysisAffected: false));
        Assert.DoesNotContain("Stale", RulesViewModel.ImportStatus(0, 2, analysisAffected: false));
        Assert.Contains("Stale analysis", ConfigurationBundle.ImportStatus(new ConfigurationImportResult(1, 0, 0, 0), true));
        Assert.False(AnalysisConfigurationMarker.Moved(null, null));
        Assert.True(AnalysisConfigurationMarker.Moved(null, new AnalysisConfigurationChange(1, "")));
    }
}
