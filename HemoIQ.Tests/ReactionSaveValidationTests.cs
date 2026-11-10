using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class ReactionSaveValidationTests
{
    private static int Setup(IsolatedDatabase iso, string acc, int cells = 2)
    {
        iso.Db.AddSpecimen(acc, "serum", null);
        var panel = iso.Db.AddPanel("RV", "RV-" + acc, "V", cells, null, false);
        iso.Db.LinkSpecimenPanel(acc, panel);
        return panel;
    }

    [Fact]
    public void RecognizedSynonyms_AreStoredNormalized()
    {
        using var iso = new IsolatedDatabase();
        var panel = Setup(iso, "RV-1");
        iso.Db.SaveReaction("RV-1", panel, "1", "nt", "W", "2", "mixed field");
        var r = iso.Db.GetReactions("RV-1", panel).Single();
        Assert.Equal(("NT", "w+", "2+", "MF"), (r.IS, r.C37, r.AHG, r.CC));
    }

    [Fact]
    public void ACellThePanelDoesNotHave_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        var panel = Setup(iso, "RV-2");
        var ex = Assert.Throws<ArgumentException>(() => iso.Db.SaveReaction("RV-2", panel, "9", "0", "0", "0", "2+"));
        Assert.Equal("Cell 9 is not on RV (lot RV-RV-2); grades can only be recorded for the panel's own cells.", ex.Message);
        Assert.Empty(iso.Db.GetReactions("RV-2", panel));
    }

    [Fact]
    public void Autocontrol_IsAcceptedWithoutAPanelCell()
    {
        using var iso = new IsolatedDatabase();
        var panel = Setup(iso, "RV-3");
        iso.Db.SaveReaction("RV-3", panel, "AC", "0", "0", "0", "2+");
        Assert.Single(iso.Db.GetReactions("RV-3", panel), r => r.CellNumber == "AC");
    }

    [Fact]
    public void ABadRow_RollsBackTheWholeGridSave()
    {
        using var iso = new IsolatedDatabase();
        var panel = Setup(iso, "RV-4");
        var run = iso.Db.GetOrCreateDefaultRun("RV-4", panel);
        var rows = new List<ReactionInput>
        {
            new("1", "0", "0", "2+", "NT", null),
            new("2", "0", "0", "huh", "NT", null),
        };
        Assert.Throws<ArgumentException>(() => iso.Db.SaveReactions(run, rows));
        Assert.Empty(iso.Db.GetReactions(run));
    }

    [Fact]
    public void ExtraPhaseGrades_AreValidatedToo()
    {
        using var iso = new IsolatedDatabase();
        var panel = Setup(iso, "RV-5");
        var run = iso.Db.GetOrCreateDefaultRun("RV-5", panel);
        Assert.Contains("Cell 1 PEG: \"strong\"",
            Assert.Throws<ArgumentException>(() => iso.Db.SaveReaction(run, "1", "0", "0", "0", "2+",
                new Dictionary<string, string> { ["PEG"] = "strong" })).Message);
        iso.Db.SaveReaction(run, "1", "0", "0", "0", "2+", new Dictionary<string, string> { ["PEG"] = "1" });
        Assert.Equal("1+", iso.Db.GetReactions(run).Single().ExtraPhases["PEG"]);
    }
}
