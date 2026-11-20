using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Autoadsorption within the recent-transfusion window can remove alloantibodies on transfused donor cells, so a
/// nonreactive autoadsorbed aliquot must not rule them out. Outside the window, or without transfusion, it may.
/// </summary>
[Collection("PersistentDatabase")]
public class AutoadsorptionTransfusionTests
{
    private static string Day(int daysAgo) => DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

    /// <summary>Only an autoadsorbed aliquot is tested: every K+ cell is nonreactive on it.</summary>
    private static IsolatedDatabase AutoadsorbedOnly(string acc, string status, string? lastTransfusion)
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen(acc, "serum", null, createdDate: Day(0));
        iso.Db.UpdateSpecimenTransfusion(acc, status, lastTransfusion);
        var panel = iso.Db.AddPanel("AA", "AA-" + acc, "V", 4, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel(acc, panel);
        foreach (var cell in iso.Db.GetPanelCells(panel).Where(c => c.CellNumber != "AC"))
            iso.Db.UpdatePanelCellAntigen(cell.Id, "K", "+");
        var run = iso.Db.AddPanelRun(acc, panel, CellTreatment.None, SerumTreatment.AutoAdsorption, "Autoadsorbed");
        foreach (var n in new[] { "1", "2", "3", "4" })
            iso.Db.SaveReaction(run, n, "0", "0", "0", "2+");
        return iso;
    }

    [Fact]
    public void RecentlyTransfused_AutoadsorbedNegatives_DoNotRuleOutAlloantibodies()
    {
        using var iso = AutoadsorbedOnly("AA-1", TransfusionStatus.Transfused, Day(10));

        var result = iso.Analyzer.AnalyzeSpecimen("AA-1", updateDb: false);

        Assert.DoesNotContain(result.RuledOut.Keys, k => k == "anti-K");
        var gate = Assert.Single(result.GatedRuleouts, g => g.Antibody == "anti-K");
        Assert.Contains("recently transfused", gate.Reason);
        Assert.Contains("allogeneic (differential) adsorption", gate.Reason);
    }

    [Fact]
    public void NotTransfused_AutoadsorbedNegatives_StillRuleOut()
    {
        using var iso = AutoadsorbedOnly("AA-2", TransfusionStatus.NotTransfused, null);

        var result = iso.Analyzer.AnalyzeSpecimen("AA-2", updateDb: false);

        Assert.Contains(result.RuledOut.Keys, k => k == "anti-K");
        Assert.DoesNotContain(result.GatedRuleouts, g => g.Antibody == "anti-K");
    }

    [Fact]
    public void TransfusedOutsideTheWindow_AutoadsorbedNegatives_StillRuleOut()
    {
        using var iso = AutoadsorbedOnly("AA-3", TransfusionStatus.Transfused, Day(200));

        var result = iso.Analyzer.AnalyzeSpecimen("AA-3", updateDb: false);

        Assert.Contains(result.RuledOut.Keys, k => k == "anti-K");
    }
}
