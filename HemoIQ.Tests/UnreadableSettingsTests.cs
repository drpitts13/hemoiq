using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// A Preferences file that exists but cannot be read must not leave the lab silently on default ID rules: start-up
/// says so, and the unreadable file is kept aside before a later save can overwrite it.
/// </summary>
[Collection("PersistentDatabase")]
public class UnreadableSettingsTests
{
    private static void InTempDir(Action<string, string> body)
    {
        var original = AppSettings.Current;
        var dir = Path.Combine(Path.GetTempPath(), "hemoiq-unreadable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { body(dir, Path.Combine(dir, "settings.json")); }
        finally
        {
            AppSettings.Current = original;
            SettingsService.Load(Path.Combine(dir, "missing.json")); // clears LastLoadProblem
            AppSettings.Current = original;
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Theory]
    [InlineData("{ \"SeparationCellCount\": 3, ")]          // cut short
    [InlineData("{ \"SeparationCellCount\": \"three\" }")]  // wrong type
    [InlineData("not json at all")]
    public void DamagedFile_FallsBackToDefaults_SaysSo_AndKeepsACopy(string content)
    {
        InTempDir((dir, path) =>
        {
            File.WriteAllText(path, content);
            SettingsService.Load(path);

            Assert.Equal(LabSettings.CreateDefault().SeparationCellCount, AppSettings.Current.SeparationCellCount);
            var problem = SettingsService.LastLoadProblem;
            Assert.NotNull(problem);
            Assert.Contains("could not be read", problem);
            Assert.Contains("default Preferences", problem);
            Assert.Contains("Review Preferences", problem);

            var copy = Assert.Single(Directory.GetFiles(dir, "settings.unreadable-*.json"));
            Assert.Equal(content, File.ReadAllText(copy));
            Assert.Contains(copy, problem);

            SettingsService.Save(path); // a later save replaces the file, but the copy remains
            Assert.Equal(content, File.ReadAllText(copy));
        });
    }

    [Fact]
    public void EmptyJsonNull_IsReportedToo()
    {
        InTempDir((_, path) =>
        {
            File.WriteAllText(path, "null");
            SettingsService.Load(path);
            Assert.Contains("the file is empty", SettingsService.LastLoadProblem);
        });
    }

    [Fact]
    public void MissingFile_IsAFirstRun_NotAProblem()
    {
        InTempDir((dir, path) =>
        {
            SettingsService.Load(path);
            Assert.Null(SettingsService.LastLoadProblem);
            Assert.Empty(Directory.GetFiles(dir, "settings.unreadable-*.json"));
        });
    }

    [Fact]
    public void GoodFile_LoadsWithoutAProblem()
    {
        InTempDir((_, path) =>
        {
            AppSettings.Current = new LabSettings { SeparationCellCount = 3 };
            SettingsService.Save(path);
            AppSettings.Current = LabSettings.CreateDefault();
            SettingsService.Load(path);
            Assert.Null(SettingsService.LastLoadProblem);
            Assert.Equal(3, AppSettings.Current.SeparationCellCount);
        });
    }

    [Fact]
    public void OutOfRangeAnalysisSetting_IsReplaced_Reported_AndTheFileKept()
    {
        InTempDir((dir, path) =>
        {
            const string content = "{ \"SeparationCellCount\": 9, \"IdentificationCellCount\": 3, \"LabName\": \"\" }";
            File.WriteAllText(path, content);
            SettingsService.Load(path);

            Assert.Equal(1, AppSettings.Current.SeparationCellCount);
            var problem = SettingsService.LastLoadProblem;
            Assert.NotNull(problem);
            Assert.Contains("SeparationCellCount: 9 → 1", problem);
            Assert.DoesNotContain("IdentificationCellCount", problem); // in range, unchanged
            Assert.DoesNotContain("LabName", problem);                 // not an analysis setting
            var copy = Assert.Single(Directory.GetFiles(dir, "settings.adjusted-*.json"));
            Assert.Equal(content, File.ReadAllText(copy));
        });
    }

    [Fact]
    public void InRangeFile_IsNotReportedAsAdjusted()
    {
        InTempDir((dir, path) =>
        {
            File.WriteAllText(path, "{ \"SeparationCellCount\": 2, \"ProbabilityThreshold\": 0.7 }");
            SettingsService.Load(path);
            Assert.Null(SettingsService.LastLoadProblem);
            Assert.Empty(Directory.GetFiles(dir, "settings.adjusted-*.json"));
        });
    }

    [Fact]
    public void DescribeClinicalChanges_ListsOnlyTheChangedSettings()
    {
        var before = new LabSettings { ProbabilityThreshold = 0.1, AcsRuleoutCount = 3 };
        var beforeJson = before.ClinicalSettingsJson();
        before.Clamp();
        var changes = LabSettings.DescribeClinicalChanges(beforeJson, before.ClinicalSettingsJson());
        Assert.Equal(new[] { "ProbabilityThreshold: 0.1 → 0.3" }, changes);
    }

    [Fact]
    public void CopyThatCannotBeKept_TellsTheUserNotToSaveYet() =>
        Assert.Contains("do not save Preferences until it has been copied elsewhere",
            SettingsService.DescribeUnreadable("C:\\x\\settings.json", "access denied", null));
}
