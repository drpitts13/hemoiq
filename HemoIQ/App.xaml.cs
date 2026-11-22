using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HemoIQ.Help;
using HemoIQ.Services.Vendors;
using HemoIQ.Data;
using HemoIQ.Services;

namespace HemoIQ
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Before anything formats or parses a date: Gregorian calendar and ":" times, whatever the regional settings.
            AppCulture.Apply();
            AppDataPaths.MigrateUserData();
            DispatcherUnhandledException += (_, args) =>
            {
                AppLog.Error("Unhandled exception", args.Exception);
                MessageBox.Show(args.Exception.Message, "HemoIQ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            AppLog.Info("Application starting");
            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(OnPreviewKeyDown));

            var manualFlag = Array.FindIndex(e.Args, a =>
                string.Equals(a, "--write-manual", StringComparison.OrdinalIgnoreCase));
            if (manualFlag >= 0)
            {
                var path = manualFlag + 1 < e.Args.Length
                    ? e.Args[manualFlag + 1]
                    : Path.Combine(AppContext.BaseDirectory, "USER_MANUAL.md");
                var directory = Path.GetDirectoryName(Path.GetFullPath(path));
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(path, UserManual.Build());
                Shutdown();
                return;
            }

            if (e.Args.Any(a => string.Equals(a, "--seed-clinical", StringComparison.OrdinalIgnoreCase)))
            {
                using var db = new DatabaseService();
                // Same rule as Tools > Load Demo Data: only an empty or demo-only database, in one transaction.
                if (DemoDataGuard.RefusalReason(db) is string refusal)
                {
                    Console.Error.WriteLine(refusal);
                    AppLog.Info("--seed-clinical refused: " + refusal);
                    Shutdown(3);
                    return;
                }
                DemoDataGuard.Load(db, new AntibodyAnalyzer(db));
                Shutdown();
                return;
            }

            if (e.Args.Any(a => string.Equals(a, "--import-vendor-samples", StringComparison.OrdinalIgnoreCase)))
            {
                using var db = new DatabaseService();
                var results = HemoIQ.Services.Vendors.VendorLiveImport
                    .ImportRandomLotPerVendorAsync(db).GetAwaiter().GetResult();
                foreach (var row in results)
                    Console.WriteLine($"{row.Vendor}: {(row.Persisted ? "OK" : "SKIP")} {row.LotNumber} {row.Message}");
                Shutdown(results.Any(r => r.DownloadSupported && !r.Persisted) ? 2 : 0);
                return;
            }

            base.OnStartup(e);
            // Opened here rather than through StartupUri: the headless runs above return before any window
            // exists, so shutting them down cannot raise the quit confirmation and wait for a click.
            MainWindow window;
            try
            {
                window = new MainWindow();
            }
            catch (Exception ex)
            {
                // Without a main window the app would otherwise keep running invisibly after the error box.
                AppLog.Error("Could not start: database unavailable", ex);
                MessageBox.Show(StartupFailure.Describe(ex, DatabaseService.DefaultDbPath()), "HemoIQ could not start",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
            window.Show();
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F1 || e.Handled) return;
            if (sender is Views.Dialogs.HelpWindow) return;

            e.Handled = true;
            var origin = Keyboard.FocusedElement as DependencyObject ?? sender as DependencyObject;
            HelpService.Show(HelpLocator.From(origin));
        }
    }
}
