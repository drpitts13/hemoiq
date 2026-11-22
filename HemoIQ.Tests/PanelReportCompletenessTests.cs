using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The panel reports and the worksheet print what the vendor sheet says beyond the antigen grid: an enzyme-treated
/// lot, product and catalog, lot notes, and each cell's donor, Rh phenotype and special types (which analysis reads).
/// </summary>
public class PanelReportCompletenessTests
{
    private static (IsolatedDatabase Iso, int Panel) TreatedLot()
    {
        var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Ficin lot", "L-FIC", "V", 2, null, false, startTypedNegative: true);
        iso.Db.SetPanelVendorMetadata(id, "CAT-77", "Panocell-F", enzymeTreated: true, null, null, null, "store at 2-8 C");
        var cells = iso.Db.GetPanelCells(id);
        cells[0].DonorId = "D-100";
        cells[0].RhPhenotype = "R1R1";
        cells[0].SpecialTypes = "Yt(a-)";
        iso.Db.UpdatePanelCell(cells[0]);
        return (iso, id);
    }

    [Theory]
    [InlineData(ReportType.PanelSummary)]
    [InlineData(ReportType.PanelAntigram)]
    public void PanelReports_PrintTreatmentProductNotesAndCellDetails(ReportType type)
    {
        var (iso, id) = TreatedLot();
        using (iso)
        {
            var text = iso.Reports.GeneratePreviewText(type, null, id);
            Assert.Contains("ENZYME-TREATED", text);
            Assert.Contains("Product: Panocell-F", text);
            Assert.Contains("Catalog: CAT-77", text);
            Assert.Contains("Lot notes:  store at 2-8 C", text);
            Assert.Contains("Cell 1: donor D-100; Rh R1R1; special types Yt(a-)", text);
            Assert.DoesNotContain("Cell 2:", text); // nothing recorded beyond the grid
        }
    }

    [Fact]
    public void Worksheet_PrintsTheLotsTreatmentAndCellDetailsUnderEachRun()
    {
        var (iso, id) = TreatedLot();
        using (iso)
        {
            iso.Db.AddSpecimen("PR-1", "serum", null);
            iso.Db.LinkSpecimenPanel("PR-1", id);
            iso.Db.SaveReaction("PR-1", id, "1", "0", "0", "0", "2+");
            var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "PR-1");
            Assert.Contains("ENZYME-TREATED", text);
            Assert.Contains("special types Yt(a-)", text);
        }
    }

    [Fact]
    public void PlainLot_AddsNothing()
    {
        var p = new Panel { Name = "Plain" };
        Assert.Empty(ReportService.PanelProductLines(p));
        Assert.Empty(ReportService.CellNoteLines(new[] { new PanelCell { CellNumber = "1" } }));
    }
}
