using AntibodyPanels.Models;
using AntibodyPanels.ViewModels;

namespace AntibodyPanels.Tests;

public class ScreenStatusTests
{
    [Fact]
    public void OpeningSearchReplacesAWorklistLine()
    {
        var worklist = ScreenStatus.Worklist(total: 0, shown: 0);
        var search = ScreenStatus.Search(completedStatus: null);

        Assert.Equal("Worklist is clear.", worklist);
        Assert.Equal("Search by antigen profile.", search);
        Assert.NotEqual(worklist, search);
    }

    [Fact]
    public void ReturningToSearchKeepsTheHomozygousResult()
    {
        var completed = SearchViewModel.FormatResultStatus(81, 1, AntigenConstants.ZygosityHomozygous);
        Assert.Equal(
            "Found 81 matching cells for 1 criterion. Positive cells are homozygous.",
            ScreenStatus.Search(completed));
        Assert.Equal(
            "Found 81 matching cells for 1 criterion. Positive cells are homozygous. Selected E2E warm auto (E2E-WARM-AUTO) cell 2.",
            ScreenStatus.Search(completed, "E2E warm auto (E2E-WARM-AUTO) cell 2"));
    }

    [Fact]
    public void WorklistFilterAndCountLinesStayDistinct()
    {
        Assert.Equal(
            "No worklist items match the selected filters.",
            ScreenStatus.Worklist(total: 4, shown: 0));
        Assert.Equal(
            "Worklist: 4 item(s) need attention.",
            ScreenStatus.Worklist(total: 4, shown: 4));
        Assert.Equal(
            "Worklist: 2 of 4 item(s) shown.",
            ScreenStatus.Worklist(total: 4, shown: 2));
    }

    [Fact]
    public void OtherScreensNameWhatIsOnScreen()
    {
        Assert.Equal(
            "Specimens: 3 shown. Selected E2E-WARM-AUTO.",
            ScreenStatus.Specimens(3, "E2E-WARM-AUTO"));
        Assert.Equal(
            "Panels: 2 shown. Selected E2E warm auto plus allos (E2E-WARM-AUTO-ALLOS).",
            ScreenStatus.Panels(2, "E2E warm auto plus allos", "E2E-WARM-AUTO-ALLOS"));
        Assert.Equal(
            "Panels: 2 shown. Selected E2E warm auto plus allos (E2E-WARM-AUTO-ALLOS) cell 3.",
            ScreenStatus.Panels(2, "E2E warm auto plus allos", "E2E-WARM-AUTO-ALLOS", "3"));
        Assert.Equal(
            "Select a specimen and panel to enter reactions.",
            ScreenStatus.Reactions(null, null, null, 0));
        Assert.Equal(
            "E2E-WARM-AUTO has not been analyzed.",
            ScreenStatus.Analysis("E2E-WARM-AUTO", analyzed: false, stale: false, suspected: 0, ruledOut: 0));
        Assert.Equal(
            "E2E-WARM-AUTO analysis is stale.",
            ScreenStatus.Analysis("E2E-WARM-AUTO", analyzed: true, stale: true, suspected: 1, ruledOut: 0));
        Assert.Equal("Report: Clinical Identification.", ScreenStatus.Reports("Clinical Identification"));
        Assert.Equal("Analytics (All time): 0 confirmed specimen(s).", ScreenStatus.Analytics("All time", 0));
        var panel = new Panel { PanelId = 42, Name = "Bio-Rad ID-DiaPanel", LotNumber = "BIO2026C" };
        var row = new SearchResultRow(panel, new PanelCell { CellNumber = "3" });
        Assert.Equal(42, row.PanelId);
        Assert.Equal("Bio-Rad ID-DiaPanel (BIO2026C) cell 3", SearchViewModel.FormatSelectedResult(row));
        var k = new AntigenCriterionRow("K");
        var lub = new AntigenCriterionRow("Lub");
        lub.Selected = "+";
        Assert.Null(SearchViewModel.FirstActiveAntigen(new[] { k }));
        Assert.Equal("Lub", SearchViewModel.FirstActiveAntigen(new[] { k, lub }));
        Assert.Equal("Lub", SearchViewModel.LastActiveAntigen(new[] { k, lub }));
        Assert.Equal(new[] { "Lub" }, SearchViewModel.ActiveAntigens(new[] { k, lub }));
        k.Selected = "+";
        Assert.Equal("K", SearchViewModel.FirstActiveAntigen(new[] { k, lub }));
        Assert.Equal("Lub", SearchViewModel.LastActiveAntigen(new[] { k, lub }));
        Assert.Equal(new[] { "K", "Lub" }, SearchViewModel.ActiveAntigens(new[] { k, lub }));
        var columns = new[] { "D", "C", "E", "K", "P1" };
        Assert.Equal(columns, SearchViewModel.ResultColumnOrder(columns, new[] { "D" }));
        Assert.Equal(new[] { "D", "P1", "C", "E", "K" },
            SearchViewModel.ResultColumnOrder(columns, new[] { "D", "P1" }));
        Assert.Equal(0, SearchViewModel.FrozenAntigenColumnCount(columns, null));
        Assert.Equal(0, SearchViewModel.FrozenAntigenColumnCount(columns, new[] { "D" }));
        Assert.Equal(2, SearchViewModel.FrozenAntigenColumnCount(columns, new[] { "D", "P1" }));
        Assert.Equal(2, SearchViewModel.FrozenAntigenColumnCount(columns, new[] { "D", "Vel", "K" }));
        Assert.Equal(0, SearchViewModel.FrozenAntigenColumnCount(new[] { "D", "C", "E", "K" }, new[] { "D", "P1" }));
        var lot = new Panel { PanelId = 7, Name = "P", LotNumber = "L" };
        var untyped = new SearchResultRow(lot, new PanelCell { CellNumber = "1" });
        var diaCell = new PanelCell { CellNumber = "2" };
        diaCell.SetAntigen("Dia", "+");
        var diaRow = new SearchResultRow(lot, diaCell);
        var warehouse = new[] { "Kna", "Dia" };
        Assert.Empty(SearchViewModel.BlankWarehouseColumns(warehouse, Array.Empty<SearchResultRow>()));
        Assert.Equal(warehouse, SearchViewModel.BlankWarehouseColumns(warehouse, new[] { untyped }));
        Assert.Equal(new[] { "Kna" }, SearchViewModel.BlankWarehouseColumns(warehouse, new[] { untyped, diaRow }));
        Assert.Equal("NT", untyped.Extra["Dia"]);
        Assert.Equal("NT", diaRow.Extra["Kna"]);
        Assert.Equal("+", diaRow.Extra["Dia"]);
        Assert.Equal("Rules: 2.", ScreenStatus.Rules(2));
        Assert.Equal("Rules: 2. Selected anti-K heterozygous.", ScreenStatus.Rules(2, "anti-K heterozygous"));
    }
}
