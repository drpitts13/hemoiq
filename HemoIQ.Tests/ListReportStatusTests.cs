using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The All Panels and All Specimens lists, and their CSV, state each record's status, so an inactive or expired lot
/// or a cancelled investigation is not listed as if it were in use.
/// </summary>
public class ListReportStatusTests
{
    private static string Day(int offset) => DateTime.Today.AddDays(offset).ToString("yyyy-MM-dd");

    private static IsolatedDatabase FourPanels()
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddPanel("Live", "L-1", "V", 2, Day(90), false);
        iso.Db.AddPanel("Imported", "I-1", "V", 2, Day(90), false, isActive: false, sourceFormat: "csv", importedAt: "2026-11-20 09:00:00");
        iso.Db.AddPanel("Off", "O-1", "V", 2, Day(90), false, isActive: false);
        iso.Db.AddPanel("Old", "X-1", "V", 2, Day(-2), false);
        return iso;
    }

    private static string RowFor(string text, string lot) =>
        text.Replace("\r\n", "\n").Split('\n').Single(l => l.Contains(" " + lot + " ", StringComparison.Ordinal));

    [Fact]
    public void AllPanels_ShowsEachLotsStatus()
    {
        using var iso = FourPanels();
        var text = iso.Reports.GeneratePreviewText(ReportType.AllPanels);

        Assert.Contains(" Status ", text);
        Assert.Contains(" Active ", RowFor(text, "L-1"));
        Assert.Contains(" Imported, inactive ", RowFor(text, "I-1"));
        Assert.Contains(" Inactive ", RowFor(text, "O-1"));
        Assert.Contains(" Expired ", RowFor(text, "X-1"));
    }

    [Fact]
    public void AllPanelsCsv_HasAStatusColumn()
    {
        using var iso = FourPanels();
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_panels_{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.AllPanels, path);
            var lines = File.ReadAllLines(path);
            Assert.EndsWith(",Status", lines[0]);
            Assert.EndsWith(",Active", lines.Single(l => l.Contains(",L-1,")));
            Assert.EndsWith(",\"Imported, inactive\"", lines.Single(l => l.Contains(",I-1,")));
            Assert.EndsWith(",Expired", lines.Single(l => l.Contains(",X-1,")));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void AllSpecimens_ShowsACancelledInvestigationAsCancelled_InTheReportAndCsv()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("CX-1", "serum", null);
        iso.Db.AddSpecimen("OK-1", "serum", null);
        iso.Db.CancelInvestigation("CX-1", "Duplicate order");

        var text = iso.Reports.GeneratePreviewText(ReportType.AllSpecimens);
        Assert.Contains("Cancelled", text.Replace("\r\n", "\n").Split('\n').Single(l => l.StartsWith("CX-1")));
        Assert.Contains("Not analyzed", text.Replace("\r\n", "\n").Split('\n').Single(l => l.StartsWith("OK-1")));

        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_specimens_{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.AllSpecimens, path);
            var lines = File.ReadAllLines(path);
            Assert.EndsWith(",Status", lines[0]);
            Assert.EndsWith(",Cancelled", lines.Single(l => l.StartsWith("CX-1,")));
        }
        finally { File.Delete(path); }
    }
}
