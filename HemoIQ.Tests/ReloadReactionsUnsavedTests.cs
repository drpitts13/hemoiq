namespace HemoIQ.Tests;

/// <summary>
/// The Load Reactions button replaced unsaved grades with the saved ones without asking. It now goes through the same
/// discard question as changing run, panel or specimen, so the grades are kept unless the user chooses to discard them.
/// </summary>
public class ReloadReactionsUnsavedTests
{
    private static string Source()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
    }

    [Fact]
    public void TheButton_AsksBeforeReloading()
    {
        var src = Source();
        Assert.Contains("LoadCommand = new RelayCommand(ReloadReactions,", src);
        var body = src[src.IndexOf("private void ReloadReactions()")..];
        body = body[..body.IndexOf("private void LoadReactions()")];
        Assert.True(body.IndexOf("if (!ConfirmDiscardUnsaved()) return;") < body.IndexOf("LoadReactions();"));
    }

    [Fact]
    public void TheQuestion_DefaultsToKeepingTheGrades()
    {
        var src = Source();
        var body = src[src.IndexOf("private bool ConfirmDiscardUnsaved()")..];
        body = body[..body.IndexOf("private void SetSaveStatus(")];
        Assert.Contains("if (!_gradesDirty) return true;", body);
        Assert.Contains("DestructiveConfirm.Ask(", body); // No under the keyboard
    }

    [Fact]
    public void NoButtonCallsTheRawReload()
    {
        Assert.DoesNotContain("new RelayCommand(LoadReactions", Source());
    }
}
