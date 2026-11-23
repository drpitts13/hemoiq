using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    public class WorklistViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly MainViewModel _main;
        private List<WorklistItem> _allItems = new();

        public ObservableCollection<WorklistItem> Items { get; } = new();

        private WorklistItem? _selectedItem;
        public WorklistItem? SelectedItem
        {
            get => _selectedItem;
            set => SetField(ref _selectedItem, value);
        }

        private int _incompleteCount;
        public int IncompleteCount
        {
            get => _incompleteCount;
            private set => SetField(ref _incompleteCount, value);
        }

        private int _staleCount;
        public int StaleCount
        {
            get => _staleCount;
            private set => SetField(ref _staleCount, value);
        }

        private int _expiringCount;
        public int ExpiringCount
        {
            get => _expiringCount;
            private set => SetField(ref _expiringCount, value);
        }

        private int _expiredCount;
        public int ExpiredCount
        {
            get => _expiredCount;
            private set => SetField(ref _expiredCount, value);
        }

        private int _reviewCount;
        /// <summary>Items in the Review category: every kind without its own card.</summary>
        public int ReviewCount
        {
            get => _reviewCount;
            private set => SetField(ref _reviewCount, value);
        }

        public int TotalCount => _allItems.Count;

        public bool ShowReview
        {
            get => AppSettings.Current.WorklistShowReview;
            set
            {
                if (AppSettings.Current.WorklistShowReview == value) return;
                AppSettings.Current.WorklistShowReview = value;
                OnPropertyChanged();
                PersistFilters();
            }
        }

        public bool ShowIncomplete
        {
            get => AppSettings.Current.WorklistShowIncomplete;
            set
            {
                if (AppSettings.Current.WorklistShowIncomplete == value) return;
                AppSettings.Current.WorklistShowIncomplete = value;
                OnPropertyChanged();
                PersistFilters();
            }
        }

        public bool ShowStale
        {
            get => AppSettings.Current.WorklistShowStale;
            set
            {
                if (AppSettings.Current.WorklistShowStale == value) return;
                AppSettings.Current.WorklistShowStale = value;
                OnPropertyChanged();
                PersistFilters();
            }
        }

        public bool ShowExpiring
        {
            get => AppSettings.Current.WorklistShowExpiring;
            set
            {
                if (AppSettings.Current.WorklistShowExpiring == value) return;
                AppSettings.Current.WorklistShowExpiring = value;
                OnPropertyChanged();
                PersistFilters();
            }
        }

        public bool ShowExpired
        {
            get => AppSettings.Current.WorklistShowExpired;
            set
            {
                if (AppSettings.Current.WorklistShowExpired == value) return;
                AppSettings.Current.WorklistShowExpired = value;
                OnPropertyChanged();
                PersistFilters();
            }
        }

        private string _listFilter = string.Empty;
        public string ListFilter
        {
            get => _listFilter;
            set { if (SetField(ref _listFilter, value)) ApplyFilter(); }
        }

        public bool IncompleteIsolated => AppSettings.Current.IsWorklistCategoryIsolated("Incomplete");
        public bool StaleIsolated => AppSettings.Current.IsWorklistCategoryIsolated("Stale");
        public bool ExpiringIsolated => AppSettings.Current.IsWorklistCategoryIsolated("Expiring");
        public bool ExpiredIsolated => AppSettings.Current.IsWorklistCategoryIsolated("Expired");
        public bool ReviewIsolated => AppSettings.Current.IsWorklistCategoryIsolated("Review");
        public string IsolatedKindLabel => AppSettings.Current.IsolatedWorklistLabel;
        public bool HasIsolatedKind => !string.IsNullOrEmpty(IsolatedKindLabel);

        public ICommand RefreshCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand ShowAllCommand { get; }
        public ICommand IsolateKindCommand { get; }
        public ICommand VerifyTypingCommand { get; }

        public WorklistViewModel(DatabaseService db, MainViewModel main)
        {
            _db = db;
            _main = main;
            RefreshCommand = new RelayCommand(Refresh);
            OpenCommand = new RelayCommand(OpenSelected, () => SelectedItem != null);
            ShowAllCommand = new RelayCommand(ShowAllCategories);
            IsolateKindCommand = new RelayCommand(IsolateOrRestoreKind);
            VerifyTypingCommand = new RelayCommand(VerifySelectedTyping,
                () => SelectedItem is { Kind: WorklistKind.ExtraAntigenReview, PanelId: not null, Antigen: not null });
            Refresh();
        }

        public void Refresh()
        {
            var title = SelectedItem?.Title;
            var kind = SelectedItem?.Kind;
            _allItems = _db.GetWorklistItems(AppSettings.Current.ExpirationWarningDays);
            IncompleteCount = _allItems.Count(i => i.Kind == WorklistKind.IncompleteReactions);
            StaleCount = _allItems.Count(i => i.Kind == WorklistKind.StaleAnalysis);
            ExpiringCount = _allItems.Count(i =>
                i.Kind is WorklistKind.ExpiringSpecimen or WorklistKind.ExpiringPanel);
            ExpiredCount = _allItems.Count(i =>
                i.Kind is WorklistKind.ExpiredSpecimen or WorklistKind.ExpiredPanel);
            ReviewCount = _allItems.Count(i => LabSettings.WorklistCategory(i.Kind) == "Review");
            OnPropertyChanged(nameof(TotalCount));
            NotifyFilterProperties();
            ApplyFilter(title, kind);
        }

        private void ApplyFilter(string? title = null, WorklistKind? kind = null)
        {
            title ??= SelectedItem?.Title;
            kind ??= SelectedItem?.Kind;
            var settings = AppSettings.Current;
            Items.Clear();
            foreach (var item in _allItems.Where(i =>
                         settings.ShowsWorklistKind(i.Kind) && i.MatchesFilter(_listFilter)))
                Items.Add(item);
            SelectedItem = Items.FirstOrDefault(i => i.Title == title && i.Kind == kind)
                ?? Items.FirstOrDefault();

            // A refresh while another screen is open must not replace that
            // screen's status. Opening Worklist announces on its own.
            if (_main.SelectedTabIndex == 0)
                AnnounceStatus();
        }

        private void VerifySelectedTyping()
        {
            if (SelectedItem is not { Kind: WorklistKind.ExtraAntigenReview, PanelId: int panelId, Antigen: string antigen }) return;
            // Before asking the user to check the vendor antigram: only a Supervisor or Administrator may verify.
            if (!RoleGate.AllowedOrExplained(_db, Permission.SupervisorReview, "Mark typing verified")) return;
            if (System.Windows.MessageBox.Show(
                    $"Confirm that {antigen} is negative on every cell of {SelectedItem.Title}, as printed on the vendor antigram?",
                    "Mark typing verified", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                _db.VerifyExtraAntigenTyping(panelId, antigen);
            }
            catch (System.Exception ex) when (ex is System.UnauthorizedAccessException or System.ArgumentException)
            {
                System.Windows.MessageBox.Show(ex.Message, "Mark typing verified",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            _main.SetStatus($"{antigen} typing on {SelectedItem.Title} marked verified.");
            Refresh();
        }

        public void AnnounceStatus() =>
            _main.SetStatus(ScreenStatus.Worklist(_allItems.Count, Items.Count));

        private void PersistFilters()
        {
            SettingsService.Save();
            ApplyFilter();
        }

        private void ShowAllCategories()
        {
            var s = AppSettings.Current;
            if (s.ShowsAllWorklistCategories)
            {
                ApplyFilter();
                return;
            }

            foreach (var c in LabSettings.WorklistCategories) s.SetWorklistCategory(c, true);
            NotifyFilterProperties();
            PersistFilters();
        }

        private void IsolateOrRestoreKind(object? parameter)
        {
            var key = parameter as string;
            if (string.IsNullOrEmpty(key)) return;
            AppSettings.Current.IsolateOrRestoreWorklistCategory(key);
            NotifyFilterProperties();
            PersistFilters();
        }

        private void NotifyFilterProperties()
        {
            OnPropertyChanged(nameof(ShowIncomplete));
            OnPropertyChanged(nameof(ShowStale));
            OnPropertyChanged(nameof(ShowExpiring));
            OnPropertyChanged(nameof(ShowExpired));
            OnPropertyChanged(nameof(ShowReview));
            OnPropertyChanged(nameof(ReviewIsolated));
            OnPropertyChanged(nameof(IncompleteIsolated));
            OnPropertyChanged(nameof(StaleIsolated));
            OnPropertyChanged(nameof(ExpiringIsolated));
            OnPropertyChanged(nameof(ExpiredIsolated));
            OnPropertyChanged(nameof(IsolatedKindLabel));
            OnPropertyChanged(nameof(HasIsolatedKind));
        }

        public void OpenItem(WorklistItem item)
        {
            SelectedItem = item;
            _main.NavigateToWorklistItem(item);
        }

        private void OpenSelected()
        {
            if (SelectedItem == null) return;
            OpenItem(SelectedItem);
        }
    }
}
