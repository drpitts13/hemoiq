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
    public void CopyPanelCells_RecordsTheSourceLot()
    {
        using var iso = new IsolatedDatabase();
        var source = iso.Db.AddPanel("Src", "LOT-S", "Vendor", 3, null, false);
        var target = iso.Db.AddPanel("Copy", "LOT-T", "Vendor", 3, null, false, isActive: false);

        iso.Db.CopyPanelCells(source, target);

        var audit = iso.Db.GetAuditEventsForEntity("panel", target.ToString()).Single(e => e.Action == "copy_panel_typing");
        var after = Read(audit.AfterJson);
        Assert.Equal(source, after["sourcePanelId"].GetInt32());
        Assert.Equal("LOT-S", after["sourceLot"].GetString());
        Assert.Equal(3, after["cells"].GetInt32());
        Assert.False(iso.Db.GetPanel(target)!.IsActive);
    }

    [Fact]
    public void CopyPanelCells_IntoALotWithHistory_IsRefused_AndWritesNoCopyAudit()
    {
        using var iso = new IsolatedDatabase();
        var source = iso.Db.AddPanel("Src", "LOT-S", "V", 2, null, false);
        var target = iso.Db.AddPanel("Used", "LOT-U", "V", 2, null, false);
        iso.Db.AddSpecimen("CP-1", "serum", null);
        iso.Db.LinkSpecimenPanel("CP-1", target);
        iso.Db.SaveReaction("CP-1", target, "1", "0", "0", "2+", "NT");

        Assert.ThrowsAny<InvalidOperationException>(() => iso.Db.CopyPanelCells(source, target));
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("panel", target.ToString()), e => e.Action == "copy_panel_typing");
    }

    [Fact]
    public void PanelsCopy_StartsInactive_AndActivatesOnlyThroughReview()
    {
        var copy = ViewModelMethod("private void CopyPanel()", "private void DownloadFromVendor()");
        Assert.Contains("IsActive = false,", copy);
        Assert.Contains("PanelEditActivation.ActiveToSave(storedActive: false, dlg.ItemIsActive)", copy);
        Assert.Contains("ActivatePanel();", copy);
        Assert.DoesNotContain("dlg.StartCell, dlg.ItemIsActive,", copy);
    }

    [Fact]
    public void ExpiryWarning_OnlyForAPassedDate()
    {
        var today = new DateTime(2026, 11, 21);
        Assert.Contains("EXPIRED: this lot expired on 2026-11-20",
            PanelEditActivation.ExpiryWarning("2026-11-20", today));
        Assert.Null(PanelEditActivation.ExpiryWarning("2026-11-21", today));
        Assert.Null(PanelEditActivation.ExpiryWarning("2027-01-01", today));
        Assert.Null(PanelEditActivation.ExpiryWarning(null, today));
    }

    [Fact]
    public void ActivatingAnExpiredLot_IsRecordedInTheAuditReason()
    {
        using var iso = new IsolatedDatabase();
        var expired = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd");
        var old = iso.Db.AddPanel("Old", "LOT-X", "V", 2, expired, false, isActive: false);
        var current = iso.Db.AddPanel("New", "LOT-Y", "V", 2, DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"), false, isActive: false);

        iso.Db.SetPanelActive(old, true);
        iso.Db.SetPanelActive(current, true);

        Assert.Equal($"Activated after expiry ({expired})",
            iso.Db.GetAuditEventsForEntity("panel", old.ToString()).Last(e => e.Action == "activate_panel").Reason);
        Assert.Null(iso.Db.GetAuditEventsForEntity("panel", current.ToString()).Last(e => e.Action == "activate_panel").Reason);
    }

    [Fact]
    public void EveryActivationPath_WarnsAboutExpiry()
    {
        Assert.Contains("PanelEditActivation.ExpiryWarning(",
            ViewModelMethod("private void ActivatePanel()", "internal static bool ConfirmExpiredActivation("));
        Assert.Contains("ConfirmExpiredActivation(",
            ViewModelMethod("private void ImportCsv()", "private void ExportCsv()"));
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var vendor = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Views", "Dialogs", "DownloadFromVendorDialog.xaml.cs"));
        Assert.Contains("PanelsViewModel.ConfirmExpiredActivation(", vendor);
    }

    private static string ViewModelMethod(string start, string end)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "PanelsViewModel.cs"));
        var body = src[src.IndexOf(start, StringComparison.Ordinal)..];
        return body[..body.IndexOf(end, StringComparison.Ordinal)];
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
