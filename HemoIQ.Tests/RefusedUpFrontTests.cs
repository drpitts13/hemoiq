using System.Windows.Controls;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.Views.Dialogs;

namespace HemoIQ.Tests;

/// <summary>
/// A role that may not purge or change Preferences is told so before it fills in the dialog, and the start-up
/// "database nearly full" prompt offers the purge only to a role that can run it.
/// </summary>
[Collection("PersistentDatabase")]
public class RefusedUpFrontTests
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
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tech", Role = UserRole.Technologist });
        });
        return iso;
    }

    private static void OnSta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { body(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
    }

    [Fact]
    public void Refusal_IsTheDemandMessage_OrNull()
    {
        using var iso = WithRegistry();
        As("tech", () => Assert.Contains("cannot purge specimens from the database", AccessControl.Refusal(iso.Db, Permission.PurgeData)));
        As("adm", () => Assert.Null(AccessControl.Refusal(iso.Db, Permission.PurgeData)));
        using var open = new IsolatedDatabase();
        Assert.Null(AccessControl.Refusal(open.Db, Permission.PurgeData)); // no users registered
    }

    [Fact]
    public void CapacityPrompt_OffersThePurgeOnlyToARoleThatCanRunIt()
    {
        using var iso = WithRegistry();
        As("adm", () => Assert.Equal("Would you like to purge old specimen data?", AccessControl.PurgeOffer(iso.Db)));
        As("tech", () =>
        {
            var offer = AccessControl.PurgeOffer(iso.Db);
            Assert.StartsWith("Ask an Administrator to purge old specimen data", offer);
            Assert.DoesNotContain("Would you like", offer);
        });
    }

    [Fact]
    public void Preferences_OpenViewOnly_ForARoleThatCannotSave()
    {
        using var iso = WithRegistry();
        OnSta(() => As("tech", () =>
        {
            var dlg = new SettingsDialog(iso.Db);
            Assert.EndsWith("(view only)", dlg.Title);
            var save = (Button)dlg.FindName("SaveButton");
            var import = (Button)dlg.FindName("ImportButton");
            Assert.False(save.IsEnabled);
            Assert.False(import.IsEnabled);
            Assert.Contains("cannot change Preferences", save.ToolTip as string);
        }));
        OnSta(() => As("adm", () =>
        {
            var dlg = new SettingsDialog(iso.Db);
            Assert.DoesNotContain("view only", dlg.Title);
            Assert.True(((Button)dlg.FindName("SaveButton")).IsEnabled);
        }));
    }
}
