using System.Text.RegularExpressions;
using System.Windows;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// A prompt that lets the user go ahead past a safety warning must default to No, so pressing Enter keeps the safe
/// choice and every recorded override is deliberate (as the stale-analysis and open-testing prompts already do).
/// </summary>
public class OverrideConfirmTests
{
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "HemoIQ");
    }

    private static IEnumerable<(string File, string Src)> Sources() =>
        Directory.EnumerateFiles(Root(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

    [Fact]
    public void DefaultAnswer_IsNo() => Assert.Equal(MessageBoxResult.No, OverrideConfirm.DefaultAnswer);

    [Fact]
    public void NoYesNoWarning_DefaultsToYes()
    {
        // A Yes/No warning with no default answer falls back to Yes under the keyboard.
        var offenders = Sources()
            .Where(s => Regex.IsMatch(s.Src, @"MessageBoxButton\.YesNo,\s*MessageBoxImage\.Warning\s*\)"))
            .Select(s => s.File).ToList();
        Assert.True(offenders.Count == 0, "Give a default of No (or use OverrideConfirm.Ask) in: " + string.Join(", ", offenders));
    }

    [Fact]
    public void NoGoAheadAnywayPrompt_BypassesTheHelpers()
    {
        var anyway = new Regex(@"MessageBox\.Show\((?:(?!MessageBox\.Show)[^;])*?anyway\?(?:(?!MessageBox\.Show)[^;])*?MessageBoxButton\.YesNo\b(?!Cancel)",
            RegexOptions.Singleline);
        var offenders = new List<string>();
        foreach (var (file, src) in Sources())
            foreach (Match m in anyway.Matches(src))
            {
                // The stale-analysis and open-testing prompts give their own default of No.
                if (Regex.IsMatch(m.Value + src.Substring(m.Index + m.Length, Math.Min(80, src.Length - m.Index - m.Length)),
                        @"MessageBoxButton\.YesNo,\s*MessageBoxImage\.\w+,\s*MessageBoxResult\.No")) continue;
                offenders.Add($"{file}: {m.Value[..Math.Min(80, m.Value.Length)]}");
            }
        Assert.True(offenders.Count == 0, "Use OverrideConfirm.Ask for: " + string.Join(" | ", offenders));
    }

    [Theory]
    [InlineData("ViewModels/PanelsViewModel.cs", "Save anyway?\",")]
    [InlineData("ViewModels/PanelsViewModel.cs", "Activate it anyway? No keeps it inactive.")]
    [InlineData("ViewModels/ReactionsViewModel.cs", "\"Expired specimen\"")]
    [InlineData("ViewModels/ReactionsViewModel.cs", "\"Expired panel\"")]
    [InlineData("ViewModels/SpecimensViewModel.cs", "\"Encounter on another patient\"")]
    [InlineData("ViewModels/SpecimensViewModel.cs", "\"Pregnancy and patient sex\"")]
    [InlineData("ViewModels/AnalysisViewModel.cs", "Confirm this identification anyway?")]
    [InlineData("Views/Dialogs/DownloadFromVendorDialog.xaml.cs", "Replace it?")]
    public void EachOverridePrompt_AsksThroughTheHelper(string file, string anchor)
    {
        var src = File.ReadAllText(Path.Combine(Root(), file));
        var at = src.IndexOf(anchor, StringComparison.Ordinal);
        Assert.True(at >= 0, anchor);
        var start = Math.Max(0, at - 500);
        Assert.Contains("OverrideConfirm.Ask(", src.Substring(start, at - start + anchor.Length));
    }
}
