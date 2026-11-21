using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Search spans every stored lot, so it is how selected cells are found. A result from a lot that is inactive or
/// past its expiration date must say so, or a cell no longer in use could be chosen for testing.
/// </summary>
public class SearchLotStatusTests
{
    [Fact]
    public void SearchResults_CarryEachLotsActiveFlagAndExpiry()
    {
        using var iso = new IsolatedDatabase();
        var current = iso.Db.AddPanel("Current", "LOT-C", "V", 1, DateTime.Today.AddDays(60).ToString("yyyy-MM-dd"), false);
        var retired = iso.Db.AddPanel("Retired", "LOT-R", "V", 1, null, false, isActive: false);
        var old = iso.Db.AddPanel("Old", "LOT-O", "V", 1, DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd"), false, isActive: false);
        foreach (var id in new[] { current, retired, old })
            iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(id)[0].Id, "Kpa", "+");

        var rows = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { ["Kpa"] = "+" })
            .Select(m => new SearchResultRow(m.panel, m.cell))
            .ToDictionary(r => r.PanelId);

        Assert.True(rows[current].LotInUse);
        Assert.Equal("LOT-C", rows[current].LotDisplay);
        Assert.False(rows[retired].LotInUse);
        Assert.Equal("LOT-R · inactive", rows[retired].LotDisplay);
        Assert.False(rows[old].LotInUse);
        Assert.StartsWith("LOT-O · expired ", rows[old].LotDisplay);
        // The raw lot number is unchanged for keys and other uses.
        Assert.Equal("LOT-O", rows[old].LotNumber);
    }

    [Theory]
    [InlineData(true, "2026-12-01", null)]
    [InlineData(true, null, null)]
    [InlineData(false, null, "inactive")]
    [InlineData(false, "2026-11-01", "expired 2026-11-01")]
    [InlineData(true, "2026-11-01", "expired 2026-11-01")]
    public void LotStatus_ExpiredWinsOverInactive(bool active, string? expiry, string? expected)
    {
        var panel = new Panel { Name = "P", IsActive = active, ExpirationDate = expiry };
        Assert.Equal(expected, SearchResultRow.LotStatus(panel, new DateTime(2026, 11, 21)));
    }

    [Fact]
    public void ResultStatus_CountsCellsOnLotsNotInUse()
    {
        Assert.Equal("Found 3 matching cells for 1 criterion.", SearchViewModel.FormatResultStatus(3, 1, null));
        Assert.Equal("Found 3 matching cells for 1 criterion. 1 is on an inactive or expired lot (marked in the Lot column).",
            SearchViewModel.FormatResultStatus(3, 1, null, 1));
        Assert.Contains("2 are on an inactive or expired lot", SearchViewModel.FormatResultStatus(3, 2, null, 2));
    }

    [Fact]
    public void SelectedResultLabel_NamesTheLotStatus()
    {
        var panel = new Panel { PanelId = 1, Name = "Old", LotNumber = "LOT-O", IsActive = false };
        var row = new SearchResultRow(panel, new PanelCell { CellNumber = "4" });
        Assert.Equal("Old (LOT-O · inactive) cell 4", SearchViewModel.FormatSelectedResult(row));
    }
}
