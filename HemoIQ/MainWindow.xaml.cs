using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HemoIQ.Help;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.ViewModels;

namespace HemoIQ
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            // Wire commands onto the VM
            vm.ExitCommand = new RelayCommand(Close);
            vm.SaveCurrentCommand = new RelayCommand(SaveCurrent);
            vm.RefreshAllCommand = new RelayCommand(vm.RefreshAll);
            vm.NewItemCommand = new RelayCommand(NewItem);
            vm.ShowManualCommand = new RelayCommand(HelpService.ShowManual);
            vm.ShowShortcutsCommand = new RelayCommand(ShowShortcuts);
            BuildHelpTopicsMenu();
            vm.ShowAboutCommand = new RelayCommand(ShowAbout);
            vm.LoadDemoDataCommand = new RelayCommand(LoadDemoData);
            vm.ShowSettingsCommand = new RelayCommand(ShowSettings);
            vm.ShowPurgeDatabaseCommand = new RelayCommand(ShowPurgeDatabase);
            vm.ShowOpenArchiveCommand = new RelayCommand(ShowOpenArchive);
            vm.ShowAuditLogCommand = new RelayCommand(ShowAuditLog);
            vm.ExportConfigurationCommand = new RelayCommand(ExportConfiguration);
            vm.ImportConfigurationCommand = new RelayCommand(ImportConfiguration);

            Loaded += MainWindow_Loaded;

            Closing += (s, e) =>
            {
                var unsaved = UnsavedWorkGuard.QuitWarning(
                    vm.ReactionsVM.UnsavedGradesDescription,
                    vm.PanelsVM.IsEditingAntigens ? vm.PanelsVM.SelectedPanel?.Name ?? "(selected panel)" : null);
                if (unsaved != null
                        ? MessageBox.Show(unsaved, "Unsaved work", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                              MessageBoxResult.No) != MessageBoxResult.Yes
                        : MessageBox.Show("Quit the application?", "Confirm",
                              MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
                else
                {
                    // A deliberate quit discards unsaved grades; only an unclean exit leaves a draft.
                    vm.ReactionsVM.DiscardDraft();
                    HelpService.Close();
                    vm.Dispose();
                }
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DemoDataSeeder.AnalyzeIfNeverAnalyzed(ViewModel.Database, ViewModel.Analyzer);
            ViewModel.PanelsVM.Refresh();
            ViewModel.WorklistVM.Refresh();
            WarnIfPreferencesUnreadable();
            AcknowledgeIntendedUseIfNeeded();
            ViewModel.RefreshSession();
            if (ViewModel.SessionViewOnly)
                ViewModel.SetStatus(ViewModel.SessionText + ". Commands that change laboratory data are disabled.");
            ViewModel.ReactionsVM.OfferDraftRecovery();
            ViewModel.NotifyCapacityIfNeeded();
            if (!ViewModel.IsDatabaseNearCapacity) return;

            var cap = ViewModel.GetCapacityStatus();
            var canPurge = AccessControl.Can(ViewModel.Database, Permission.PurgeData);
            // Only a role that can purge is offered the purge; anyone else is told who can.
            var result = MessageBox.Show(
                $"The database is {cap.PercentUsed:0}% of the configured maximum size " +
                $"({DatabaseCapacityStatus.FormatBytes(cap.FileBytes)} / {DatabaseCapacityStatus.FormatBytes(cap.MaxBytes)}).\n\n" +
                AccessControl.PurgeOffer(ViewModel.Database),
                "Database nearly full",
                canPurge ? MessageBoxButton.YesNo : MessageBoxButton.OK,
                MessageBoxImage.Warning,
                canPurge ? MessageBoxResult.No : MessageBoxResult.OK);
            if (canPurge && result == MessageBoxResult.Yes)
                ShowPurgeDatabase();
        }

        private void ShowPurgeDatabase()
        {
            // Refused before the cutoff and archive are chosen, not after the confirmation.
            if (AccessControl.Refusal(ViewModel.Database, Permission.PurgeData) is string refusal)
            {
                MessageBox.Show(this, refusal, "Purge Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dlg = new Views.Dialogs.PurgeDatabaseDialog(ViewModel.Database) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            ViewModel.RefreshAll();
            var status = $"Purged {dlg.DeletedCount} specimen(s). Database is now {dlg.SizeAfterDisplay}.";
            if (!string.IsNullOrEmpty(dlg.ArchivePath))
                status += $" Archive: {dlg.ArchivePath}";
            ViewModel.SetStatus(status);
            ViewModel.NotifyCapacityIfNeeded();

            var archiveNote = string.IsNullOrEmpty(dlg.ArchivePath)
                ? ""
                : $"\n\nArchive saved to:\n{dlg.ArchivePath}";
            MessageBox.Show(
                $"Removed {dlg.DeletedCount} specimen(s).\nDatabase size is now {dlg.SizeAfterDisplay}.{archiveNote}",
                "Purge complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowOpenArchive()
        {
            var dlg = new Views.Dialogs.RestoreArchiveDialog(ViewModel.Database) { Owner = this };
            if (dlg.ShowDialog() != true || dlg.RestoreResult == null) return;

            ViewModel.RefreshAll();
            var result = dlg.RestoreResult;
            var status =
                $"Restored {result.SpecimensRestored} specimen(s)" +
                (result.SpecimensSkipped > 0 ? $", skipped {result.SpecimensSkipped} already present" : "") +
                ".";
            ViewModel.SetStatus(status);
            ViewModel.NotifyCapacityIfNeeded();
            var review = result.ReviewFindings.Count == 0 ? "" :
                $"\n\nReview before use ({result.ReviewFindings.Count}); restored as archived, nothing was changed:\n• " +
                string.Join("\n• ", result.ReviewFindings.Take(8)) +
                (result.ReviewFindings.Count > 8 ? $"\n• … and {result.ReviewFindings.Count - 8} more in the audit log entry for this restore." : "");
            MessageBox.Show(
                status +
                (result.PanelsRestored > 0 ? $"\nAlso restored {result.PanelsRestored} panel(s)." : "") + review,
                "Restore complete",
                MessageBoxButton.OK,
                result.ReviewFindings.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MainTabControl) return;
            var header = (MainTabControl.SelectedItem as TabItem)?.Header as string;
            if (header == "Analysis")
                ViewModel.AnalysisVM.OnTabSelected();
            else if (header == "Worklist")
                ViewModel.WorklistVM.Refresh();
            else if (header == "Reports")
                ViewModel.ReportsVM.OnTabSelected();
            else if (header == "Analytics")
                ViewModel.AnalyticsVM.OnTabSelected();
            else if (header == "Search")
                ViewModel.SearchVM.OnTabSelected();
            else if (header == "Reactions")
                ViewModel.ReactionsVM.RefreshLockState();
            ViewModel.AnnounceScreen(header);
        }

        private void SaveCurrent()
        {
            switch ((MainTabControl.SelectedItem as TabItem)?.Header as string)
            {
                case "Panels": RunShortcut(ViewModel.PanelsVM.SaveCellsCommand, "Save"); break;
                case "Reactions": RunShortcut(ViewModel.ReactionsVM.SaveCommand, "Save"); break;
                default: ViewModel.SetStatus("No save action for this tab."); break;
            }
        }

        /// <summary>A shortcut runs only when the matching button would be enabled; otherwise the status says why.</summary>
        private void RunShortcut(ICommand command, string action)
        {
            if (ShortcutCommand.Run(command, action, SessionAccess.Current) is string refused)
                ViewModel.SetStatus(refused);
        }

        private void NewItem()
        {
            switch ((MainTabControl.SelectedItem as TabItem)?.Header as string)
            {
                case "Specimens": RunShortcut(ViewModel.SpecimensVM.AddCommand, "New specimen"); break;
                case "Panels": RunShortcut(ViewModel.PanelsVM.AddCommand, "New panel"); break;
                case "Rules": RunShortcut(ViewModel.RulesVM.AddCommand, "New rule"); break;
                default: ViewModel.SetStatus("No new item action for this tab."); break;
            }
        }

        private void ShowSettings()
        {
            var dlg = new Views.Dialogs.SettingsDialog(ViewModel.Database) { Owner = this };
            var marker = ViewModel.Database.LastAnalysisConfigurationChange();
            var saved = dlg.ShowDialog() == true;
            ViewModel.RefreshSession(); // users and roles may have changed even when Preferences were cancelled
            if (saved)
                ViewModel.SetStatus(MainViewModel.PreferencesSavedStatus(
                    AnalysisConfigurationMarker.Moved(marker, ViewModel.Database.LastAnalysisConfigurationChange()),
                    MainViewModel.CapacityText(ViewModel.GetCapacityStatus())));
        }

        private void ShowShortcuts() => HelpService.Show(HelpCatalog.ShortcutsId);

        private void BuildHelpTopicsMenu()
        {
            HelpTopicsMenu.Items.Clear();
            foreach (var node in HelpCatalog.Tree())
                HelpTopicsMenu.Items.Add(CreateTopicMenu(node));
        }

        private static MenuItem CreateTopicMenu(HelpTopicNode node)
        {
            var item = new MenuItem { Header = node.Title.Replace("_", "__") };
            if (node.Children.Count == 0)
            {
                item.Command = HelpCommands.ShowTopic;
                item.CommandParameter = node.Id;
                return item;
            }

            item.Items.Add(new MenuItem
            {
                Header = node.Title.Replace("_", "__"),
                Command = HelpCommands.ShowTopic,
                CommandParameter = node.Id
            });
            foreach (var child in node.Children)
                item.Items.Add(CreateTopicMenu(child));
            return item;
        }

        private void LoadDemoData()
        {
            if (DemoDataGuard.RefusalReason(ViewModel.Database) is string refusal)
            {
                MessageBox.Show(refusal, "Load Demo Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show(
                    "This will add sample specimens, panels, and results if they are not already present.\n" +
                    "Existing data will not be removed. Continue?",
                    "Load Demo Data", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                DemoDataGuard.Load(ViewModel.Database, ViewModel.Analyzer);
                ViewModel.RefreshAll();
                ViewModel.SetStatus("Sample workload and demo scenarios loaded.");
                MessageBox.Show(
                    "Sample data loaded.\n\n" +
                    "• 10 clinical specimens (2026-001 … 2026-010) on 5 shared panels\n" +
                    "• Enzyme (ficin) and absorption runs mixed in\n" +
                    "• 11 DEMO- scenarios including warehouse antigens with ficin/DTT runs\n" +
                    "• E2E- workups for warm, cold, auto, exotic, and combination patterns",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Demo data was not loaded: {ex.Message}\n\nNothing was added (the load is one transaction).",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAuditLog()
        {
            var dlg = new Views.Dialogs.AuditLogDialog(ViewModel.Database) { Owner = this };
            dlg.ShowDialog();
        }

        private void ExportConfiguration()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Lab Configuration",
                Filter = "HemoIQ configuration (*.json)|*.json",
                FileName = $"hemoiq-configuration-{System.DateTime.Now:yyyyMMdd}.json",
            };
            if (dlg.ShowDialog(this) != true) return;
            var rules = ViewModel.Database.GetAllRules();
            try
            {
                System.IO.File.WriteAllText(dlg.FileName, ConfigurationBundle.Export(rules, AppSettings.Current, System.DateTime.Now));
            }
            catch (System.Exception ex) when (ex is System.IO.IOException or System.UnauthorizedAccessException)
            {
                MessageBox.Show($"The configuration was not exported: {ex.Message}", "Export Lab Configuration",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ViewModel.SetStatus($"Lab configuration ({rules.Count} rule(s) and Preferences) exported to {System.IO.Path.GetFileName(dlg.FileName)}.");
        }

        private void ImportConfiguration()
        {
            var db = ViewModel.Database;
            if (ConfigurationBundle.RefusalBeforeChoosingAFile(db) is string cannot)
            {
                MessageBox.Show(this, cannot, "Import Lab Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Import Lab Configuration", Filter = "HemoIQ configuration (*.json)|*.json" };
            if (dlg.ShowDialog(this) != true) return;
            ConfigurationPreview preview;
            try
            {
                preview = ConfigurationBundle.Preview(System.IO.File.ReadAllText(dlg.FileName), db.GetAllRules(),
                    db.GetKnownAntigens(), AppSettings.Current);
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.IO.IOException)
            {
                MessageBox.Show(ex.Message, "Configuration not imported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var summary = ConfigurationBundle.Summarize(preview);
            if (preview.HasProblems)
            {
                MessageBox.Show("Nothing was changed: the file has refused entries, so it would not reproduce its configuration.\n\n" + summary,
                    "Configuration not imported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var removeLocalOnly = false;
            if (preview.LocalOnlyRules.Count > 0)
            {
                var answer = MessageBox.Show(summary + $"\n\n{preview.LocalOnlyRules.Count} stored rule(s) are not in the file. " +
                        "Remove them so the rules match the file exactly?\n\nYes: remove them (audited). No: keep them.",
                    "Import Lab Configuration", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
                if (answer == MessageBoxResult.Cancel) return;
                removeLocalOnly = answer == MessageBoxResult.Yes;
            }
            if (!preview.HasChanges && !removeLocalOnly)
            {
                MessageBox.Show("Nothing to change.\n\n" + summary, "Import Lab Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // A Supervisor may change rules but not Preferences: refuse this file before asking to apply it.
            if (ConfigurationBundle.Refusal(db, preview, removeLocalOnly) is string refused)
            {
                MessageBox.Show(this, "Nothing was changed.\n\n" + refused + "\n\n" + summary, "Configuration not imported",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show(summary + "\n\nApply the rules and Preferences together now?", "Import Lab Configuration",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            ConfigurationImportResult result;
            var marker = db.LastAnalysisConfigurationChange();
            try
            {
                result = ConfigurationBundle.Apply(db, AppSettings.Current, preview, removeLocalOnly, System.IO.Path.GetFileName(dlg.FileName),
                    persist: s => SettingsService.Persist(s));
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.UnauthorizedAccessException or SettingsSaveException)
            {
                MessageBox.Show("Nothing was changed.\n\n" + ex.Message, "Configuration not imported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AppSettings.RaiseChanged();
            ViewModel.RulesVM.Refresh();
            ViewModel.WorklistVM.Refresh();
            ViewModel.SetStatus(ConfigurationBundle.ImportStatus(result,
                AnalysisConfigurationMarker.Moved(marker, db.LastAnalysisConfigurationChange())));
        }

        /// <summary>Says so when start-up fell back to default Preferences because the file could not be read.</summary>
        private void WarnIfPreferencesUnreadable()
        {
            if (SettingsService.LastLoadProblem is not string problem) return;
            MessageBox.Show(this, problem, "Review Preferences", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AcknowledgeIntendedUseIfNeeded()
        {
            if (AppSettings.Current.IntendedUseAcknowledged) return;
            MessageBox.Show(SoftwareIdentity.IntendedUse, "Intended use",
                MessageBoxButton.OK, MessageBoxImage.Information);
            AppSettings.Current.IntendedUseAcknowledged = true;
            SettingsService.Save();
        }

        private void ShowAbout()
        {
            MessageBox.Show(SoftwareIdentity.AboutText(), "About",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
