using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// A confirmed or cancelled investigation is shown read-only on the Reactions tab, with the lock and who can lift
/// it, and its change buttons are disabled: grades are never typed only to be refused at Save.
/// </summary>
public class ReactionsLockStateTests
{
    [Fact]
    public void OpenInvestigation_IsNotLocked()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RL-OPEN", "serum", null);
        Assert.Null(ReactionsViewModel.LockMessageFor(iso.Db, "RL-OPEN"));
        Assert.Null(ReactionsViewModel.LockMessageFor(iso.Db, null));
    }

    [Fact]
    public void ConfirmedInvestigation_GivesTheLockMessageASaveWouldBeRefusedWith()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RL-ID", "serum", null);
        iso.Db.SetSpecimenFinalCall("RL-ID", "anti-K", null, "TE");
        var message = ReactionsViewModel.LockMessageFor(iso.Db, "RL-ID");
        Assert.Contains("has a confirmed identification and is locked", message);
        var refused = Assert.Throws<HemoIQ.Models.RecordLockedException>(() => iso.Db.EnsureSpecimenUnlocked("RL-ID"));
        Assert.Equal(refused.Message, message);
    }

    [Fact]
    public void CancelledInvestigation_SaysCancelled()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RL-CX", "serum", null);
        iso.Db.CancelInvestigation("RL-CX", "duplicate");
        Assert.Contains("is cancelled and locked", ReactionsViewModel.LockMessageFor(iso.Db, "RL-CX"));
    }

    [Fact]
    public void ChangeCommands_AreDisabledWhileLocked_AndTheTabReReadsTheLock()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var root = Path.Combine(dir!.FullName, "HemoIQ");
        var vm = File.ReadAllText(Path.Combine(root, "ViewModels", "ReactionsViewModel.cs"));
        Assert.Contains("public bool GradesReadOnly => !SessionAccess.CanRecord || LockMessage != null;", vm);
        foreach (var command in new[] { "SaveCommand", "SaveAnalyzeCommand", "ClearCommand", "FillNegativesCommand", "AddRunCommand", "DeleteRunCommand" })
        {
            var at = vm.IndexOf($"{command} = new RelayCommand(", StringComparison.Ordinal);
            Assert.True(at >= 0, command);
            Assert.Contains("SessionAccess.CanRecord && !IsLocked", vm.Substring(at, 200));
        }
        Assert.Contains("ReactionsVM.RefreshLockState()", File.ReadAllText(Path.Combine(root, "MainWindow.xaml.cs")));
        Assert.Contains("{Binding LockMessage}", File.ReadAllText(Path.Combine(root, "Views", "ReactionsView.xaml")));
    }
}
