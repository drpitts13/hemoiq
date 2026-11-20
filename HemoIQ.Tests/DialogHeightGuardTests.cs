using System.Windows;
using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.Views.Dialogs;

namespace HemoIQ.Tests;

/// <summary>
/// Every dialog must fit a 1366×768 workstation: the work area there is about 728 px tall
/// (768 minus the taskbar). A dialog taller than that hides its Save/Cancel buttons below the
/// screen (found in Preferences, Iteration 122). Each dialog is shown off-screen with no height
/// cap and its natural height is measured; a dialog that cannot fit must scroll and cap itself
/// like Preferences does.
/// </summary>
[Collection("PersistentDatabase")]
public class DialogHeightGuardTests
{
    private const double SmallScreenWorkArea = 728;

    private static void OnSta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
    }

    private static double NaturalHeight(Window dlg, bool capsItself)
    {
        dlg.WindowStartupLocation = WindowStartupLocation.Manual;
        dlg.Left = -10000; dlg.Top = -10000; dlg.ShowInTaskbar = false; dlg.ShowActivated = false;
        if (!capsItself) dlg.MaxHeight = double.PositiveInfinity;
        dlg.Show();
        try
        {
            dlg.UpdateLayout();
            return dlg.ActualHeight;
        }
        finally
        {
            dlg.Close();
        }
    }

    [Fact]
    public void EveryDialog_FitsASmallScreen_OrCapsItself() => OnSta(() =>
    {
        using var iso = new IsolatedDatabase();
        var dialogs = new (string Name, Func<Window> Make, bool CapsItself)[]
        {
            ("Specimen", () => new SpecimenDialog(), false),
            ("Patient", () => new PatientDialog("MRN-1", null), false),
            ("Panel", () => new PanelDialog(), false),
            ("Panel run", () => new PanelRunDialog(), false),
            ("Rule", () => new RuleDialog(), false),
            ("Reason", () => new ReasonDialog("Enter the reason for this change."), false),
            ("Purge database", () => new PurgeDatabaseDialog(iso.Db), false),
            ("Preferences", () => new SettingsDialog(), true),
        };
        var tooTall = new List<string>();
        foreach (var (name, make, caps) in dialogs)
        {
            var height = NaturalHeight(make(), caps);
            var limit = caps ? Math.Max(300, SystemParameters.WorkArea.Height - 40) + 0.5 : SmallScreenWorkArea;
            if (height > limit) tooTall.Add($"{name}: {height:0} px (limit {limit:0})");
        }
        Assert.True(tooTall.Count == 0,
            "Dialogs taller than a 1366×768 screen hide their buttons. Scroll the fields and cap the window " +
            "(see SettingsDialog): " + string.Join("; ", tooTall));
    });
}
