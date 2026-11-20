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
    public void NoGatedCells_NoSection(ReportType type)
    {
        using var iso = TwoCells("GR-2", "2+");

        var text = iso.Reports.GeneratePreviewText(type, "GR-2");

        Assert.DoesNotContain("Rule-outs not counted", text);
    }
}
