using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The printed record lists nonreactive cells that did not count toward a rule-out, with the reason, as the
/// Analysis tab's Special Panels does.
/// </summary>
public class GatedRuleoutReportTests
{
    private static IsolatedDatabase TwoCells(string acc, string cell1Cc)
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen(acc, "serum", null);
        var panel = iso.Db.AddPanel("G", "G-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel(acc, panel);
        foreach (var cell in iso.Db.GetPanelCells(panel).Where(c => c.CellNumber != "AC"))
            iso.Db.UpdatePanelCellAntigen(cell.Id, "K", "++");
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", cell1Cc);
        iso.Db.SaveReaction(acc, panel, "2", "0", "0", "0", "2+");
        return iso;
    }

    [Theory]
    [InlineData(ReportType.AnalysisResults)]
    [InlineData(ReportType.ClinicalIdentification)]
    [InlineData(ReportType.SpecimenSummary)]
    public void FailedCheckCell_IsListedUnderRuleoutsNotCounted(ReportType type)
    {
        using var iso = TwoCells("GR-1", "0");

        var text = iso.Reports.GeneratePreviewText(type, "GR-1");

        Assert.Contains("Rule-outs not counted (1):", text);
        Assert.Contains("  anti-K: Cell 1 is AHG 0 but its check cell did not react, so the result is invalid and " +
                        "cannot rule this antibody out. Repeat the test.", text);
    }

    [Theory]
    [InlineData(ReportType.AnalysisResults)]
    [InlineData(ReportType.ClinicalIdentification)]
    [InlineData(ReportType.SpecimenSummary)]
    public void NoGatedCells_NoSection(ReportType type)
    {
        using var iso = TwoCells("GR-2", "2+");

        var text = iso.Reports.GeneratePreviewText(type, "GR-2");

        Assert.DoesNotContain("Rule-outs not counted", text);
    }

    [Fact]
    public void SpecimenSummaryCsv_ListsRuleoutsNotCounted_WithTheReason()
    {
        using var iso = TwoCells("GR-3", "0");
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_gated_{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.SpecimenSummary, path, "GR-3");
            var lines = File.ReadAllLines(path);
            var section = Array.IndexOf(lines, "RuleoutsNotCounted");
            Assert.True(section > 0, string.Join(Environment.NewLine, lines));
            Assert.Equal("Antibody,Reason", lines[section + 1]);
            // The reason contains commas, so CSV quoting wraps it.
            Assert.StartsWith("anti-K,\"Cell 1 is AHG 0 but its check cell did not react", lines[section + 2]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SpecimenSummaryCsv_NoGatedCells_NoSection()
    {
        using var iso = TwoCells("GR-4", "2+");
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_gated_{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.SpecimenSummary, path, "GR-4");
            Assert.DoesNotContain("RuleoutsNotCounted", File.ReadAllLines(path));
        }
        finally { File.Delete(path); }
    }
}
