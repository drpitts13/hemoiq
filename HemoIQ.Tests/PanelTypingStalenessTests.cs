using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Correcting a panel's typing changes the evidence behind every analysis that used it, so open investigations on
/// that panel become out of date, as after a grade change. Confirmed and cancelled investigations are not reopened.
/// </summary>
// Staleness compares each analysis with the global settings, so run apart from tests that change them.
[Collection("PersistentDatabase")]
public class PanelTypingStalenessTests
{
    private static (IsolatedDatabase Iso, int Panel) PanelWith(params string[] accessions)
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("TS", "TS-1", "V", 2, null, false, startTypedNegative: true);
        foreach (var acc in accessions)
        {
            iso.Db.AddSpecimen(acc, "serum", null);
            iso.Db.LinkSpecimenPanel(acc, panel);
            iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", "2+");
            iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        }
        return (iso, panel);
    }

    private static void RetypeK(IsolatedDatabase iso, int panel)
    {
        var cells = iso.Db.GetPanelCells(panel);
        cells.Single(c => c.CellNumber == "1").Antigens["K"] = "+";
        iso.Db.SavePanelCellEdits(panel, cells, "Vendor corrected antigram");
    }

    [Fact]
    public void TypingEdit_MarksOpenInvestigationsOnThePanelOutOfDate()
    {
        var (iso, panel) = PanelWith("TS-OPEN");
        using var _ = iso;
        Assert.False(iso.Db.IsSpecimenAnalysisStale("TS-OPEN"));

        RetypeK(iso, panel);

        Assert.True(iso.Db.IsSpecimenAnalysisStale("TS-OPEN"));
        Assert.Contains("Reactions, panel typing or specimen details changed since last analysis.", iso.Db.AnalysisStaleText("TS-OPEN"));
    }

    [Fact]
    public void TypingEdit_DoesNotReopenConfirmedOrCancelledInvestigations()
    {
        var (iso, panel) = PanelWith("TS-DONE", "TS-CX");
        using var _ = iso;
        iso.Db.SetSpecimenFinalCall("TS-DONE", "NAD", null, "TE");
        iso.Db.CancelInvestigation("TS-CX", "Duplicate order");
        var doneBefore = iso.Db.GetSpecimen("TS-DONE")!.ReactionsUpdatedAt;
        var cxBefore = iso.Db.GetSpecimen("TS-CX")!.ReactionsUpdatedAt;

        RetypeK(iso, panel);

        Assert.Equal(doneBefore, iso.Db.GetSpecimen("TS-DONE")!.ReactionsUpdatedAt);
        Assert.Equal(cxBefore, iso.Db.GetSpecimen("TS-CX")!.ReactionsUpdatedAt);
        Assert.Empty(iso.Db.OpenInvestigationsOnPanel(panel));
    }

    [Fact]
    public void SaveWithoutChanges_MarksNothing()
    {
        var (iso, panel) = PanelWith("TS-SAME");
        using var _ = iso;

        iso.Db.SavePanelCellEdits(panel, iso.Db.GetPanelCells(panel));

        Assert.False(iso.Db.IsSpecimenAnalysisStale("TS-SAME"));
    }
}
