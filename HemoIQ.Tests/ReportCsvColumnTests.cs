using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Each report CSV carries the columns of its printed counterpart: All Specimens the final ID (in full, where the
/// printed column truncates), All Panels the typing summary, and Panel Summary the lot details and each cell's donor,
/// Rh phenotype and special types.
/// </summary>
public class ReportCsvColumnTests
{
    private static string[] Export(IsolatedDatabase iso, ReportType type, int? panelId = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq-rcsv-{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(type, path, panelId: panelId);
            return File.ReadAllLines(path);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void AllSpecimensCsv_HasTheFinalIdInFull()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("FI-1", "serum", null);
        const string longId = "anti-E, anti-K, anti-Fya, anti-Jkb, anti-S";
        iso.Db.SetSpecimenFinalCall("FI-1", longId, null, "TE");
        var row = Export(iso, ReportType.AllSpecimens).Single(l => l.StartsWith("FI-1,"));
        Assert.Contains(longId, row);
    }

    [Fact]
    public void AllPanelsCsv_HasTheTypingSummary()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Typed", "L-TY", "V", 2, null, false, startTypedNegative: true);
        var lines = Export(iso, ReportType.AllPanels);
        var expected = PanelIntegrityCheck.Summarize(iso.Db.GetPanelCells(id)).Display;
        Assert.Contains(lines, l => l.Contains(",L-TY,") && l.Contains(expected.Split(',')[0]));
    }

    [Fact]
    public void PanelSummaryCsv_HasLotDetailsAndCellDetails()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Ficin", "L-FI", "V", 2, null, false, startTypedNegative: true);
        iso.Db.SetPanelVendorMetadata(id, "CAT-9", "Panocell", enzymeTreated: true, null, null, null, "keep cold");
        var cells = iso.Db.GetPanelCells(id);
        cells[0].DonorId = "D-1"; cells[0].RhPhenotype = "R1R1"; cells[0].SpecialTypes = "Yt(a-)";
        iso.Db.UpdatePanelCell(cells[0]);

        var lines = Export(iso, ReportType.PanelSummary, id);
        Assert.Equal("Panel", lines[0].Trim('"'));
        Assert.Contains(lines, l => l.StartsWith("EnzymeTreated,") && l.EndsWith("Yes"));
        Assert.Contains(lines, l => l.StartsWith("CatalogNumber,") && l.Contains("CAT-9"));
        var header = lines.First(l => l.StartsWith("Cell,"));
        Assert.EndsWith(",Donor,Rh,SpecialTypes", header);
        var one = lines.First(l => l.StartsWith("1,"));
        Assert.EndsWith(",D-1,R1R1,Yt(a-)", one);
    }
}
