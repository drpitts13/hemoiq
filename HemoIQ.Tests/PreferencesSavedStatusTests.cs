using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// After Preferences are saved the status says when a clinical change marked open analyses stale (as rule edits and
/// imports do), and keeps the near-capacity warning instead of one replacing the other.
/// </summary>
public class PreferencesSavedStatusTests
{
    [Fact]
    public void Status_NamesStaleAnalyses_OnlyWhenAClinicalValueChanged()
    {
        Assert.Equal("Preferences saved.", MainViewModel.PreferencesSavedStatus(false, null));
        Assert.Contains("Stale analysis", MainViewModel.PreferencesSavedStatus(true, null));
    }

    [Fact]
    public void Status_KeepsTheCapacityWarning()
    {
        var cap = new DatabaseCapacityStatus { FileBytes = 900L * 1024 * 1024, MaxBytes = 1000L * 1024 * 1024, PercentUsed = 90, IsNearCapacity = true };
        var text = MainViewModel.PreferencesSavedStatus(false, MainViewModel.CapacityText(cap));
        Assert.StartsWith("Preferences saved.", text);
        Assert.Contains("Database 90% full", text);
        Assert.Null(MainViewModel.CapacityText(new DatabaseCapacityStatus { IsNearCapacity = false }));
    }

    [Fact]
    public void Marker_MovesForAClinicalSettingsAudit_NotForOtherAudits()
    {
        using var iso = new IsolatedDatabase();
        var before = iso.Db.LastAnalysisConfigurationChange();
        iso.Db.AppendAudit("export_data", "export", "audit_log", null);           // not a configuration change
        Assert.False(AnalysisConfigurationMarker.Moved(before, iso.Db.LastAnalysisConfigurationChange()));
        iso.Db.AppendAudit("update_settings", "settings", "lab", null, "{}", "{}"); // what Save writes for a clinical change
        Assert.True(AnalysisConfigurationMarker.Moved(before, iso.Db.LastAnalysisConfigurationChange()));
    }
}
