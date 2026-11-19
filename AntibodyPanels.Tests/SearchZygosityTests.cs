using AntibodyPanels.Models;
using AntibodyPanels.Services;
using AntibodyPanels.Tests.Infrastructure;
using AntibodyPanels.ViewModels;

namespace AntibodyPanels.Tests;

public class SearchZygosityTests
{
    [Fact]
    public void Both_FindsHomozygousAndHeterozygousCells()
    {
        using var iso = CreateCcPanel(out var homo, out var het, out var neg);
        var matches = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["C"] = "+" },
            AntigenConstants.ZygosityBoth);

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.cell.CellNumber == homo);
        Assert.Contains(matches, m => m.cell.CellNumber == het);
        Assert.DoesNotContain(matches, m => m.cell.CellNumber == neg);
    }

    [Fact]
    public void Homozygous_FindsOnlyAntigenPositiveAntitheticalNegative()
    {
        using var iso = CreateCcPanel(out var homo, out var het, out _);
        var matches = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["C"] = "+" },
            AntigenConstants.ZygosityHomozygous);

        Assert.Single(matches);
        Assert.Equal(homo, matches[0].cell.CellNumber);
        Assert.Equal("+", matches[0].cell.GetAntigen("C"));
        Assert.Equal("-", matches[0].cell.GetAntigen("c"));
        Assert.DoesNotContain(matches, m => m.cell.CellNumber == het);
    }

    [Fact]
    public void Heterozygous_FindsOnlyBothAllelesPositive()
    {
        using var iso = CreateCcPanel(out var homo, out var het, out _);
        var matches = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["C"] = "+" },
            AntigenConstants.ZygosityHeterozygous);

        Assert.Single(matches);
        Assert.Equal(het, matches[0].cell.CellNumber);
        Assert.Equal("+", matches[0].cell.GetAntigen("C"));
        Assert.Equal("+", matches[0].cell.GetAntigen("c"));
        Assert.DoesNotContain(matches, m => m.cell.CellNumber == homo);
    }

    [Fact]
    public void NegativeCriteria_IgnoreZygosity()
    {
        using var iso = CreateCcPanel(out _, out _, out var neg);
        var matches = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["C"] = "-" },
            AntigenConstants.ZygosityHomozygous);

        Assert.Single(matches);
        Assert.Equal(neg, matches[0].cell.CellNumber);
    }

    [Fact]
    public void UnpairedAntigen_StaysPositiveWhenZygosityIsHomozygous()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("D panel", "L", "V", 2, null, false);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "D", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "D", "-");

        var matches = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["D"] = "+" },
            AntigenConstants.ZygosityHomozygous);

        Assert.Single(matches);
        Assert.Equal("+", matches[0].cell.GetAntigen("D"));
    }

    [Fact]
    public void Warehouse_HomozygousAndHeterozygous()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Do panel", "L", "V", 3, null, false);
        iso.Db.AddPanelExtraAntigen(panelId, "Doa");
        iso.Db.AddPanelExtraAntigen(panelId, "Dob");
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "Doa", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "Dob", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "Doa", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "Dob", "+");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "Doa", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "Dob", "+");

        var both = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityBoth);
        var homo = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityHomozygous);
        var het = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityHeterozygous);

        Assert.Equal(2, both.Count);
        Assert.Single(homo);
        Assert.Equal("+", homo[0].cell.GetAntigen("Doa"));
        Assert.Equal("-", homo[0].cell.GetAntigen("Dob"));
        Assert.Single(het);
        Assert.Equal("+", het[0].cell.GetAntigen("Doa"));
        Assert.Equal("+", het[0].cell.GetAntigen("Dob"));
    }

    [Fact]
    public void Warehouse_UnknownZygosityExcludedFromHomoAndHet()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Doa only", "L", "V", 1, null, false);
        iso.Db.AddPanelExtraAntigen(panelId, "Doa");
        var cell = iso.Db.GetPanelCells(panelId).Single();
        iso.Db.UpdatePanelCellAntigen(cell.Id, "Doa", "+");

        var both = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityBoth);
        var homo = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityHomozygous);
        var het = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["Doa"] = "+" },
            AntigenConstants.ZygosityHeterozygous);

        Assert.Single(both);
        Assert.Empty(homo);
        Assert.Empty(het);
    }

    [Fact]
    public void WarmAuto_NegativeSearchSkipsNt_AndResultShowsNt()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var dNegative = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["D"] = "-" });
        Assert.DoesNotContain(dNegative, m => m.panel.LotNumber == "E2E-WARM-AUTO");

        var ePositive = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["E"] = "+" });
        var warmE = Assert.Single(ePositive, m =>
            m.panel.LotNumber == "E2E-WARM-AUTO" && m.cell.CellNumber == "1");
        var row = new SearchResultRow(warmE.panel, warmE.cell);
        Assert.Equal("+", row.E);
        Assert.Equal("NT", row.D);

        var kNegative = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["K"] = "-" });
        var warmK = Assert.Single(kNegative, m =>
            m.panel.LotNumber == "E2E-WARM-AUTO" && m.cell.CellNumber == "1");
        Assert.Equal("-", new SearchResultRow(warmK.panel, warmK.cell).K);
    }

    [Fact]
    public void WarmAutoAllos_EPositiveKeepsTypedKMinus_AndDNegativeSkipsNt()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var ePositive = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["E"] = "+" });
        var allos = ePositive
            .Where(m => m.panel.LotNumber == E2EWorkflowSeeder.WarmAutoAllos)
            .OrderBy(m => m.cell.CellNumber)
            .ToList();
        Assert.Equal(new[] { "1", "3", "5" }, allos.Select(m => m.cell.CellNumber).ToArray());

        var cell1 = new SearchResultRow(allos[0].panel, allos[0].cell);
        Assert.Equal("+", cell1.E);
        Assert.Equal("-", cell1.K);
        Assert.Equal("-", cell1.c);
        Assert.Equal("NT", cell1.D);

        var cell3 = new SearchResultRow(allos[1].panel, allos[1].cell);
        Assert.Equal("+", cell3.K);

        var dNegative = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["D"] = "-" });
        Assert.DoesNotContain(dNegative, m => m.panel.LotNumber == E2EWorkflowSeeder.WarmAutoAllos);
    }

    [Fact]
    public void WarmAutoAllos_HomozygousE_SkipsCellsWhosePartnerIsNt()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var both = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["E"] = "+" },
            AntigenConstants.ZygosityBoth);
        var allos = both
            .Where(m => m.panel.LotNumber == E2EWorkflowSeeder.WarmAutoAllos)
            .OrderBy(m => m.cell.CellNumber)
            .ToList();
        Assert.Equal(new[] { "1", "3", "5" }, allos.Select(m => m.cell.CellNumber).ToArray());
        Assert.All(allos, m => Assert.False(m.cell.IsHomozygousFor("E")));
        Assert.Equal("NT", new SearchResultRow(allos[0].panel, allos[0].cell).e);

        var homo = iso.Db.SearchCellsByProfile(
            new Dictionary<string, string> { ["E"] = "+" },
            AntigenConstants.ZygosityHomozygous);
        Assert.DoesNotContain(homo, m => m.panel.LotNumber == E2EWorkflowSeeder.WarmAutoAllos);
        Assert.All(homo, m => Assert.True(m.cell.IsHomozygousFor("E")));

        Assert.Equal(
            "Found 3 matching cells for 1 criterion. Positive cells are homozygous.",
            SearchViewModel.FormatResultStatus(3, 1, AntigenConstants.ZygosityHomozygous));
        Assert.Equal(
            "Found 141 matching cells for 1 criterion.",
            SearchViewModel.FormatResultStatus(141, 1, AntigenConstants.ZygosityBoth));
        Assert.Equal(
            "Found 4 matching cells for 2 criteria. Positive cells are heterozygous.",
            SearchViewModel.FormatResultStatus(4, 2, AntigenConstants.ZygosityHeterozygous));
    }

    private static IsolatedDatabase CreateCcPanel(out string homoCell, out string hetCell, out string negCell)
    {
        var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Cc panel", "L", "V", 3, null, false);
        var cells = iso.Db.GetPanelCells(panelId);

        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "C", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "c", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "C", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "c", "+");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "C", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "c", "+");

        homoCell = cells[0].CellNumber;
        hetCell = cells[1].CellNumber;
        negCell = cells[2].CellNumber;
        return iso;
    }
}
