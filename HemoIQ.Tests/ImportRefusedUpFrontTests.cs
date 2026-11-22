using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Load Demo Data and Import Lab Configuration both write. A role that cannot make the change is told before the
/// confirmation (or, when no import could apply, before choosing a file), not after it.
/// </summary>
[Collection("PersistentDatabase")]
public class ImportRefusedUpFrontTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static IsolatedDatabase WithRegistry()
    {
        var iso = new IsolatedDatabase();
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "sup", Initials = "SU", Name = "Sup", Role = UserRole.Supervisor });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tech", Role = UserRole.Technologist });
            iso.Db.SaveUser(new AppUser { Login = "ro", Initials = "RO", Name = "Reader", Role = UserRole.ReadOnly });
        });
        return iso;
    }

    private static string Bundle(bool withRule, bool withPreference)
    {
        using var source = new IsolatedDatabase();
        if (withRule) source.Db.AddRule("Kell three", "policy", "anti-K", null, false, 3);
        var settings = withPreference ? new LabSettings { LabName = "Reference Lab" } : new LabSettings();
        return ConfigurationBundle.Export(source.Db.GetAllRules(), settings, new DateTime(2026, 11, 9));
    }

    [Fact]
    public void DemoData_IsRefusedForAViewOnlySession_BeforeTheConfirmation()
    {
        using var iso = WithRegistry();
        As("ro", () =>
        {
            var reason = DemoDataGuard.RefusalReason(iso.Db);
            Assert.NotNull(reason);
            Assert.Contains("cannot record or change laboratory data", reason);
            Assert.EndsWith("Nothing was added.", reason);
        });
        As("adm", () => Assert.Null(DemoDataGuard.RefusalReason(iso.Db))); // empty database, allowed role
    }

    [Fact]
    public void Technologist_IsRefusedBeforeChoosingAFile_SupervisorIsNot()
    {
        using var iso = WithRegistry();
        As("tech", () => Assert.NotNull(ConfigurationBundle.RefusalBeforeChoosingAFile(iso.Db)));
        As("sup", () => Assert.Null(ConfigurationBundle.RefusalBeforeChoosingAFile(iso.Db)));  // may change rules
        As("adm", () => Assert.Null(ConfigurationBundle.RefusalBeforeChoosingAFile(iso.Db)));
    }

    [Fact]
    public void Supervisor_IsRefusedAFileThatChangesPreferences_BeforeTheConfirmation()
    {
        using var iso = WithRegistry();
        var current = new LabSettings();
        var withPrefs = ConfigurationBundle.Preview(Bundle(true, true), iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), current);
        var rulesOnly = ConfigurationBundle.Preview(Bundle(true, false), iso.Db.GetAllRules(), iso.Db.GetKnownAntigens(), current);

        As("sup", () =>
        {
            var refusal = ConfigurationBundle.Refusal(iso.Db, withPrefs, false);
            Assert.Contains("cannot change Preferences", refusal);
            Assert.DoesNotContain("analysis rules", refusal);   // rules are within the role
            Assert.Null(ConfigurationBundle.Refusal(iso.Db, rulesOnly, false));
            // Apply gives the same refusal and changes nothing.
            var ex = Assert.Throws<UnauthorizedAccessException>(() =>
                ConfigurationBundle.Apply(iso.Db, current, withPrefs, false, "f.json"));
            Assert.Equal(refusal, ex.Message);
            Assert.Empty(iso.Db.GetAllRules());
        });
        As("adm", () => Assert.Null(ConfigurationBundle.Refusal(iso.Db, withPrefs, false)));
    }
}
