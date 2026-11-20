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
                var analyzer = new AntibodyAnalyzer(db);
                ClinicalDataSeeder.SeedIfNeeded(db, analyzer);
                DemoDataSeeder.SeedIfNeeded(db);
                DemoDataSeeder.AnalyzeIfNeverAnalyzed(db, analyzer);
                E2EWorkflowSeeder.SeedIfNeeded(db, analyzer);
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
