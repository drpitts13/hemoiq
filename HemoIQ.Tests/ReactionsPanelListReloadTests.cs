using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// A panel attached or detached on the Specimens tab must show on the Reactions tab for the specimen already selected
/// there, without dropping unsaved grades on a panel that was detached.
/// </summary>
public class ReactionsPanelListReloadTests
{
    private static Panel P(int id, string name) => new() { PanelId = id, Name = name };

    [Fact]
    public void AnAttachedPanel_IsListed_AndTheSelectionIsKept()
    {
        var a = P(1, "A");
        var (panels, select) = ReactionsViewModel.ReconcilePanels(new[] { a, P(2, "B") }, a, gradesUnsaved: false);
        Assert.Equal(new[] { 1, 2 }, panels.Select(p => p.PanelId));
        Assert.Equal(1, select);
    }

    [Fact]
    public void TheFirstAttachedPanel_IsSelected_WhenNoneWasListed()
    {
        var (panels, select) = ReactionsViewModel.ReconcilePanels(new[] { P(5, "New") }, null, gradesUnsaved: false);
        Assert.Single(panels);
        Assert.Equal(5, select);
    }

    [Fact]
    public void ADetachedPanelWithoutUnsavedGrades_IsDropped()
    {
        var (panels, select) = ReactionsViewModel.ReconcilePanels(new[] { P(2, "B") }, P(1, "A"), gradesUnsaved: false);
        Assert.Equal(new[] { 2 }, panels.Select(p => p.PanelId));
        Assert.Equal(2, select);
    }

    [Fact]
    public void ADetachedPanelWithUnsavedGrades_StaysSelected()
    {
        var (panels, select) = ReactionsViewModel.ReconcilePanels(new[] { P(2, "B") }, P(1, "A"), gradesUnsaved: true);
        Assert.Equal(new[] { 1, 2 }, panels.Select(p => p.PanelId));
        Assert.Equal(1, select);
    }

    [Fact]
    public void DetachingTheOnlyPanel_LeavesNothingSelected()
    {
        var (panels, select) = ReactionsViewModel.ReconcilePanels(Array.Empty<Panel>(), P(1, "A"), gradesUnsaved: false);
        Assert.Empty(panels);
        Assert.Null(select);
    }

    [Fact]
    public void RefreshSpecimens_ReloadsPanelsForTheSameSpecimen()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var body = src[src.IndexOf("public void RefreshSpecimens()")..];
        body = body[..body.IndexOf("public static (List<Panel>")];
        Assert.Contains("ReloadPanelsInPlace()", body);
    }
}
