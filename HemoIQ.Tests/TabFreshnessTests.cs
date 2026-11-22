using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Search and Analytics keep what they computed until asked again, so another tab can change the data under them.
/// Opening each tab recomputes it. Search re-runs the queries behind the results on screen with the criteria they
/// were run with, so a lot taken out of use is not picked as a selected cell from an older list.
/// </summary>
public class TabFreshnessTests
{
    private static (IsolatedDatabase Iso, int Panel) KPanel()
    {
        var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Fresh", "L-FR", "V", 2, null, false);
        var cells = iso.Db.GetPanelCells(id);
        cells[0].SetAntigen("K", "+");
        cells[1].SetAntigen("K", "-");
        iso.Db.UpdatePanelCell(cells[0]);
        iso.Db.UpdatePanelCell(cells[1]);
        return (iso, id);
    }

    private static SearchViewModel SearchK(IsolatedDatabase iso)
    {
        var vm = new SearchViewModel(iso.Db);
        vm.Criteria.Single(c => c.Antigen == "K").Selected = "+";
        vm.SearchCommand.Execute(null);
        Assert.Single(vm.Results);
        vm.SelectedResult = vm.Results[0];
        return vm;
    }

    [Fact]
    public void LotDeactivatedElsewhere_ShowsAsNotInUse_WhenSearchOpens_AndTheSelectionIsKept()
    {
        var (iso, panel) = KPanel();
        using (iso)
        {
            var vm = SearchK(iso);
            Assert.True(vm.Results[0].LotInUse);

            iso.Db.SetPanelActive(panel, false);
            Assert.True(vm.Results[0].LotInUse); // the list on screen is older than the change

            vm.OnTabSelected();
            Assert.False(vm.Results.Single().LotInUse);
            Assert.NotNull(vm.SelectedResult);
            Assert.Equal(vm.Results[0], vm.SelectedResult);
        }
    }

    [Fact]
    public void Reopening_UsesTheCriteriaTheResultsWereRunWith_NotCriteriaEditedSince()
    {
        var (iso, _) = KPanel();
        using (iso)
        {
            var vm = SearchK(iso);
            vm.Criteria.Single(c => c.Antigen == "K").Selected = "-";
            vm.OnTabSelected();
            Assert.Equal("1", vm.Results.Single().CellNumber);
        }
    }

    [Fact]
    public void ClearedSearch_IsNotRunAgain()
    {
        var (iso, _) = KPanel();
        using (iso)
        {
            var vm = SearchK(iso);
            vm.ClearCommand.Execute(null);
            vm.OnTabSelected();
            Assert.Empty(vm.Results);
        }
    }

    [Fact]
    public void InvestigationResults_ShowAConfirmationMadeElsewhere_WhenSearchOpens()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("TF-1", "serum", null);
        var vm = new SearchViewModel(iso.Db) { InvestigationQuery = "TF-1" };
        vm.InvestigationSearchCommand.Execute(null);
        vm.SelectedInvestigation = vm.InvestigationResults.Single();
        Assert.Equal("", vm.SelectedInvestigation.FinalId);

        iso.Db.SetSpecimenFinalCall("TF-1", "anti-K", null, "TE");
        vm.InvestigationQuery = "something else";
        vm.OnTabSelected();

        Assert.Equal("anti-K", vm.InvestigationResults.Single().FinalId);
        Assert.Equal("TF-1", vm.SelectedInvestigation?.Accession);
    }

    [Fact]
    public void InvalidInvestigationSearch_LeavesNothingToRunAgain()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("TF-2", "serum", null);
        var vm = new SearchViewModel(iso.Db) { InvestigationQuery = "TF-2" };
        vm.InvestigationSearchCommand.Execute(null);
        vm.InvestigationQuery = "";
        vm.InvestigationSearchCommand.Execute(null); // refused: no text or dates
        vm.OnTabSelected();
        Assert.Empty(vm.InvestigationResults);
    }

    [Fact]
    public void Analytics_CountsAConfirmationMadeElsewhere_WhenTheTabOpens()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("TF-3", "serum", null);
        var vm = new AnalyticsViewModel(iso.Db);
        Assert.Equal(0, vm.ConfirmedSpecimens);

        iso.Db.SetSpecimenFinalCall("TF-3", "anti-E", null, "TE");
        Assert.Equal(0, vm.ConfirmedSpecimens);
        vm.OnTabSelected();
        Assert.Equal(1, vm.ConfirmedSpecimens);
    }
}
