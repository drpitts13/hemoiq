using System.Windows;
using System.Windows.Controls;
using HemoIQ.Views.Dialogs;

namespace HemoIQ.Tests;

/// <summary>
/// The Preferences dialog has more than twenty rows. On a short screen (for example 1366×768)
/// it must not grow past the screen: the settings scroll and Save/Cancel stay visible.
/// </summary>
[Collection("PersistentDatabase")]
public class SettingsDialogLayoutTests
{
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

    [Fact]
    public void Dialog_IsCappedToTheScreen_ByDefault() => OnSta(() =>
    {
        var dlg = new SettingsDialog();
        Assert.True(dlg.MaxHeight <= SystemParameters.WorkArea.Height);
        Assert.True(dlg.MaxHeight >= 300);
    });

    [Fact]
    public void OnAShortScreen_SettingsScroll_AndSaveStaysVisible() => OnSta(() =>
    {
        var dlg = new SettingsDialog
        {
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
            ShowInTaskbar = false, ShowActivated = false,
            MaxHeight = 420,
        };
        dlg.Show();
        try
        {
            dlg.UpdateLayout();
            Assert.True(dlg.ActualHeight <= 420.5, $"Dialog is {dlg.ActualHeight:0} px tall");

            var scroll = (ScrollViewer)dlg.FindName("FieldsScroll");
            Assert.True(scroll.ScrollableHeight > 0, "The settings should scroll on a short screen.");

            var save = (Button)dlg.FindName("SaveButton");
            var content = (FrameworkElement)dlg.Content;
            var bottom = save.TranslatePoint(new Point(0, save.ActualHeight), content).Y;
            Assert.True(save.ActualHeight > 0 && bottom <= content.ActualHeight + 0.5,
                $"Save ends at {bottom:0} px but the dialog content is {content.ActualHeight:0} px tall.");
        }
        finally
        {
            dlg.Close();
        }
    });
}
