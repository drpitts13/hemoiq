using System.Text.RegularExpressions;
using System.Windows;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Confirmations that delete data must default to No, so pressing Enter (or Enter twice, after a dialog's own
/// default button) never deletes records. The Purge button must not be the dialog's Enter button.
/// </summary>
public class DestructiveConfirmTests
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "HemoIQ");
    }

    [Fact]
    public void DefaultAnswer_IsNo() =>
        Assert.Equal(MessageBoxResult.No, DestructiveConfirm.DefaultAnswer);

    [Fact]
    public void NoYesNoMessageBox_ThatDeletes_BypassesTheHelper()
    {
        // A Yes/No MessageBox whose message starts by deleting, removing, detaching or purging something.
        var deleting = new Regex(
            @"MessageBox\.Show\(\s*(?:this,\s*)?\$?""(?:Permanently delete|Delete|Remove|Detach|Purge|Clear|Discard)[^;]*?MessageBoxButton\.YesNo",
            RegexOptions.Singleline);
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Root(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            var src = File.ReadAllText(file);
            foreach (Match m in deleting.Matches(src))
                offenders.Add($"{Path.GetFileName(file)}: {m.Value[..Math.Min(80, m.Value.Length)]}");
        }
        Assert.True(offenders.Count == 0, "Use DestructiveConfirm.Ask for: " + string.Join(" | ", offenders));
    }

    [Theory]
    [InlineData("ViewModels/PanelsViewModel.cs", "private void DeletePanel()")]
    [InlineData("ViewModels/RulesViewModel.cs", "private void DeleteRule()")]
    [InlineData("ViewModels/SpecimensViewModel.cs", "Delete specimen {acc}?")]
    [InlineData("ViewModels/ReactionsViewModel.cs", "private bool TryGetDeletionReason(")]
    [InlineData("Views/Dialogs/PurgeDatabaseDialog.xaml.cs", "Permanently delete {count} specimen(s)")]
    public void EachDeletingAction_AsksThroughTheHelper(string file, string anchor)
    {
        var src = File.ReadAllText(Path.Combine(Root(), file));
        var at = src.IndexOf(anchor, StringComparison.Ordinal);
        Assert.True(at >= 0, anchor);
        var window = src.Substring(Math.Max(0, at - 200), Math.Min(1500, src.Length - Math.Max(0, at - 200)));
        Assert.Contains("DestructiveConfirm.Ask(", window);
    }

    [Fact]
    public void PurgeButton_IsNotTheEnterButton()
    {
        var xaml = File.ReadAllText(Path.Combine(Root(), "Views", "Dialogs", "PurgeDatabaseDialog.xaml"));
        var purge = Regex.Match(xaml, @"<Button Content=""Purge""[^>]*/>").Value;
        Assert.NotEmpty(purge);
        Assert.DoesNotContain("IsDefault=\"True\"", purge);
    }
}
