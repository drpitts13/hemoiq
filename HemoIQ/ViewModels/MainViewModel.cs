using System;
using System.Linq;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        public DatabaseService Database { get; }
        public AntibodyAnalyzer Analyzer { get; }

        public WorklistViewModel WorklistVM { get; }
        public SpecimensViewModel SpecimensVM { get; }
        public PanelsViewModel PanelsVM { get; }
        public ReactionsViewModel ReactionsVM { get; }
        public AnalysisViewModel AnalysisVM { get; }
        public ReportsViewModel ReportsVM { get; }
        public AnalyticsViewModel AnalyticsVM { get; }
        public SearchViewModel SearchVM { get; }
        public RulesViewModel RulesVM { get; }

        // Set by MainWindow after construction
        public ICommand? ExitCommand { get; set; }
        public ICommand? SaveCurrentCommand { get; set; }
        public ICommand? RefreshAllCommand { get; set; }
        public ICommand? NewItemCommand { get; set; }
        public ICommand? ShowManualCommand { get; set; }
        public ICommand? ShowShortcutsCommand { get; set; }
        public ICommand? ShowAboutCommand { get; set; }
        public ICommand? LoadDemoDataCommand { get; set; }
        public ICommand? ShowSettingsCommand { get; set; }
        public ICommand? ShowPurgeDatabaseCommand { get; set; }
        public ICommand? ShowOpenArchiveCommand { get; set; }
        public ICommand? ShowAuditLogCommand { get; set; }
        public ICommand? ExportConfigurationCommand { get; set; }
        public ICommand? ImportConfigurationCommand { get; set; }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetField(ref _selectedTabIndex, value);
        }

        public MainViewModel()
        {
            SettingsService.Load();
            Database = new DatabaseService();
            Analyzer = new AntibodyAnalyzer(Database);

            WorklistVM = new WorklistViewModel(Database, this);
            SpecimensVM = new SpecimensViewModel(Database, this);
            PanelsVM = new PanelsViewModel(Database, this);
            ReactionsVM = new ReactionsViewModel(Database, Analyzer, this);
            AnalysisVM = new AnalysisViewModel(Database, Analyzer, this);
            ReportsVM = new ReportsViewModel(Database, this);
            AnalyticsVM = new AnalyticsViewModel(Database, this);
            SearchVM = new SearchViewModel(Database, this);
            RulesVM = new RulesViewModel(Database, this);

            SpecimensVM.ShowInactive = AppSettings.Current.ShowInactiveByDefault;
            PanelsVM.ShowInactive = AppSettings.Current.ShowInactiveByDefault;
            ApplyDatabaseSizeLimit();
            AppSettings.Changed += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            SpecimensVM.ShowInactive = AppSettings.Current.ShowInactiveByDefault;
            PanelsVM.ShowInactive = AppSettings.Current.ShowInactiveByDefault;
            ReactionsVM.ApplyColumnVisibilitySettings();
            AnalysisVM.RefreshStaleState();
            WorklistVM.Refresh();
            ApplyDatabaseSizeLimit();
            NotifyCapacityIfNeeded();
        }

        public DatabaseCapacityStatus GetCapacityStatus()
        {
            var maxBytes = DatabaseCapacityStatus.BytesFromMb(AppSettings.Current.MaxDatabaseSizeMb);
            return Database.GetCapacityStatus(maxBytes);
        }

        public bool IsDatabaseNearCapacity => GetCapacityStatus().IsNearCapacity;

        public void ApplyDatabaseSizeLimit()
        {
            var maxBytes = DatabaseCapacityStatus.BytesFromMb(AppSettings.Current.MaxDatabaseSizeMb);
            Database.ApplyMaxPageCount(maxBytes);
        }

        public void NotifyCapacityIfNeeded()
        {
            if (CapacityText(GetCapacityStatus()) is string text) SetStatus(text);
        }

        /// <summary>The near-capacity warning for the status line, or null when the database is not near capacity.</summary>
        public static string? CapacityText(DatabaseCapacityStatus cap) =>
            cap.IsNearCapacity
                ? $"Database {cap.PercentUsed:0}% full ({DatabaseCapacityStatus.FormatBytes(cap.FileBytes)} / {DatabaseCapacityStatus.FormatBytes(cap.MaxBytes)})"
                : null;

        /// <summary>
        /// The status after Preferences are saved: says when a clinical change marked open analyses stale (as rule
        /// edits and imports do) and keeps the near-capacity warning rather than replacing one with the other.
        /// </summary>
        public static string PreferencesSavedStatus(bool analysisAffected, string? capacity) =>
            "Preferences saved." +
            (analysisAffected ? " Open investigations analyzed before this change are listed on the Worklist as Stale analysis." : "") +
            (capacity == null ? "" : " " + capacity + ".");

        public void SetStatus(string message) => StatusText = message;

        private SessionInfo _session = SessionAccess.Current;
        public string SessionText => _session.Text;
        public string SessionDetail => _session.Detail;
        public bool SessionViewOnly => _session.ViewOnly;

        /// <summary>Re-reads who is signed in and what they may do; change commands follow on the next requery.</summary>
        public void RefreshSession()
        {
            _session = SessionAccess.Refresh(Database);
            OnPropertyChanged(nameof(SessionText));
            OnPropertyChanged(nameof(SessionDetail));
            OnPropertyChanged(nameof(SessionViewOnly));
            ReactionsVM.NotifySessionChanged();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Replace the status line with the screen the user just opened.
        /// </summary>
        public void AnnounceScreen(string? header)
        {
            switch (header)
            {
                case "Worklist": WorklistVM.AnnounceStatus(); break;
                case "Specimens": SpecimensVM.AnnounceStatus(); break;
                case "Panels": PanelsVM.AnnounceStatus(); break;
                case "Reactions": ReactionsVM.AnnounceStatus(); break;
                case "Analysis": AnalysisVM.AnnounceStatus(); break;
                case "Reports": ReportsVM.AnnounceStatus(); break;
                case "Analytics": AnalyticsVM.AnnounceStatus(); break;
                case "Search": SearchVM.AnnounceStatus(); break;
                case "Rules": RulesVM.AnnounceStatus(); break;
            }
        }

        public void RefreshAll()
        {
            RefreshSession();
            WorklistVM.Refresh();
            SpecimensVM.Refresh();
            PanelsVM.Refresh();
            ReactionsVM.RefreshSpecimens();
            AnalysisVM.Refresh();
            ReportsVM.Refresh();
            AnalyticsVM.Refresh();
            RulesVM.Refresh();
            SetStatus("All tabs refreshed");
        }

        public void NavigateToWorklistItem(WorklistItem item)
        {
            switch (item.TargetTab)
            {
                case "Reactions":
                    OpenSpecimenReactions(item.AccessionNumber);
                    break;
                case "Analysis":
                    OpenSpecimenAnalysis(item.AccessionNumber);
                    break;
                case "Panels":
                    SelectedTabIndex = 2;
                    if (item.PanelId is int pid)
                        PanelsVM.SelectPanel(pid);
                    break;
                case "Rules":
                    SelectedTabIndex = 8;
                    break;
                default:
                    if (item.AccessionNumber != null && NotOpened(SpecimensVM.SelectSpecimen(item.AccessionNumber)))
                        break;
                    SelectedTabIndex = 1;
                    break;
            }
        }

        // The specimen is selected before the tab is switched: when it cannot be shown, the user stays where they were
        // and is told why, instead of landing on another patient's specimen that the tab fell back to.
        public void OpenSpecimenReactions(string? accessionNumber)
        {
            if (accessionNumber != null && NotOpened(ReactionsVM.SelectSpecimen(accessionNumber))) return;
            SelectedTabIndex = 3;
        }

        public void OpenSpecimenAnalysis(string? accessionNumber)
        {
            if (accessionNumber != null && NotOpened(AnalysisVM.SelectSpecimen(accessionNumber))) return;
            SelectedTabIndex = 4;
        }

        public void OpenSpecimenReport(string? accessionNumber, string reportType = "Clinical Identification")
        {
            if (accessionNumber != null && NotOpened(ReportsVM.SelectSpecimen(accessionNumber, reportType))) return;
            SelectedTabIndex = 5;
        }

        /// <summary>Shows why a specimen could not be opened; true when <paramref name="problem"/> is set.</summary>
        private bool NotOpened(string? problem)
        {
            if (problem == null) return false;
            SetStatus(problem);
            System.Windows.MessageBox.Show(problem, "Specimen not opened",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return true;
        }

        public void OpenPanel(
            int panelId,
            string? cellNumber = null,
            string? antigen = null,
            IReadOnlyList<string>? searchAntigens = null)
        {
            SelectedTabIndex = 2;
            PanelsVM.SelectPanel(panelId, cellNumber, antigen, searchAntigens);
        }

        public void Dispose()
        {
            AppSettings.Changed -= OnSettingsChanged;
            Database?.Dispose();
        }
    }
}
