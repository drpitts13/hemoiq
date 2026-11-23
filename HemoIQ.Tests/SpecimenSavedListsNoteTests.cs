using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// When the Specimens tab cannot read the current analysis it falls back to the lists saved at the last Run Analysis.
/// That must be said on the panes, not only on the transient status line, and the ruled-out pane must not claim that
/// "no specificity meets the current rule-out criteria" for an analysis that did not run.
/// </summary>
public class SpecimenSavedListsNoteTests
{
    private static string Source(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", relative));
    }

    [Fact]
    public void TheNote_SaysTheListsAreSavedAndMayBeOutOfDate()
    {
        var note = SpecimensViewModel.SavedListsNote("Panel #3 has no cells.");
        Assert.Contains("current analysis could not be read (Panel #3 has no cells)", note);
        Assert.Contains("saved at the last Run Analysis and may be out of date", note);
        Assert.Contains("Run Analysis", note);
    }

    [Fact]
    public void AFailure_ShowsTheNote_AndDropsTheCriteriaClaim()
    {
        var src = Source(Path.Combine("ViewModels", "SpecimensViewModel.cs"));
        var body = src[src.IndexOf("private bool LoadAnalysisSummary(")..];
        body = body[..body.IndexOf("public static string SavedListsNote(")];
        Assert.Contains("AnalysisReadProblem = failure != null ? SavedListsNote(failure)", body);
        Assert.Contains("RuleoutNote = failure != null ? \"\"", body);
        Assert.Contains("AppLog.Error(", body);
    }

    [Fact]
    public void TheView_ShowsTheNoteAboveSuspectedAntibodies()
    {
        var xaml = Source(Path.Combine("Views", "SpecimensView.xaml"));
        var note = xaml.IndexOf("{Binding AnalysisReadProblem}");
        Assert.True(note > 0);
        Assert.True(note < xaml.IndexOf("Header=\"Suspected Antibodies\""));
        Assert.Contains("Binding=\"{Binding HasAnalysisReadProblem}\" Value=\"False\"", xaml);
    }
}
