using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Editing a lot's cells marks open investigations on it stale only when the edit changes something analysis reads
/// (antigen typing or special types). A donor ID or Rh phenotype label is not an analysis input; a needless "stale"
/// warning teaches users to ignore real ones.
/// </summary>
[Collection("PersistentDatabase")]
public class PanelMetadataStalenessTests
{
    private static (IsolatedDatabase Iso, int Panel) Analyzed()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Meta", "L-META", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("META-1", "serum", null);
        iso.Db.LinkSpecimenPanel("META-1", panel);
        iso.Db.SaveReaction("META-1", panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen("META-1", updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale("META-1"));
        return (iso, panel);
    }

    private static List<PanelCell> Edit(IsolatedDatabase iso, int panel, Action<PanelCell> change)
    {
        var cells = iso.Db.GetPanelCells(panel);
        change(cells[0]);
        return cells;
    }

    [Theory]
    [InlineData("donor")]
    [InlineData("rh")]
    public void LabelOnlyEdits_AreSavedAndAudited_ButDoNotMarkAnalysesStale(string field)
    {
        var (iso, panel) = Analyzed();
        using (iso)
        {
            var cells = Edit(iso, panel, c =>
            {
                if (field == "donor") c.DonorId = "D-12345"; else c.RhPhenotype = "R1R1";
            });
            var changes = iso.Db.SavePanelCellEdits(panel, cells, "label correction");

            Assert.Single(changes);
            Assert.Contains(iso.Db.GetAuditEventsForEntity("panel", panel.ToString()), e => e.Action == "edit_panel_typing");
            Assert.False(iso.Db.IsSpecimenAnalysisStale("META-1"));
        }
    }

    [Fact]
    public void SpecialTypesAndAntigenEdits_StillMarkAnalysesStale()
    {
        var (iso, panel) = Analyzed();
        using (iso)
        {
            iso.Db.SavePanelCellEdits(panel, Edit(iso, panel, c => c.SpecialTypes = "Yt(a-)"), "special types");
            Assert.True(iso.Db.IsSpecimenAnalysisStale("META-1"));
        }

        var (iso2, panel2) = Analyzed();
        using (iso2)
        {
            iso2.Db.SavePanelCellEdits(panel2, Edit(iso2, panel2, c => c.SetAntigen("K", "+")), "typing");
            Assert.True(iso2.Db.IsSpecimenAnalysisStale("META-1"));
        }
    }

    [Theory]
    [InlineData("Cell 3 donor: — → D-1", false)]
    [InlineData("Cell AC Rh phenotype: R1R1 → R2R2", false)]
    [InlineData("Cell 3 special types: — → Yt(a-)", true)]
    [InlineData("Cell 3 K: - → +", true)]
    [InlineData("Cell 3 E: NT → +", true)]
    public void ChangesAnalysisInput_SeparatesLabelsFromTyping(string change, bool expected) =>
        Assert.Equal(expected, DatabaseService.ChangesAnalysisInput(change));
}
