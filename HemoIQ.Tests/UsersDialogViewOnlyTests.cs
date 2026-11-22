using System.Windows.Controls;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.Views.Dialogs;

namespace HemoIQ.Tests;

/// <summary>
/// Users & roles shows who is registered to anyone, but only an Administrator may change it: any other role sees
/// the dialog view-only with the reason, instead of filling in a user and being refused at Save.
/// </summary>
[Collection("PersistentDatabase")]
public class UsersDialogViewOnlyTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
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
    public void NonAdministrator_SeesUsersViewOnly_AdministratorCanSave()
    {
        using var iso = new IsolatedDatabase();
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "sup", Initials = "SU", Name = "Sup", Role = UserRole.Supervisor });
        });

        OnSta(() => As("sup", () =>
        {
            var dlg = new UsersDialog(iso.Db);
            Assert.EndsWith("(view only)", dlg.Title);
            var save = (Button)dlg.FindName("SaveUserButton");
            Assert.False(save.IsEnabled);
            Assert.Contains("cannot manage users and roles", save.ToolTip as string);
            Assert.Contains("View only:", ((TextBlock)dlg.FindName("CurrentUserText")).Text);
            Assert.Equal(2, ((System.Collections.IEnumerable)((DataGrid)dlg.FindName("UsersGrid")).ItemsSource).Cast<object>().Count());
        }));
        OnSta(() => As("adm", () =>
        {
            var dlg = new UsersDialog(iso.Db);
            Assert.DoesNotContain("view only", dlg.Title);
            Assert.True(((Button)dlg.FindName("SaveUserButton")).IsEnabled);
        }));
    }

    [Fact]
    public void FirstUser_CanBeRegistered_WhenNoneExist()
    {
        using var iso = new IsolatedDatabase();
        OnSta(() =>
        {
            var dlg = new UsersDialog(iso.Db);
            Assert.True(((Button)dlg.FindName("SaveUserButton")).IsEnabled);
            Assert.Null(dlg.ViewOnlyNote);
        });
    }
}
