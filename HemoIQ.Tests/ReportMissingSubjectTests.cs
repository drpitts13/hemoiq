using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A specimen or lot can be deleted on another workstation while the Reports tab still lists it. Printing, copying or
/// exporting must then be refused, not issue a "not found" page that the audit log records as an issued report.
/// </summary>
public class ReportMissingSubjectTests
{
    private static string TempFile(string ext) => Path.Combine(Path.GetTempPath(), $"hemoiq-missing-{Guid.NewGuid():N}.{ext}");

    [Fact]
    public void AnExistingSpecimen_CanBeIssued()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RMS-1", "serum", null);
        Assert.Null(iso.Reports.MissingSubject(ReportType.ClinicalIdentification, "RMS-1", null));
    }

    [Theory]
    [InlineData("pdf")]
    [InlineData("csv")]
    public void ADeletedSpecimen_IsRefused_AndNoOutputIsRecorded(string kind)
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RMS-2", "serum", null);
        iso.Db.DeleteSpecimen("RMS-2");

        var why = iso.Reports.MissingSubject(ReportType.SpecimenSummary, "RMS-2", null);
        Assert.NotNull(why);
        Assert.Contains("RMS-2 no longer exists", why);

        var file = TempFile(kind);
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            if (kind == "pdf") iso.Reports.ExportToPdf(ReportType.SpecimenSummary, file, "RMS-2");
            else iso.Reports.ExportToCsv(ReportType.SpecimenSummary, file, "RMS-2");
        });
        Assert.Contains("not printed, copied or exported", ex.Message);
        Assert.False(File.Exists(file));
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("specimen", "RMS-2"), e => e.Action == "report_output");
    }

    [Fact]
    public void ADeletedPanel_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("RMS", "L-RMS", "V", 2, null, false, startTypedNegative: true);
        iso.Db.DeletePanel(panel);
        Assert.Contains($"Panel #{panel} no longer exists", iso.Reports.MissingSubject(ReportType.PanelAntigram, null, panel));
        var file = TempFile("pdf");
        Assert.Throws<InvalidOperationException>(() => iso.Reports.ExportToPdf(ReportType.PanelAntigram, file, null, panel));
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void AListReport_IgnoresAStaleSelection()
    {
        using var iso = new IsolatedDatabase();
        Assert.Null(iso.Reports.MissingSubject(ReportType.AllSpecimens, "GONE-1", 999));
    }

    [Fact]
    public void CopyPrintAndExport_CheckBeforeIssuing()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReportsViewModel.cs"));
        foreach (var method in new[] { "CopyPreview", "PrintPreview", "ExportCsv", "ExportPdf" })
        {
            var body = src[src.IndexOf($"private void {method}()")..];
            body = body[..body.IndexOf("try")];
            Assert.Contains("if (RefusedMissingSubject()) return;", body);
        }
    }
}
