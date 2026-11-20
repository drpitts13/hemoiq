using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// rr (ce/ce) absorbing cells carry the compound antigen f (ce), so an rr-absorbed aliquot has lost anti-f and its
/// nonreactive f+ cells must not rule anti-f out. R1R1 and R2R2 cells lack f.
/// </summary>
public class AbsorptionCompoundAntigenTests
{
    [Fact]
    public void RrAbsorption_RemovesAntiF_R1R1AndR2R2DoNot()
    {
        Assert.Contains("anti-f", AntigenTreatmentEffects.GetAbsorbedAntibodies(SerumTreatment.AlloAdsorptionRr));
        Assert.DoesNotContain("anti-f", AntigenTreatmentEffects.GetAbsorbedAntibodies(SerumTreatment.AlloAdsorptionR1R1));
        Assert.DoesNotContain("anti-f", AntigenTreatmentEffects.GetAbsorbedAntibodies(SerumTreatment.AlloAdsorptionR2R2));
    }

    [Fact]
    public void RrAbsorbedAliquot_NonreactiveFPositiveCells_DoNotRuleOutAntiF()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("ABS-F", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L-F", "V", 4, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel("ABS-F", panelId);
        foreach (var cell in iso.Db.GetPanelCells(panelId).Where(c => c.CellNumber != "AC"))
            iso.Db.UpdatePanelCellAntigen(cell.Id, "f", "+");
        // Only the absorbed aliquot is tested: every f+ cell is nonreactive after rr absorption.
        var adsorbed = iso.Db.AddPanelRun("ABS-F", panelId, CellTreatment.None, SerumTreatment.AlloAdsorptionRr, "Absorbed: rr");
        foreach (var n in new[] { "1", "2", "3", "4" })
            iso.Db.SaveReaction(adsorbed, n, "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("ABS-F", updateDb: false);

        Assert.DoesNotContain(result.RuledOut.Keys, k => k == "anti-f");
    }

    [Fact]
    public void UntreatedRun_NonreactiveFPositiveCells_StillRuleOutAntiF()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("ABS-F2", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L-F2", "V", 4, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel("ABS-F2", panelId);
        foreach (var cell in iso.Db.GetPanelCells(panelId).Where(c => c.CellNumber != "AC"))
            iso.Db.UpdatePanelCellAntigen(cell.Id, "f", "+");
        var untreated = iso.Db.GetOrCreateDefaultRun("ABS-F2", panelId);
        foreach (var n in new[] { "1", "2", "3", "4" })
            iso.Db.SaveReaction(untreated, n, "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("ABS-F2", updateDb: false);

        Assert.Contains(result.RuledOut.Keys, k => k == "anti-f");
    }
}
