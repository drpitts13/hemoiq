using HemoIQ.Services;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Ctrl+S and Ctrl+N run their command only when its button would be enabled. RelayCommand.Execute does not check
/// CanExecute, so the shortcuts must, or a view-only session opens Add dialogs and saves it cannot complete.
/// </summary>
public class ShortcutCommandTests
{
    private static readonly SessionInfo ViewOnly = new("Signed in: Reader (RO) (Read-only) — view only", "", CanRecord: false, ViewOnly: true);
    private static readonly SessionInfo Recording = new("Signed in: Tech (TE) (Technologist)", "", CanRecord: true, ViewOnly: false);

    [Fact]
    public void EnabledCommand_Runs()
    {
        var ran = false;
        Assert.Null(ShortcutCommand.Run(new RelayCommand(() => ran = true, () => true), "New specimen", Recording));
        Assert.True(ran);
    }

    [Fact]
    public void DisabledCommand_InAViewOnlySession_DoesNotRun_AndSaysWhy()
    {
        var ran = false;
        var status = ShortcutCommand.Run(new RelayCommand(() => ran = true, () => false), "New specimen", ViewOnly);
        Assert.False(ran);
        Assert.StartsWith("New specimen is not available: Signed in: Reader (RO) (Read-only) — view only.", status);
        Assert.Contains("Commands that change laboratory data are disabled", status);
    }

    [Fact]
    public void DisabledCommand_ForAnotherReason_DoesNotRun()
    {
        var ran = false;
        Assert.Equal("Save is not available right now.",
            ShortcutCommand.Run(new RelayCommand(() => ran = true, () => false), "Save", Recording));
        Assert.False(ran);
    }

    [Fact]
    public void MainWindowShortcuts_GoThroughTheCheck()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "MainWindow.xaml.cs"));
        foreach (var direct in new[] { "AddCommand.Execute(", "SaveCommand.Execute(", "PanelsVM.SaveAllCells()" })
            Assert.DoesNotContain(direct, src);
        Assert.Contains("RunShortcut(ViewModel.SpecimensVM.AddCommand", src);
        Assert.Contains("RunShortcut(ViewModel.ReactionsVM.SaveCommand", src);
    }
}
