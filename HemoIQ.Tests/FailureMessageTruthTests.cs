using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// A failure message says what was kept. A failed Save writes nothing (one transaction); when Save &amp; Analyze's
/// analysis fails, the grades were already saved, and the message must not read as if they were lost.
/// </summary>
public class FailureMessageTruthTests
{
    [Fact]
    public void SaveFailure_SaysNothingWasWritten()
    {
        var text = ReactionsViewModel.SaveFailedText("disk full.");
        Assert.StartsWith("Not saved: disk full.", text);
        Assert.Contains("Nothing was written; the grades are still on screen.", text);
    }

    [Fact]
    public void AnalysisFailureAfterSave_SaysTheGradesWereSaved()
    {
        var text = ReactionsViewModel.AnalyzeAfterSaveFailedText("rule error.");
        Assert.StartsWith("Grades saved, but analysis did not run: rule error.", text);
        Assert.DoesNotContain("failed", text);
    }

    [Fact]
    public void AFailedSave_ReallyWritesNothing()
    {
        // The claim in SaveFailedText: a refused save leaves every reaction as it was.
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("F", "L-F", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("FM-1", "serum", null);
        iso.Db.LinkSpecimenPanel("FM-1", panel);
        iso.Db.SaveReaction("FM-1", panel, "1", "0", "0", "2+", "NT");
        var run = iso.Db.GetPanelRuns("FM-1", panel).Single();
        iso.Db.SetSpecimenFinalCall("FM-1", "anti-K", null, "TE");

        Assert.ThrowsAny<Exception>(() => iso.Db.SaveReactions(run.RunId, new List<HemoIQ.Models.ReactionInput>
        {
            new("1", "0", "0", "0", "2+", null),
            new("2", "0", "0", "3+", "NT", null),
        }));
        var stored = iso.Db.GetReactions(run.RunId);
        Assert.Single(stored);
        Assert.Equal("2+", stored[0].AHG);
    }
}
