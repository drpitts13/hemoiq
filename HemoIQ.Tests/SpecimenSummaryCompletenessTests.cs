using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The Specimen Summary and its CSV say which investigation they describe and where it stands (status, confirmed
/// identification, review, cancellation), and an eluate's reports name the red cells it was made from.
/// </summary>
public class SpecimenSummaryCompletenessTests
{
    private static string Summary(IsolatedDatabase iso, string acc) =>
        iso.Reports.GeneratePreviewText(ReportType.SpecimenSummary, acc);

    [Fact]
    public void ConfirmedSpecimen_PrintsStatusFinalIdHemolysisAndPregnancy()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("SS-1", "serum", null);
        iso.Db.UpdateSpecimenHemolysis("SS-1", SpecimenHemolysis.Slight);
        iso.Db.SetSpecimenFinalCall("SS-1", "anti-E", "strong at AHG", "TE");

        var text = Summary(iso, "SS-1");
        Assert.Contains("Status:", text);
        Assert.Contains("Final ID:        anti-E (confirmed by TE on", text);
        Assert.Contains("Final comment:   strong at AHG", text);
        Assert.Contains("Hemolysis:       Slight", text);
        Assert.Contains("Pregnancy:", text);
        Assert.Contains("Received:", text);
        Assert.DoesNotContain("Cancelled:", text);
    }

    [Fact]
    public void CancelledSpecimen_SaysSo_WithTheReason()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("SS-2", "serum", null);
        iso.Db.CancelInvestigation("SS-2", "duplicate request");

        var text = Summary(iso, "SS-2");
        Assert.Contains("Final ID:        Not confirmed", text);
        Assert.Contains("Cancelled:", text);
        Assert.Contains("duplicate request", text);
    }

    [Fact]
    public void Eluate_NamesItsSourceSpecimen_OnTheSummaryAndTheWorksheet()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("SS-RBC", "serum", null);
        iso.Db.UpdateSpecimenMrn("SS-RBC", "MRN-SS");
        iso.Db.AddSpecimen("SS-EL", "eluate", null);
        iso.Db.UpdateSpecimenMrn("SS-EL", "MRN-SS");
        iso.Db.SetEluateSource("SS-EL", "SS-RBC");

        Assert.Contains("Eluate of:       SS-RBC", Summary(iso, "SS-EL"));
        Assert.Contains("Eluate of: SS-RBC", iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "SS-EL"));
        Assert.DoesNotContain("Eluate of", Summary(iso, "SS-RBC"));
    }

    [Fact]
    public void Csv_StartsWithTheSpecimenItDescribes()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("SS-CSV", "serum", null);
        iso.Db.UpdateSpecimenMrn("SS-CSV", "MRN-CSV");
        iso.Db.SetSpecimenFinalCall("SS-CSV", "anti-K", null, "TE");
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq-summary-{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.SpecimenSummary, path, "SS-CSV", null);
            var lines = File.ReadAllLines(path);
            Assert.Equal("Specimen", lines[0].Trim('"'));
            Assert.Contains(lines, l => l.Contains("Accession") && l.Contains("SS-CSV"));
            Assert.Contains(lines, l => l.Contains("MRN") && l.Contains("MRN-CSV"));
            Assert.Contains(lines, l => l.Contains("FinalId") && l.Contains("anti-K"));
            Assert.Contains(lines, l => l.TrimStart('"').StartsWith("Status"));
            Assert.Contains(lines, l => l.Trim('"') == "Antibodies"); // the sections that were there are still there
        }
        finally { File.Delete(path); }
    }
}
