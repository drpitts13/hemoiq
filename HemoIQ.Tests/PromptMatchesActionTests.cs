using HemoIQ.Models;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// A prompt's Yes must do what the prompt says. The ACS-with-exception offer only fills in Final ID and the comment,
/// so it must not read as resulting the call, and it says when it would replace what is already in Final ID. The
/// prompt that discards typed grades defaults to No.
/// </summary>
public class PromptMatchesActionTests
{
    private static AcsEvaluation Acs() => new()
    {
        IsEligibleWithException = true,
        Exceptions = { new AcsExceptionAntibody { Antibody = "anti-K", CombinedScore = 0.97 } },
        SuggestedCombinedResult = "ACS; anti-K",
    };

    [Fact]
    public void AcsOffer_SaysItFillsInFinalId_AndThatNothingIsConfirmed()
    {
        var prompt = AnalysisViewModel.AcsExceptionPrompt(Acs(), "");
        Assert.Contains("Fill in Final ID with ACS and anti-K", prompt);
        Assert.Contains("Nothing is confirmed until you review them and choose Confirm ID.", prompt);
        Assert.DoesNotContain("Result ACS", prompt);
        Assert.DoesNotContain("replaces", prompt);
    }

    [Fact]
    public void AcsOffer_SaysWhenItWouldReplaceWhatIsTyped()
    {
        var prompt = AnalysisViewModel.AcsExceptionPrompt(Acs(), " anti-E ");
        Assert.Contains("This replaces what is in Final ID now (anti-E).", prompt);
    }

    [Fact]
    public void DiscardingUnsavedGrades_AsksThroughTheDestructiveHelper()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        Assert.Contains("DestructiveConfirm.Ask(\"Discard unsaved reaction grades?\"", src);
    }
}
