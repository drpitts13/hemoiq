using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// + Run with unsaved grades on screen used to create the run, ask to discard the grades when switching to it, and on
/// No reload the current run anyway, losing the grades the user chose to keep. It is now refused before anything is
/// created.
/// </summary>
public class AddRunUnsavedGradesTests
{
    [Fact]
    public void WithUnsavedGrades_AddRunIsRefused_AndSaysWhatToDo()
    {
        var why = ReactionsViewModel.AddRunRefusal(true, "Untreated");
        Assert.NotNull(why);
        Assert.Contains("The grades on Untreated are not saved", why);
        Assert.Contains("no run was added", why);
        Assert.Contains("Pre-fill with grades from the current run", why);
    }

    [Fact]
    public void WithNothingUnsaved_AddRunGoesAhead()
    {
        Assert.Null(ReactionsViewModel.AddRunRefusal(false, "Untreated"));
    }

    [Fact]
    public void TheCheck_RunsBeforeTheDialogAndBeforeTheRunIsCreated()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var body = src[src.IndexOf("private void AddRun()")..];
        var check = body.IndexOf("AddRunRefusal(_gradesDirty");
        Assert.True(check > 0);
        Assert.True(check < body.IndexOf("new Views.Dialogs.PanelRunDialog"));
        Assert.True(check < body.IndexOf("_db.AddPanelRun("));
        var label = File.ReadAllText(Path.Combine(dir.FullName, "HemoIQ", "Views", "Dialogs", "PanelRunDialog.xaml"));
        Assert.Contains("Pre-fill with grades from the current run", label); // the refusal names the real option
    }
}
