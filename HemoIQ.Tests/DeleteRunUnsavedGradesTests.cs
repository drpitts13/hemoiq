using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Deleting a run that holds unsaved grades discards them with the run: the question says so, and afterwards the tab
/// does not ask again to discard grades for a run that no longer exists, nor keep a recovery draft for it.
/// </summary>
public class DeleteRunUnsavedGradesTests
{
    [Fact]
    public void TheQuestion_MentionsUnsavedGrades_OnlyWhenThereAreSome()
    {
        Assert.Equal("Delete run 'Ficin panel' and all its reactions?",
            ReactionsViewModel.DeleteRunQuestion("Ficin panel", unsavedGrades: false));
        var withUnsaved = ReactionsViewModel.DeleteRunQuestion("Ficin panel", unsavedGrades: true);
        Assert.StartsWith("Delete run 'Ficin panel' and all its reactions?", withUnsaved);
        Assert.Contains("The unsaved grades on this run are discarded too.", withUnsaved);
    }

    [Fact]
    public void AfterTheDelete_TheGradesAreNoLongerUnsaved_AndTheDraftIsRemoved()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var body = src[src.IndexOf("private void DeleteRun()")..];
        body = body[..body.IndexOf("public static string DeleteRunQuestion(")];
        var deleted = body.IndexOf("_db.DeletePanelRun(");
        var clean = body.IndexOf("MarkGradesClean();");
        var draft = body.IndexOf("Drafts.Delete();");
        var refresh = body.IndexOf("RefreshRuns();");
        Assert.True(deleted > 0 && clean > deleted && draft > deleted, "cleaned only after a successful delete");
        Assert.True(clean < refresh && draft < refresh, "cleaned before the next run is selected");
        Assert.Contains("DeleteRunQuestion(SelectedRun.DisplayLabel, _gradesDirty)", body);
    }
}
