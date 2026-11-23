using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// When an analyzed specimen's results cannot be rebuilt on the Analysis tab, the empty lists must not pass for a
/// result: the tab says the analysis could not be shown and how to retry.
/// </summary>
public class AnalysisReloadFailureTests
{
    [Fact]
    public void TheText_SaysTheEmptyListsAreNotAResult()
    {
        var text = AnalysisViewModel.ReloadFailedText("RF-1", "Panel #4 has no cells.");
        Assert.Contains("analysis of RF-1 could not be shown: Panel #4 has no cells.", text);
        Assert.Contains("The empty lists are not a result", text);
        Assert.Contains("Run Analysis", text);
        Assert.DoesNotContain("..", text);
    }

    [Fact]
    public void AReasonWithoutAFullStop_StillReadsAsASentence()
    {
        Assert.Contains("could not be shown: disk error. The empty",
            AnalysisViewModel.ReloadFailedText("RF-2", " disk error "));
    }

    [Fact]
    public void AutoLoad_ShowsTheFailure_RatherThanSwallowingIt()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "AnalysisViewModel.cs"));
        var body = src[src.IndexOf("private bool AutoLoadAnalysis()")..];
        body = body[..body.IndexOf("public static string ReloadFailedText(")];
        Assert.DoesNotContain("catch\n", body.Replace("\r\n", "\n"));
        Assert.Contains("SummaryText = ReloadFailedText(", body);
        Assert.Contains("AppLog.Error(", body);
    }
}
