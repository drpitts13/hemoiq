using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A panel with no saved grades can be detached while grades typed on the Reactions tab are still unsaved. Saving them
/// afterwards must be refused rather than record results on a lot the investigation no longer lists.
/// </summary>
public class DetachedPanelSaveTests
{
    private static (IsolatedDatabase Iso, int Panel, int Run) RunWithoutGrades()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Det", "L-DET", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("DP-1", "serum", null);
        iso.Db.LinkSpecimenPanel("DP-1", panel);
        iso.Db.SaveReaction("DP-1", panel, "1", "NT", "NT", "NT", "NT"); // creates the run, nothing graded
        var run = iso.Db.GetPanelRuns("DP-1", panel).Single().RunId;
        return (iso, panel, run);
    }

    [Fact]
    public void SavingOnADetachedPanel_IsRefused_AndNothingIsWritten()
    {
        var (iso, panel, run) = RunWithoutGrades();
        using (iso)
        {
            iso.Db.UnlinkSpecimenPanel("DP-1", panel); // allowed: no graded cells
            var ex = Assert.Throws<InvalidOperationException>(() =>
                iso.Db.SaveReaction(run, "1", "0", "0", "2+", "NT"));
            Assert.Contains("is no longer attached to DP-1", ex.Message);
            Assert.Contains("Attach the panel again", ex.Message);
            Assert.DoesNotContain(iso.Db.GetReactions(run), r => r.AHG == "2+");
        }
    }

    [Fact]
    public void AttachingAgain_LetsTheGradesBeSaved()
    {
        var (iso, panel, run) = RunWithoutGrades();
        using (iso)
        {
            iso.Db.UnlinkSpecimenPanel("DP-1", panel);
            iso.Db.LinkSpecimenPanel("DP-1", panel);
            iso.Db.SaveReaction(run, "1", "0", "0", "2+", "NT");
            Assert.Contains(iso.Db.GetReactions(run), r => r.CellNumber == "1" && r.AHG == "2+");
        }
    }
}
