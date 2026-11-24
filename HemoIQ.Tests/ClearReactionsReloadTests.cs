using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Clearing a run's reactions keeps the run and its cells: the grid reloads blank so grades can be entered again, the
/// question says when unsaved grades are discarded, and no recovery draft is left for grades that were cleared.
/// </summary>
public class ClearReactionsReloadTests
{
    [Fact]
    public void TheQuestion_MentionsUnsavedGrades_OnlyWhenThereAreSome()
    {
        Assert.Equal("Clear all reactions for this run?", ReactionsViewModel.ClearReactionsQuestion(false));
        Assert.Contains("The unsaved grades on screen are discarded too.", ReactionsViewModel.ClearReactionsQuestion(true));
    }

    [Fact]
    public void AClearedRun_StillHasItsCells_ForReEntry()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("CR", "L-CR", "V", 3, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("CR-1", "serum", null);
        iso.Db.LinkSpecimenPanel("CR-1", panel);
        iso.Db.SaveReaction("CR-1", panel, "1", "0", "0", "2+", "NT");
        var run = iso.Db.GetPanelRuns("CR-1", panel).Single().RunId;
        iso.Db.DeleteReactions(run, null);
        Assert.DoesNotContain(iso.Db.GetReactions(run), r => r.AHG == "2+");
        Assert.NotNull(iso.Db.GetPanelRun(run));
        Assert.Equal(3, iso.Db.GetPanelCells(panel).Count);
    }

    [Fact]
    public void Clear_ReloadsTheRun_AndRemovesTheDraft()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var body = src[src.IndexOf("private void ClearReactions()")..];
        body = body[..body.IndexOf("public static string ClearReactionsQuestion(")];
        var deleted = body.IndexOf("_db.DeleteReactions(");
        Assert.True(body.IndexOf("Drafts.Delete();") > deleted);
        Assert.True(body.IndexOf("LoadReactions();") > deleted);
        Assert.DoesNotContain("Rows.Clear();", body);
        Assert.Contains("ClearReactionsQuestion(_gradesDirty)", body);
    }
}
