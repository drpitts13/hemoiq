using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// When the Preferences file cannot be written, the session must not keep running on settings that are neither on
/// disk nor audited (they would silently revert at the next start), and a write cut short must not leave a
/// truncated file that the next start reads as corrupt and replaces with defaults.
/// </summary>
[Collection("PersistentDatabase")]
public class SettingsSaveFailureTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hemoiq-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void WithSettings(Action<string> body)
    {
        var original = AppSettings.Current;
        var dir = TempDir();
        var path = Path.Combine(dir, "settings.json");
        try { body(path); }
        finally
        {
            if (File.Exists(path)) File.SetAttributes(path, FileAttributes.Normal);
            AppSettings.Current = original;
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void FailedSave_RestoresTheSavedSettings_LeavesTheFileWhole_AndSaysSo()
    {
        WithSettings(path =>
        {
            AppSettings.Current = new LabSettings { SeparationCellCount = 1 };
            SettingsService.Save(path);
            var onDisk = File.ReadAllText(path);
            File.SetAttributes(path, FileAttributes.ReadOnly);

            AppSettings.Current.SeparationCellCount = 3;
            var ex = Assert.Throws<SettingsSaveException>(() => SettingsService.Save(path));

            Assert.Contains("nothing was changed", ex.Message);
            Assert.Equal(1, AppSettings.Current.SeparationCellCount);   // not left running unsaved
            Assert.Equal(onDisk, File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        });
    }

    [Fact]
    public void Save_WritesTheWholeFile_AndLeavesNoTemporaryFile()
    {
        WithSettings(path =>
        {
            AppSettings.Current = new LabSettings { SeparationCellCount = 2, LabName = "Ref" };
            SettingsService.Save(path);
            Assert.False(File.Exists(path + ".tmp"));
            SettingsService.Load(path);
            Assert.Equal(2, AppSettings.Current.SeparationCellCount);
            Assert.Equal("Ref", AppSettings.Current.LabName);
        });
    }

    [Fact]
    public void Persist_DoesNotChangeTheSettingsInEffect()
    {
        WithSettings(path =>
        {
            AppSettings.Current = new LabSettings { SeparationCellCount = 1 };
            SettingsService.Persist(new LabSettings { SeparationCellCount = 4 }, path);
            Assert.Equal(1, AppSettings.Current.SeparationCellCount);
            Assert.Contains("\"SeparationCellCount\": 4", File.ReadAllText(path));
        });
    }

    [Fact]
    public void ConfigurationImport_WhosePreferencesCannotBeSaved_ChangesNothing()
    {
        using var source = new IsolatedDatabase();
        source.Db.AddRule("Kell three", "lab policy", "anti-K", null, false, 3);
        var json = ConfigurationBundle.Export(source.Db.GetAllRules(),
            new LabSettings { LabName = "Reference Lab", SeparationCellCount = 2 }, new DateTime(2026, 11, 9));

        using var target = new IsolatedDatabase();
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, target.Db.GetAllRules(), target.Db.GetKnownAntigens(), settings);
        var auditsBefore = target.Db.GetAuditEvents().Count;

        Assert.Throws<SettingsSaveException>(() => ConfigurationBundle.Apply(target.Db, settings, preview, false, "ref.json",
            persist: _ => throw new SettingsSaveException("disk full", new IOException("disk full"))));

        Assert.Empty(target.Db.GetAllRules());
        Assert.Equal(auditsBefore, target.Db.GetAuditEvents().Count);
        Assert.NotEqual("Reference Lab", settings.LabName);
    }

    [Fact]
    public void ConfigurationImport_PersistsTheNewPreferences_InsideTheImport()
    {
        using var source = new IsolatedDatabase();
        var json = ConfigurationBundle.Export(source.Db.GetAllRules(),
            new LabSettings { LabName = "Reference Lab" }, new DateTime(2026, 11, 9));
        using var target = new IsolatedDatabase();
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, target.Db.GetAllRules(), target.Db.GetKnownAntigens(), settings);
        LabSettings? persisted = null;

        ConfigurationBundle.Apply(target.Db, settings, preview, false, "ref.json", persist: s => persisted = s);

        Assert.Equal("Reference Lab", persisted?.LabName);
        Assert.Equal("Reference Lab", settings.LabName);
    }
}
