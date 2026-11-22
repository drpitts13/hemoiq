using System.Text.RegularExpressions;

namespace HemoIQ.Tests;

/// <summary>
/// When a dialog refuses a value on Save, the message names the field and the cursor goes to it, so the user fixes
/// the right box instead of hunting for it.
/// </summary>
public class ValidationFocusTests
{
    [Theory]
    [InlineData("SpecimenDialog")]
    [InlineData("PanelDialog")]
    [InlineData("RuleDialog")]
    [InlineData("SettingsDialog")]
    [InlineData("PatientDialog")]
    public void EveryValidationMessage_OnSave_FocusesAField(string dialog)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Views", "Dialogs", dialog + ".xaml.cs"));

        // Each "Validation" message box, up to the return that ends that refusal.
        var refusals = Regex.Matches(src, @"MessageBox\.Show\([^;]*?""Validation""[^;]*?;(?<after>.*?)return;", RegexOptions.Singleline);
        var unfocused = refusals.Cast<Match>()
            .Where(m => !m.Groups["after"].Value.Contains(".Focus()"))
            .Select(m => m.Value[..Math.Min(70, m.Value.Length)])
            .ToList();
        Assert.True(unfocused.Count == 0, $"{dialog}: validation without focus: " + string.Join(" | ", unfocused));
    }
}
