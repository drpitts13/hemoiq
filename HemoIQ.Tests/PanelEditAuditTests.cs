using System.Text.Json;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Editing a panel's details changes clinically important history (which lot, which expiry, whether the cells were
/// enzyme-treated). The audit entry must keep the old value of every changed field, and the Edit dialog's Active box
/// must not switch an inactive lot on without the activation review.
/// </summary>
public class PanelEditAuditTests
{
    private static Dictionary<string, JsonElement> Read(string? json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!)!;

    [Fact]
    public void UpdatePanel_RecordsOldAndNewValueOfEveryChangedField()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Panel A", "LOT-1", "Vendor", 3, "2027-01-31", false, 1, true,
            "CAT-1", "Line", false);

        iso.Db.UpdatePanel(panelId, "Panel A", "LOT-2", "Vendor", 3, "2027-03-31", false, 1, true,
            "CAT-1", "Line", enzymeTreated: true);

        var audit = iso.Db.GetAuditEventsForEntity("panel", panelId.ToString()).Last(e => e.Action == "update_panel");
        var before = Read(audit.BeforeJson);
        var after = Read(audit.AfterJson);
        Assert.Equal("LOT-1", before["lotNumber"].GetString());
        Assert.Equal("LOT-2", after["lotNumber"].GetString());
        Assert.Equal("2027-01-31", before["expirationDate"].GetString());
        Assert.Equal("2027-03-31", after["expirationDate"].GetString());
        Assert.False(before["enzymeTreated"].GetBoolean());
        Assert.True(after["enzymeTreated"].GetBoolean());
        // Unchanged fields are not repeated.
        Assert.False(before.ContainsKey("name"));
        Assert.False(before.ContainsKey("vendor"));
        Assert.False(after.ContainsKey("catalogNumber"));
        Assert.Equal(0, after["cellsAdded"].GetArrayLength());
    }

    [Fact]
    public void UpdatePanel_WithNoDetailChange_RecordsAnEmptyBefore_AndTreatsBlankAsNull()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Same", null, "V", 2, null, false);

        iso.Db.UpdatePanel(panelId, "Same", "", "V", 2, null, false);

        var audit = iso.Db.GetAuditEventsForEntity("panel", panelId.ToString()).Last(e => e.Action == "update_panel");
        Assert.Empty(Read(audit.BeforeJson));
    }

    [Fact]
    public void UpdatePanel_OnAMissingPanel_IsRefused_AndWritesNoAudit()
    {
        using var iso = new IsolatedDatabase();
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.UpdatePanel(9999, "X", null, null, 3, null, false));
        Assert.Contains("#9999 was not found", ex.Message);
        Assert.Empty(iso.Db.GetAuditEventsForEntity("panel", "9999"));
    }

    [Theory]
    [InlineData(false, true, true, false)]   // inactive lot ticked active: route to Activate, save inactive
    [InlineData(true, true, false, true)]    // stays active
    [InlineData(true, false, false, false)]  // deactivate with the edit
    [InlineData(false, false, false, false)] // stays inactive
    public void EditDialog_ActiveBox_NeverActivatesWithoutReview(bool stored, bool requested, bool review, bool save)
    {
        Assert.Equal(review, PanelEditActivation.NeedsActivationReview(stored, requested));
        Assert.Equal(save, PanelEditActivation.ActiveToSave(stored, requested));
    }

    [Fact]
    public void PanelsEdit_SavesThroughTheActivationGuard()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "PanelsViewModel.cs"));
        var edit = src[src.IndexOf("private void EditPanel()", StringComparison.Ordinal)..];
        edit = edit[..edit.IndexOf("private void DeletePanel()", StringComparison.Ordinal)];
        Assert.Contains("PanelEditActivation.ActiveToSave(", edit);
        Assert.Contains("ActivatePanel();", edit);
        Assert.DoesNotContain("dlg.ItemIsActive,\n", edit.Replace("\r\n", "\n"));
    }
}
