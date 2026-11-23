using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Opening a specimen from the Worklist or Search must never land on another specimen: a tab whose list does not hold
/// it reloads once, and if it still cannot show it, says why and keeps its selection.
/// </summary>
public class SpecimenNavigationTests
{
    [Fact]
    public void ADeletedSpecimen_CannotBeOpened_AndTheReasonSaysSo()
    {
        using var iso = new IsolatedDatabase();
        var why = SpecimenNavigation.CannotOpen(iso.Db, "NOPE-1", "Reactions", activeOnly: true);
        Assert.NotNull(why);
        Assert.Contains("NOPE-1 was not found", why);
        Assert.Contains("deleted on another workstation", why);
    }

    [Fact]
    public void AnInactiveSpecimen_CannotBeOpenedOnTheReactionsTab_ButCanElsewhere()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("INA-1", "serum", null);
        iso.Db.UpdateSpecimen("INA-1", "serum", null, isActive: false);
        var why = SpecimenNavigation.CannotOpen(iso.Db, "INA-1", "Reactions", activeOnly: true);
        Assert.NotNull(why);
        Assert.Contains("inactive", why);
        Assert.Contains("Reactivate it", why);
        Assert.Null(SpecimenNavigation.CannotOpen(iso.Db, "INA-1", "Analysis", activeOnly: false));
    }

    [Fact]
    public void Reports_AMissingSpecimen_KeepsTheCurrentSelection()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("NAV-A", "serum", null);
        iso.Db.AddSpecimen("NAV-B", "serum", null);
        var vm = new ReportsViewModel(iso.Db);
        Assert.Null(vm.SelectSpecimen("NAV-B"));
        Assert.Equal("NAV-B", vm.SelectedSpecimen?.AccessionNumber);

        var why = vm.SelectSpecimen("NAV-GONE");
        Assert.NotNull(why);
        Assert.Contains("not found", why);
        Assert.Equal("NAV-B", vm.SelectedSpecimen?.AccessionNumber);
    }

    [Fact]
    public void Reports_ASpecimenAddedAfterTheListLoaded_IsFoundByReloading()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("NAV-A", "serum", null);
        var vm = new ReportsViewModel(iso.Db);
        vm.SelectSpecimen("NAV-A");
        iso.Db.AddSpecimen("NAV-NEW", "serum", null); // e.g. registered on another workstation
        Assert.Null(vm.SelectSpecimen("NAV-NEW"));
        Assert.Equal("NAV-NEW", vm.SelectedSpecimen?.AccessionNumber);
    }

    [Fact]
    public void Reports_AMissingSpecimen_DoesNotChangeTheReportType()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("NAV-A", "serum", null);
        var vm = new ReportsViewModel(iso.Db);
        vm.SelectSpecimen("NAV-A", "Clinical Identification");
        vm.SelectSpecimen("NAV-GONE", "Specimen Summary");
        Assert.Equal("Clinical Identification", vm.SelectedReportType);
    }

    [Fact]
    public void OpenSpecimen_SelectsBeforeSwitchingTabs_AndStopsWhenNotOpened()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "MainViewModel.cs"));
        foreach (var (method, call, tab) in new[]
                 {
                     ("OpenSpecimenReactions", "ReactionsVM.SelectSpecimen", "SelectedTabIndex = 3"),
                     ("OpenSpecimenAnalysis", "AnalysisVM.SelectSpecimen", "SelectedTabIndex = 4"),
                     ("OpenSpecimenReport", "ReportsVM.SelectSpecimen", "SelectedTabIndex = 5"),
                 })
        {
            var body = src[src.IndexOf($"public void {method}(")..];
            body = body[..body.IndexOf('}')];
            Assert.Contains($"NotOpened({call}(", body);
            Assert.True(body.IndexOf(call) < body.IndexOf(tab), method);
        }
    }

    [Fact]
    public void NoTab_FallsBackToTheFirstSpecimen_WhenTheTargetIsMissing()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        foreach (var file in new[] { "ReactionsViewModel.cs", "AnalysisViewModel.cs", "ReportsViewModel.cs", "SpecimensViewModel.cs" })
        {
            var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", file));
            var body = src[src.IndexOf("public string? SelectSpecimen(")..];
            var select = body.IndexOf("ApplySpecimenFilter(accessionNumber);");
            if (select < 0) select = body.IndexOf("SelectedSpecimen = match;");
            Assert.True(select > 0, file);
            Assert.Contains("SpecimenNavigation.CannotOpen(", body[..select]);
        }
    }
}
