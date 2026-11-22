using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The Analysis Results report prints the analysis's Suggestions, warnings and REVIEW notes first, as the Analysis
/// tab shows them, so a printed analysis never reads cleaner than the screen it came from.
/// </summary>
public class AnalysisResultsReportTests
{
    private static IsolatedDatabase Analyzed(string acc, string hemolysis)
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("AR " + acc, "L-" + acc, "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        cells[0].SetAntigen("K", "+");
        iso.Db.UpdatePanelCell(cells[0]);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction(acc, panel, "2", "0", "0", "0", "2+");
        iso.Db.UpdateSpecimenHemolysis(acc, hemolysis);
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        return iso;
    }

    [Fact]
    public void Report_PrintsEverySuggestion_InTheAnalysisOrder()
    {
        using var iso = Analyzed("AR-1", SpecimenHemolysis.None);
        var suggestions = iso.Analyzer.AnalyzeSpecimen("AR-1", updateDb: false).Suggestions;
        Assert.NotEmpty(suggestions);

        var text = iso.Reports.GeneratePreviewText(ReportType.AnalysisResults, "AR-1");
        var section = text.IndexOf("SUGGESTIONS (warnings and review items first):", StringComparison.Ordinal);
        Assert.True(section >= 0, text);
        var at = section;
        foreach (var s in suggestions)
        {
            var next = text.IndexOf("• " + s, at, StringComparison.Ordinal);
            Assert.True(next > at, $"missing or out of order: {s}");
            at = next;
        }
    }

    [Fact]
    public void GrosslyHemolyzedSpecimen_ItsReviewNoteIsPrinted()
    {
        using var iso = Analyzed("AR-2", SpecimenHemolysis.Gross);
        var suggestions = iso.Analyzer.AnalyzeSpecimen("AR-2", updateDb: false).Suggestions;
        var hemolysis = Assert.Single(suggestions, s => s.Contains("hemoly", StringComparison.OrdinalIgnoreCase));

        var text = iso.Reports.GeneratePreviewText(ReportType.AnalysisResults, "AR-2");
        Assert.Contains(hemolysis, text);
    }
}
