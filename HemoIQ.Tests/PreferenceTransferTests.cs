using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class PreferenceTransferTests
{
    [Fact]
    public void ExportThenPreview_OnDifferentSettings_ListsEachChange_AndPreviewChangesNothing()
    {
        var source = LabSettings.CreateDefault();
        source.LabName = "Central Blood Bank";
        source.DefaultMinRuleoutCount = 2;
        source.RequireSupervisorReview = true;
        source.ProbabilityThreshold = 0.7;
        source.DefaultIdentifiedBy = "ZZ"; // per-workstation: not exported
        var json = PreferenceTransfer.Export(source, new DateTime(2026, 11, 9));
        Assert.Contains("\"format\": \"hemoiq-preferences\"", json);
        Assert.DoesNotContain("defaultIdentifiedBy", json);
        Assert.DoesNotContain("worklistShow", json);

        var target = LabSettings.CreateDefault();
        var preview = PreferenceTransfer.Preview(json, target);
        Assert.Equal(4, preview.Changes.Count);
        Assert.Contains(preview.Changes, c => c.Name == "defaultMinRuleoutCount" && c.Before == "1" && c.After == "2");
        Assert.Contains(preview.Changes, c => c.Name == "requireSupervisorReview" && c.After == "yes");
        Assert.Contains(preview.Changes, c => c.Name == "probabilityThreshold" && c.After == "0.7");
        Assert.Equal(1, target.DefaultMinRuleoutCount); // nothing applied yet
        Assert.Contains("4 to change", PreferenceTransfer.Summarize(preview));
    }

    [Fact]
    public void Preview_RefusesOutOfRangeAndWrongTypes_IgnoresUnknownNames()
    {
        var json = """
        { "format": "hemoiq-preferences", "version": 1, "settings": {
            "defaultMinRuleoutCount": 9, "probabilityThreshold": 0.1, "requireSupervisorReview": "yes",
            "defaultSpecimenType": "whole blood", "labName": "  ", "turnaroundThresholdDays": 5,
            "worklistShowReview": false, "somethingNew": 1 } }
        """;
        var preview = PreferenceTransfer.Preview(json, LabSettings.CreateDefault());
        Assert.Contains("defaultMinRuleoutCount: 9 is outside 1–5.", preview.Refused);
        Assert.Contains("probabilityThreshold: 0.1 is outside 0.3–0.95.", preview.Refused);
        Assert.Contains("requireSupervisorReview: must be true or false.", preview.Refused);
        Assert.Contains("defaultSpecimenType: must be serum, plasma or eluate.", preview.Refused);
        Assert.Contains("labName: cannot be blank.", preview.Refused);
        Assert.Equal(new[] { "worklistShowReview", "somethingNew" }, preview.Ignored);
        Assert.Equal("turnaroundThresholdDays", Assert.Single(preview.Changes).Name);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("""{ "format": "hemoiq-rules", "version": 1, "rules": [] }""")]
    [InlineData("""{ "format": "hemoiq-preferences", "version": 2, "settings": {} }""")]
    [InlineData("""{ "format": "hemoiq-preferences", "version": 1 }""")]
    public void Preview_RejectsFilesThatAreNotPreferenceExports(string json) =>
        Assert.Throws<ArgumentException>(() => PreferenceTransfer.Preview(json, LabSettings.CreateDefault()));

    [Fact]
    public void Apply_ChangesSettings_AuditsImport_AndAuditsAnalysisSettingsAsUpdateSettings()
    {
        using var iso = new IsolatedDatabase();
        var settings = LabSettings.CreateDefault();
        var json = """
        { "format": "hemoiq-preferences", "version": 1, "settings": {
            "defaultMinRuleoutCount": 2, "department": "Transfusion Service", "extraPhases": "peg, rt" } }
        """;
        var preview = PreferenceTransfer.Preview(json, settings);
        PreferenceTransfer.Apply(iso.Db, settings, preview, "site-a.json");

        Assert.Equal(2, settings.DefaultMinRuleoutCount);
        Assert.Equal("Transfusion Service", settings.Department);
        Assert.Equal(ExtraPhaseParser.NormalizeList("peg, rt"), settings.ExtraPhases);
        var events = iso.Db.GetAuditEvents();
        var import = Assert.Single(events, e => e.Action == "import_preferences");
        Assert.Contains("Transfusion Service", import.AfterJson);
        var update = Assert.Single(events, e => e.Action == "update_settings");
        Assert.Contains("\"DefaultMinRuleoutCount\":2", update.AfterJson);
    }

    [Fact]
    public void Apply_OnlyNonAnalysisChanges_DoesNotMarkAnalysesStale()
    {
        using var iso = new IsolatedDatabase();
        var settings = LabSettings.CreateDefault();
        var preview = PreferenceTransfer.Preview(
            """{ "format": "hemoiq-preferences", "version": 1, "settings": { "labName": "Other Lab" } }""", settings);
        PreferenceTransfer.Apply(iso.Db, settings, preview, null);
        Assert.Equal("Other Lab", settings.LabName);
        Assert.DoesNotContain(iso.Db.GetAuditEvents(), e => e.Action == "update_settings");
        Assert.Single(iso.Db.GetAuditEvents(), e => e.Action == "import_preferences");
    }
}
