using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class ConfigurationBundleTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 10, 0, 0);

    private static string SourceBundle()
    {
        using var source = new IsolatedDatabase();
        source.Db.AddRule("Kell three", "lab policy", "anti-K", null, false, 3);
        source.Db.AddRule("Fya het", null, "anti-Fya", null, true, 2);
        var settings = new LabSettings { LabName = "Reference Lab", SeparationCellCount = 2, DefaultMinRuleoutCount = 2 };
        return ConfigurationBundle.Export(source.Db.GetAllRules(), settings, Now);
    }

    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    [Fact]
    public void ExportThenImport_ReproducesRulesAndPreferences_InOneAuditedStep()
    {
        var json = SourceBundle();
        Assert.Contains("\"format\": \"hemoiq-configuration\"", json);
        Assert.Contains("\"hemoiq-rules\"", json);
        Assert.Contains("\"hemoiq-preferences\"", json);

        using var target = new IsolatedDatabase();
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, target.Db.GetAllRules(), target.Db.GetKnownAntigens(), settings);
        Assert.False(preview.HasProblems);
        Assert.True(preview.HasChanges);
        Assert.Empty(target.Db.GetAllRules());               // preview writes nothing
        Assert.NotEqual("Reference Lab", settings.LabName);

        var result = ConfigurationBundle.Apply(target.Db, settings, preview, removeLocalOnlyRules: false, "ref.json");

        Assert.Equal(new ConfigurationImportResult(2, 0, 0, 3), result);
        Assert.Equal(3, Assert.Single(target.Db.GetAllRules(), r => r.Antibody == "anti-K").MinRuleoutCount);
        Assert.Equal("Reference Lab", settings.LabName);
        Assert.Equal(2, settings.SeparationCellCount);
        var audits = target.Db.GetAuditEvents();
        var bundle = Assert.Single(audits, e => e.Action == "import_configuration");
        Assert.Equal("ref.json", bundle.Reason);
        Assert.Contains("\"rulesAdded\":2", bundle.AfterJson);
        Assert.Contains("separationCellCount", bundle.AfterJson);
        Assert.Contains(audits, e => e.Action == "update_settings");   // analyses become stale
        Assert.Equal(2, audits.Count(e => e.Action == "add_rule"));
    }

    [Fact]
    public void StoredRulesMissingFromTheFile_AreListed_KeptByDefault_RemovedOnRequest()
    {
        var json = SourceBundle();
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("Local Jka", null, "anti-Jka", null, false, 2);
        var settings = new LabSettings();

        var preview = ConfigurationBundle.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), settings);
        Assert.Equal("anti-Jka", Assert.Single(preview.LocalOnlyRules).Antibody);
        Assert.Contains("Stored rule(s) not in the file: anti-Jka 'Local Jka'", ConfigurationBundle.Summarize(preview));

        ConfigurationBundle.Apply(iso.Db, settings, preview, removeLocalOnlyRules: false, null);
        Assert.Contains(iso.Db.GetAllRules(), r => r.Antibody == "anti-Jka");

        var again = ConfigurationBundle.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), settings);
        Assert.False(again.HasChanges);
        Assert.Equal(new ConfigurationImportResult(0, 0, 1, 0),
            ConfigurationBundle.Apply(iso.Db, settings, again, removeLocalOnlyRules: true, null));
        Assert.DoesNotContain(iso.Db.GetAllRules(), r => r.Antibody == "anti-Jka");
        Assert.Contains(iso.Db.GetAuditEvents(), e => e.Action == "delete_rule");
    }

    [Fact]
    public void FileWithARefusedEntry_IsNotApplied_AndNothingChanges()
    {
        var json = SourceBundle().Replace("\"separationCellCount\": 2", "\"separationCellCount\": 9");
        using var iso = new IsolatedDatabase();
        var settings = new LabSettings();

        var preview = ConfigurationBundle.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), settings);
        Assert.True(preview.HasProblems);
        Assert.Contains("separationCellCount: 9 is outside 0–5", ConfigurationBundle.Summarize(preview));

        Assert.Throws<ArgumentException>(() => ConfigurationBundle.Apply(iso.Db, settings, preview, false, null));
        Assert.Empty(iso.Db.GetAllRules());
        Assert.NotEqual("Reference Lab", settings.LabName);
        Assert.DoesNotContain(iso.Db.GetAuditEvents(), e => e.Action == "import_configuration");
    }

    [Fact]
    public void Preview_RefusesFilesThatAreNotConfigurationBundles()
    {
        using var iso = new IsolatedDatabase();
        var rulesOnly = RuleTransfer.Export(Array.Empty<Rule>(), Now);
        Assert.Contains("not a HemoIQ configuration export",
            Assert.Throws<ArgumentException>(() => ConfigurationBundle.Preview(rulesOnly, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), new LabSettings())).Message);
        Assert.Contains("reads version 1 or earlier",
            Assert.Throws<ArgumentException>(() => ConfigurationBundle.Preview("""{"format":"hemoiq-configuration","version":2}""",
                iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), new LabSettings())).Message);
        Assert.Contains("no preferences section",
            Assert.Throws<ArgumentException>(() => ConfigurationBundle.Preview("""{"format":"hemoiq-configuration","version":1,"rules":{"format":"hemoiq-rules","version":1}}""",
                iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), new LabSettings())).Message);
    }

    [Fact]
    public void Supervisor_CannotApplyPreferenceChanges_AndTheRulesRollBackToo()
    {
        var json = SourceBundle();
        using var iso = new IsolatedDatabase();
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "sup", Initials = "SU", Name = "Sup", Role = UserRole.Supervisor });
        });
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), settings);

        As("sup", () => Assert.Throws<UnauthorizedAccessException>(() =>
            ConfigurationBundle.Apply(iso.Db, settings, preview, false, null)));
        Assert.Empty(iso.Db.GetAllRules());
        Assert.NotEqual("Reference Lab", settings.LabName);

        As("adm", () => ConfigurationBundle.Apply(iso.Db, settings, preview, false, null));
        Assert.Equal(2, iso.Db.GetAllRules().Count);
        Assert.Equal("Reference Lab", settings.LabName);
    }

    [Fact]
    public void FailureDuringApply_RollsBackEveryWrite_AndLeavesSettingsUntouched()
    {
        var json = SourceBundle();
        using var iso = new IsolatedDatabase();
        var settings = new LabSettings();
        var preview = ConfigurationBundle.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), settings);
        // A rule with the same antibody appears after the preview, so the planned add now fails.
        iso.Db.AddRule("Raced", null, "anti-Fya", null, false, 1);

        Assert.ThrowsAny<Exception>(() => ConfigurationBundle.Apply(iso.Db, settings, preview, false, null));
        Assert.Equal("Raced", Assert.Single(iso.Db.GetAllRules()).Name);
        Assert.NotEqual("Reference Lab", settings.LabName);
        Assert.DoesNotContain(iso.Db.GetAuditEvents(), e => e.Action is "import_configuration" or "update_settings");
    }
}
