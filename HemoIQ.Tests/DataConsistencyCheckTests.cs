using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class DataConsistencyCheckTests
{
    private static void Raw(IsolatedDatabase iso, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={iso.DbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Specimen DC-1 on a 3-cell panel with grades on cells 1 and 2.</summary>
    private static int Tested(IsolatedDatabase iso, string acc = "DC-1")
    {
        var panel = iso.Db.AddPanel("DC Panel", "DC-L", "V", 3, null, false);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction(acc, panel, "2", "0", "0", "0", "2+");
        return panel;
    }

    [Fact]
    public void CleanData_HasNoFindings_AndNoWorklistItems()
    {
        using var iso = new IsolatedDatabase();
        Tested(iso);
        Assert.Empty(DataConsistencyCheck.Find(iso.Db.GetReactionRecords()));
        Assert.DoesNotContain(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.DataInconsistency);
    }

    [Fact]
    public void DetachingAPanelWithGrades_IsRefused_AndTheLinkStays()
    {
        using var iso = new IsolatedDatabase();
        var panel = Tested(iso);
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.UnlinkSpecimenPanel("DC-1", panel));
        Assert.Equal("DC Panel (lot DC-L) has 2 graded cell(s) for DC-1. Analysis uses them, so the panel stays attached. " +
                     "Clear its reactions on the Reactions tab (with a reason) first.", ex.Message);
        Assert.Single(iso.Db.GetSpecimenPanels("DC-1"));

        iso.Db.DeleteReactions("DC-1", panel, "wrong panel attached");
        iso.Db.UnlinkSpecimenPanel("DC-1", panel);
        Assert.Empty(iso.Db.GetSpecimenPanels("DC-1"));
    }

    [Fact]
    public void LegacyDetachedPanelWithGrades_IsFound_AndListedOnTheWorklist()
    {
        using var iso = new IsolatedDatabase();
        var panel = Tested(iso);
        Raw(iso, "DELETE FROM specimen_panels WHERE specimen_id = 'DC-1'");

        var f = Assert.Single(DataConsistencyCheck.Find(iso.Db.GetReactionRecords()));
        Assert.Equal("DC-1: reactions on detached panel", f.Title);
        Assert.Contains("DC Panel has 2 graded cell(s) for DC-1 but is no longer attached", f.Detail);
        Assert.Contains("Analysis still uses these reactions", f.Detail);
        Assert.Equal("Specimens", f.TargetTab);

        var item = Assert.Single(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.DataInconsistency);
        Assert.Equal("DC-1", item.AccessionNumber);
        Assert.Equal(panel, item.PanelId);
        Assert.Equal("Specimens", item.TargetTab);
        Assert.Equal("Review", LabSettings.WorklistCategory(item.Kind));
    }

    [Fact]
    public void GradesForMissingCells_AndUnrecognizedGrades_AreFound()
    {
        using var iso = new IsolatedDatabase();
        Tested(iso);
        Raw(iso, "DELETE FROM panel_cells WHERE cell_number = '2'");
        Raw(iso, "UPDATE reactions SET AHG = 'pos' WHERE cell_number = '1'");

        var findings = DataConsistencyCheck.Find(iso.Db.GetReactionRecords());
        Assert.Equal(2, findings.Count);
        var missing = Assert.Single(findings, f => f.Title == "DC-1: grades for cells the panel lacks");
        Assert.Contains("cell(s) 2 for DC-1", missing.Detail);
        Assert.Equal("Reactions", missing.TargetTab);
        var bad = Assert.Single(findings, f => f.Title == "DC-1: unrecognized grades");
        Assert.Contains("cell 1 AHG \"pos\"", bad.Detail);
    }

    [Fact]
    public void UngradedRows_OnDetachedPanels_AreNotReported_AndAutocontrolNeedsNoPanelCell()
    {
        var rows = new[]
        {
            new ReactionRecord("X-1", 1, "P", "1", new Dictionary<string, string?> { ["IS"] = "NT", ["AHG"] = null }, PanelLinked: false, CellOnPanel: true),
            new ReactionRecord("X-2", 2, "P", "AC", new Dictionary<string, string?> { ["AHG"] = "0" }, PanelLinked: true, CellOnPanel: true),
        };
        Assert.Empty(DataConsistencyCheck.Find(rows));
        Assert.True(DataConsistencyCheck.IsRecognized("w+"));
        Assert.True(DataConsistencyCheck.IsRecognized(""));
        Assert.False(DataConsistencyCheck.IsRecognized("W+"));
    }
}
