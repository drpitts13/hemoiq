using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Attach Panel and Detach are disabled on a confirmed or cancelled investigation, with the lock (and who can lift
/// it) as their tooltip, rather than refusing after a panel has been chosen.
/// </summary>
public class PanelLinkLockTests
{
    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "HemoIQ" }.Concat(parts).ToArray()));
    }

    [Theory]
    [InlineData("AttachPanelCommand")]
    [InlineData("DetachPanelCommand")]
    public void LinkCommands_AreDisabledOnALockedInvestigation(string command)
    {
        var vm = Source("ViewModels", "SpecimensViewModel.cs");
        var at = vm.IndexOf($"{command} = new RelayCommand(", StringComparison.Ordinal);
        Assert.True(at >= 0, command);
        var args = vm.Substring(at, vm.IndexOf(");", at, StringComparison.Ordinal) - at);
        Assert.Contains("SessionAccess.CanRecord", args);
        Assert.Contains("!(SelectedSpecimen.HasFinalCall || SelectedSpecimen.IsCancelled)", args);
    }

    [Fact]
    public void BothButtons_ShowTheLockAsTheirTooltip()
    {
        var xaml = Source("Views", "SpecimensView.xaml");
        foreach (var command in new[] { "AttachPanelCommand", "DetachPanelCommand" })
        {
            var at = xaml.IndexOf($"Command=\"{{Binding {command}}}\"", StringComparison.Ordinal);
            Assert.True(at >= 0, command);
            Assert.Contains("ToolTip=\"{Binding PanelLinkLock}\"", xaml.Substring(at, 200));
        }
    }

    [Fact]
    public void TheTooltip_IsTheMessageTheLinkWouldBeRefusedWith()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Lock", "L-LK", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("PL-1", "serum", null);
        iso.Db.SetSpecimenFinalCall("PL-1", "anti-K", null, "TE");
        var refused = Assert.Throws<HemoIQ.Models.RecordLockedException>(() => iso.Db.LinkSpecimenPanel("PL-1", panel));
        Assert.Equal(refused.Message, ReactionsViewModel.LockMessageFor(iso.Db, "PL-1"));
    }
}
