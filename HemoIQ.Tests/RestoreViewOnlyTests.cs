using System.Reflection;
using System.Windows.Controls;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.Views.Dialogs;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// Anyone may open and read an archive, but restoring writes specimens: a view-only session sees Restore disabled,
/// with the reason, rather than being refused after confirming.
/// </summary>
[Collection("PersistentDatabase")]
public class RestoreViewOnlyTests
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

    private static (Button Restore, TextBlock Summary) Open(IsolatedDatabase iso, string archive)
    {
        var dlg = new RestoreArchiveDialog(iso.Db);
        typeof(RestoreArchiveDialog).GetMethod("LoadArchive", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(dlg, new object[] { archive });
        return ((Button)dlg.FindName("RestoreButton"), (TextBlock)dlg.FindName("SummaryText"));
    }

    [Fact]
    public void ViewOnlySession_CanReadTheArchive_ButRestoreIsDisabledWithTheReason()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Arch", "AR-1", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("ARCH-OLD", "serum", null, createdDate: "2020-01-15");
        iso.Db.LinkSpecimenPanel("ARCH-OLD", panel);
        iso.Db.SaveReaction("ARCH-OLD", panel, "1", "0", "0", "0", "2+");
        var archive = Path.Combine(Path.GetTempPath(), $"hemoiq_viewonly_{Guid.NewGuid():N}.db");
        iso.Db.PurgeSpecimensCreatedBefore("2024-01-01", archive);
        SqliteConnection.ClearAllPools();
        try
        {
            As("adm", () =>
            {
                iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
                iso.Db.SaveUser(new AppUser { Login = "ro", Initials = "RO", Name = "Reader", Role = UserRole.ReadOnly });
            });

            OnSta(() => As("ro", () =>
            {
                var (restore, summary) = Open(iso, archive);
                Assert.Contains("can be restored", summary.Text);   // the archive was read
                Assert.False(restore.IsEnabled);
                Assert.Contains("cannot record or change laboratory data", restore.ToolTip as string);
                Assert.Contains("View only:", summary.Text);
            }));
            OnSta(() => As("adm", () =>
            {
                var (restore, summary) = Open(iso, archive);
                Assert.True(restore.IsEnabled);
                Assert.Null(restore.ToolTip);
                Assert.DoesNotContain("View only", summary.Text);
            }));
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(archive); }
    }
}
