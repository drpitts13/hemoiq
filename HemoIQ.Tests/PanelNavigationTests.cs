using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Opening a lot from Search or the Worklist must not leave another lot selected with the search result's cell and
/// antigen highlighted on it.
/// </summary>
public class PanelNavigationTests
{
    private static string Source(string file)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", file));
    }

    [Fact]
    public void ADeletedPanel_CannotBeOpened_AndTheReasonSaysSo()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Gone", "L-NAVG", "V", 2, null, false, startTypedNegative: true);
        iso.Db.DeletePanel(panel);
        var why = SpecimenNavigation.PanelCannotOpen(iso.Db, panel);
        Assert.NotNull(why);
        Assert.Contains($"Panel #{panel} was not found", why);
        Assert.Contains("deleted on another workstation", why);
    }

    [Fact]
    public void AnExistingPanel_CanBeOpened()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Here", "L-NAVH", "V", 2, null, false, startTypedNegative: true);
        Assert.Null(SpecimenNavigation.PanelCannotOpen(iso.Db, panel));
    }

    [Fact]
    public void OpenPanel_RefusesBeforeSwitchingTab()
    {
        var src = Source("MainViewModel.cs");
        var body = src[src.IndexOf("public void OpenPanel(")..];
        body = body[..body.IndexOf("PanelsVM.SelectPanel(")];
        var check = body.IndexOf("NotOpened(SpecimenNavigation.PanelCannotOpen(");
        Assert.True(check > 0);
        Assert.True(check < body.IndexOf("SelectedTabIndex = 2"));
    }

    [Fact]
    public void SelectPanel_ReloadsALotMissingFromItsList()
    {
        var src = Source("PanelsViewModel.cs");
        var body = src[src.IndexOf("public void SelectPanel(")..];
        body = body[..body.IndexOf("_applyingFocusedCell = true;")];
        Assert.Contains("!_allPanels.Any(p => p.PanelId == panelId)", body);
        Assert.Contains("Refresh();", body);
    }
}
