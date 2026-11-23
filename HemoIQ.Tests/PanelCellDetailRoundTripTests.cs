using System.Text;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// A panel exported to CSV and imported again keeps each cell's donor ID, Rh phenotype and special types, which the
/// printed antigram shows and analysis reads (special types). A structured (JSON) file's donor and Rh are kept too.
/// </summary>
public class PanelCellDetailRoundTripTests
{
    private static string TempCsv() => Path.Combine(Path.GetTempPath(), $"hemoiq-cells-{Guid.NewGuid():N}.csv");

    private static List<PanelCell> Cells()
    {
        var a = new PanelCell { CellNumber = "1", DonorId = "D-100", RhPhenotype = "R1R1", SpecialTypes = "Yt(a-)" };
        a.SetAntigen("D", "+"); a.SetAntigen("K", "-");
        var b = new PanelCell { CellNumber = "2" };
        b.SetAntigen("D", "-"); b.SetAntigen("K", "+");
        return new List<PanelCell> { a, b };
    }

    [Fact]
    public void CsvRoundTrip_KeepsDonorRhAndSpecialTypes()
    {
        var path = TempCsv();
        try
        {
            PanelCsvService.Export(Cells(), path);
            var header = File.ReadLines(path).First();
            Assert.Contains("Donor", header);
            Assert.Contains("Rh", header);
            Assert.Contains("Special types", header);

            var imported = PanelCsvService.Import(path);
            Assert.True(imported.Success, string.Join("; ", imported.Errors));
            Assert.Empty(imported.UnknownHeaders);
            var stored = LabPanelImportService.ToCells(imported);
            var one = stored.Single(c => c.CellNumber == "1");
            Assert.Equal("D-100", one.DonorId);
            Assert.Equal("R1R1", one.RhPhenotype);
            Assert.Equal("Yt(a-)", one.SpecialTypes);
            Assert.Equal("+", one.GetTypedValue("D"));
            var two = stored.Single(c => c.CellNumber == "2");
            Assert.Null(two.DonorId);
            Assert.Null(two.SpecialTypes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CellsWithoutDetails_ExportTheUsualColumnsOnly()
    {
        var path = TempCsv();
        try
        {
            var cells = Cells();
            cells[0].DonorId = null; cells[0].RhPhenotype = null; cells[0].SpecialTypes = null;
            PanelCsvService.Export(cells, path);
            var header = File.ReadLines(path).First();
            Assert.DoesNotContain("Donor", header);
            Assert.DoesNotContain("Special types", header);
            Assert.StartsWith("Cell,", header);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void OtherHeaderSpellings_AreRead()
    {
        var path = TempCsv();
        try
        {
            File.WriteAllText(path, "Cell,Donor ID,Rh phenotype,D,K,Special\n1,X9,R2R2,+,-,Bg(a+)\n");
            var stored = LabPanelImportService.ToCells(PanelCsvService.Import(path));
            Assert.Equal("X9", stored[0].DonorId);
            Assert.Equal("R2R2", stored[0].RhPhenotype);
            Assert.Equal("Bg(a+)", stored[0].SpecialTypes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void StructuredImport_KeepsDonorAndRh()
    {
        var json = "{\"lotNumber\":\"L1\",\"antigenOrder\":[\"D\"],\"cells\":[{\"cellNumber\":\"1\",\"donorId\":\"D-7\",\"rhPhenotype\":\"R1r\",\"antigens\":{\"D\":\"+\"}}]}";
        var parsed = StructuredPanelParser.ParseJson(Encoding.UTF8.GetBytes(json), "Lab", "lot.json", null);
        var cells = LabPanelImportService.ToCells(StructuredPanelParser.ToLabResult(parsed));
        Assert.Equal("D-7", cells.Single().DonorId);
        Assert.Equal("R1r", cells.Single().RhPhenotype);
    }
}
