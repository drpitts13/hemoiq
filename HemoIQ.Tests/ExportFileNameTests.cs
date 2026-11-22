using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// Save dialogs suggest file names built from the panel name and lot the user typed. They must be valid Windows
/// file names, and two lots of one panel must not be offered the same file.
/// </summary>
public class ExportFileNameTests
{
    [Fact]
    public void IncludesTheLot_SoTwoLotsAreNotOfferedTheSameFile()
    {
        var a = ExportFileName.ForPanel(new Panel { Name = "Resolve A", LotNumber = "L100" }, "antigram", "pdf");
        var b = ExportFileName.ForPanel(new Panel { Name = "Resolve A", LotNumber = "L200" }, "antigram", "pdf");
        Assert.Equal("Resolve A lot L100 antigram.pdf", a);
        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData("Panel A/B", "Panel A_B lot 7.csv")]
    [InlineData("Screen: 3-cell?", "Screen_ 3-cell_ lot 7.csv")]
    [InlineData("Cells \"selected\" <x>", "Cells _selected_ _x_ lot 7.csv")]
    public void CharactersWindowsRefuses_AreReplaced(string name, string expected) =>
        Assert.Equal(expected, ExportFileName.ForPanel(new Panel { Name = name, LotNumber = "7" }, "", "csv"));

    [Fact]
    public void Result_IsAValidFileName()
    {
        var name = ExportFileName.ForPanel(new Panel { Name = "a|b*c\\d\te.", LotNumber = "x/y" }, "", "csv");
        Assert.Equal(-1, name.IndexOfAny(Path.GetInvalidFileNameChars()));
        Assert.EndsWith(".csv", name);
    }

    [Fact]
    public void NoLot_AndLongNames_AreHandled()
    {
        Assert.Equal("Selected.csv", ExportFileName.ForPanel(new Panel { Name = "Selected" }, "", "csv"));
        var longName = ExportFileName.ForPanel(new Panel { Name = new string('x', 300) }, "", "csv");
        Assert.True(longName.Length <= 124);
        Assert.Equal("panel", ExportFileName.Clean("///".Replace("/", "")));
    }
}
