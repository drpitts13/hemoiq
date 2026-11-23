using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    public class ReactionsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly AntibodyAnalyzer _analyzer;
        private readonly MainViewModel _main;

        public ObservableCollection<Specimen> Specimens { get; } = new();
        public ObservableCollection<Panel> Panels { get; } = new();
        public ObservableCollection<PanelRun> Runs { get; } = new();
        public ObservableCollection<ReactionRow> Rows { get; } = new();
        public ObservableCollection<ReactionRow> VisibleRows { get; } = new();
        public ObservableCollection<CompareReactionRow> CompareRows { get; } = new();

        private bool _gradesDirty;
        public bool HasUnsavedGrades => _gradesDirty;
        /// <summary>Recovery file for unsaved grades; written on each edit, removed on save or discard.</summary>
        public ReactionDraftStore Drafts { get; } = new();
        /// <summary>Accession whose grades are entered but not saved, or null.</summary>
        public string? UnsavedAccession => _gradesDirty ? SelectedSpecimen?.AccessionNumber : null;
        /// <summary>"2026-002 (Panel A, Untreated)" while grades are unsaved, or null.</summary>
        public string? UnsavedGradesDescription => _gradesDirty && SelectedSpecimen != null
            ? $"{SelectedSpecimen.AccessionNumber} ({SelectedRun?.PanelName ?? SelectedPanel?.Name ?? "panel"}, {SelectedRun?.DisplayLabel ?? "run"})"
            : null;

        public void NotePaste(int count, string phase, string firstCell, string lastCell) =>
            _main.SetStatus($"Pasted {count} {phase} grade(s) into cells {firstCell}–{lastCell}. Save to keep them.");

        /// <summary>Saves the unsaved grades with the usual checks (expired lot, reason). False if not saved.</summary>
        public bool SaveUnsavedGrades()
        {
            if (!_gradesDirty) return true;
            if (!TrySaveGrades(out var entered)) return false;
            var msg = $"Saved grades for {entered} of {Rows.Count} cells.";
            SetSaveStatus(true, msg);
            _main.WorklistVM.Refresh();
            return true;
        }

        private Specimen? _selectedSpecimen;
        public Specimen? SelectedSpecimen
        {
            get => _selectedSpecimen;
            set
            {
                if (_selectedSpecimen?.AccessionNumber == value?.AccessionNumber)
                {
                    _selectedSpecimen = value;
                    return;
                }
                if (!ConfirmDiscardUnsaved())
                {
                    OnPropertyChanged();
                    return;
                }
                if (SetField(ref _selectedSpecimen, value))
                {
                    RefreshLockState();
                    RefreshPanels();
                    RefreshRuledOutAntigens();
                }
            }
        }

        private Panel? _selectedPanel;
        public Panel? SelectedPanel
        {
            get => _selectedPanel;
            set
            {
                if (_selectedPanel?.PanelId == value?.PanelId)
                {
                    _selectedPanel = value;
                    return;
                }
                if (!ConfirmDiscardUnsaved())
                {
                    OnPropertyChanged();
                    return;
                }
                if (SetField(ref _selectedPanel, value))
                {
                    RefreshExtraAntigens();
                    RefreshRuns();
                }
            }
        }

        private PanelRun? _selectedRun;
        public PanelRun? SelectedRun
        {
            get => _selectedRun;
            set
            {
                if (_selectedRun?.RunId == value?.RunId)
                {
                    _selectedRun = value;
                    return;
                }
                if (!ConfirmDiscardUnsaved())
                {
                    OnPropertyChanged();
                    return;
                }
                if (SetField(ref _selectedRun, value))
                {
                    UpdateTreatmentBanner();
                    RefreshCompareRunChoices();
                    LoadReactions();
                }
            }
        }

        private bool _compareMode;
        public bool CompareMode
        {
            get => _compareMode;
            set
            {
                if (SetField(ref _compareMode, value))
                {
                    OnPropertyChanged(nameof(ComparePanelVisibility));
                    RefreshCompareRunChoices();
                    RebuildCompareRows();
                }
            }
        }

        public Visibility ComparePanelVisibility =>
            CompareMode ? Visibility.Visible : Visibility.Collapsed;

        public IReadOnlyList<string> CompareExtraPhases { get; private set; } = Array.Empty<string>();

        private PanelRun? _compareRun;
        public PanelRun? CompareRun
        {
            get => _compareRun;
            set
            {
                if (SetField(ref _compareRun, value))
                    RebuildCompareRows();
            }
        }

        public ObservableCollection<PanelRun> CompareRunChoices { get; } = new();

        private void ResetCompareRows()
        {
            CompareRows.Clear();
            if (CompareExtraPhases.Count == 0) return;
            CompareExtraPhases = Array.Empty<string>();
            OnPropertyChanged(nameof(CompareExtraPhases));
        }

        public bool HideRuledOutAntigenColumns
        {
            get => AppSettings.Current.HideRuledOutAntigenColumns;
            set
            {
                if (AppSettings.Current.HideRuledOutAntigenColumns == value) return;
                AppSettings.Current.HideRuledOutAntigenColumns = value;
                SettingsService.Save();
                ApplyColumnVisibilitySettings();
            }
        }

        public bool ShowAntigramDosage
        {
            get => AppSettings.Current.ShowAntigramDosage;
            set
            {
                if (AppSettings.Current.ShowAntigramDosage == value) return;
                AppSettings.Current.ShowAntigramDosage = value;
                SettingsService.Save();
                RefreshExtraAntigens();
                OnPropertyChanged();
            }
        }

        private List<Specimen> _allSpecimens = new();
        private string _specimenFilter = string.Empty;
        public string SpecimenFilter
        {
            get => _specimenFilter;
            set { if (SetField(ref _specimenFilter, value)) ApplySpecimenFilter(); }
        }

        /// <summary>Antigen names ruled out for the current specimen (column header colouring).</summary>
        private HashSet<string> _ruledOutAntigens = new();
        public HashSet<string> RuledOutAntigens
        {
            get => _ruledOutAntigens;
            private set => SetField(ref _ruledOutAntigens, value);
        }

        /// <summary>Antigens destroyed by the currently selected run's cell treatment.</summary>
        private HashSet<string> _destroyedAntigens = new();
        public HashSet<string> DestroyedAntigens
        {
            get => _destroyedAntigens;
            private set => SetField(ref _destroyedAntigens, value);
        }

        /// <summary>Phases suppressed by the currently selected run's serum treatment.</summary>
        private HashSet<string> _nonInterpretablePhases = new();
        public HashSet<string> NonInterpretablePhases
        {
            get => _nonInterpretablePhases;
            private set => SetField(ref _nonInterpretablePhases, value);
        }

        private string _treatmentBannerText = string.Empty;
        public string TreatmentBannerText
        {
            get => _treatmentBannerText;
            private set
            {
                if (SetField(ref _treatmentBannerText, value))
                    OnPropertyChanged(nameof(TreatmentBannerVisibility));
            }
        }

        public Visibility TreatmentBannerVisibility =>
            string.IsNullOrEmpty(_treatmentBannerText) ? Visibility.Collapsed : Visibility.Visible;

        private IReadOnlyList<string> _extraAntigens = Array.Empty<string>();
        public IReadOnlyList<string> ExtraAntigens
        {
            get => _extraAntigens;
            private set => SetField(ref _extraAntigens, value);
        }

        private IReadOnlyList<string> _antigenDisplayOrder = Array.Empty<string>();
        public IReadOnlyList<string> AntigenDisplayOrder
        {
            get => _antigenDisplayOrder;
            private set
            {
                if (!SetField(ref _antigenDisplayOrder, value)) return;
                FilterAntigenChoices = new[] { AntigramRowFilter.AllCells }.Concat(_antigenDisplayOrder).ToList();
                if (AntigramRowFilter.IsSpecificAntigen(FilterAntigen)
                    && !_antigenDisplayOrder.Contains(FilterAntigen))
                    FilterAntigen = AntigramRowFilter.AllCells;
                else
                    ApplyVisibleRows();
            }
        }

        private IReadOnlyList<string> _filterAntigenChoices = new[] { AntigramRowFilter.AllCells };
        public IReadOnlyList<string> FilterAntigenChoices
        {
            get => _filterAntigenChoices;
            private set => SetField(ref _filterAntigenChoices, value);
        }

        public IReadOnlyList<string> FilterZygosityChoices => AntigenConstants.PositiveZygosityOptions;
        public IReadOnlyList<string> FilterSortChoices => AntigramRowFilter.SortOptions;

        private string _filterAntigen = AntigramRowFilter.AllCells;
        public string FilterAntigen
        {
            get => _filterAntigen;
            set
            {
                if (SetField(ref _filterAntigen, value ?? AntigramRowFilter.AllCells))
                {
                    OnPropertyChanged(nameof(FilterAntigenIsSpecific));
                    ApplyVisibleRows();
                }
            }
        }

        public bool FilterAntigenIsSpecific => AntigramRowFilter.IsSpecificAntigen(FilterAntigen);

        private string _filterZygosity = AntigenConstants.ZygosityBoth;
        public string FilterZygosity
        {
            get => _filterZygosity;
            set
            {
                if (SetField(ref _filterZygosity, value ?? AntigenConstants.ZygosityBoth))
                    ApplyVisibleRows();
            }
        }

        private string _filterSort = AntigramRowFilter.SortCellNumber;
        public string FilterSort
        {
            get => _filterSort;
            set
            {
                if (SetField(ref _filterSort, value ?? AntigramRowFilter.SortCellNumber))
                    ApplyVisibleRows();
            }
        }

        public string FilterExplanation { get; private set; } = "Showing all cells, sorted by cell number.";

        private void ApplyVisibleRows()
        {
            var source = Rows.Select(r => new AntigramFilterRow(r.CellNumber, r.AntigenValues, r.IsReactive)).ToList();
            var ordered = AntigramRowFilter.Apply(source, FilterAntigen, FilterZygosity, FilterSort);
            var byCell = Rows.ToDictionary(r => r.CellNumber, StringComparer.OrdinalIgnoreCase);
            VisibleRows.Clear();
            foreach (var item in ordered)
            {
                if (byCell.TryGetValue(item.CellNumber, out var row))
                    VisibleRows.Add(row);
            }
            FilterExplanation = AntigramRowFilter.Explain(ordered, Rows.Count, FilterAntigen, FilterZygosity, FilterSort);
            OnPropertyChanged(nameof(FilterExplanation));
        }

        private string _saveStatusMessage = string.Empty;
        public string SaveStatusMessage
        {
            get => _saveStatusMessage;
            private set
            {
                if (SetField(ref _saveStatusMessage, value))
                    OnPropertyChanged(nameof(SaveStatusVisibility));
            }
        }

        public Visibility SaveStatusVisibility =>
            string.IsNullOrEmpty(_saveStatusMessage) ? Visibility.Collapsed : Visibility.Visible;

        private bool _saveStatusIsSuccess;
        public bool SaveStatusIsSuccess
        {
            get => _saveStatusIsSuccess;
            private set => SetField(ref _saveStatusIsSuccess, value);
        }

        public string EntryProgressText
        {
            get
            {
                if (Rows.Count == 0) return string.Empty;
                var note = LockMessage != null ? $"Locked: {LockMessage} " : GradesReadOnly ? ViewOnlyNote : "";
                return note + FormatEntryProgress(Rows.Count(r => r.HasEnteredGrade), Rows.Count);
            }
        }

        public const string ViewOnlyNote = "View only: grades are shown but cannot be changed in this session. ";

        /// <summary>
        /// True when the signed-in session may not record results (Read-only role or unregistered login):
        /// the grid is read-only, grade keys and paste do nothing, and no recovery draft is written.
        /// </summary>
        public bool GradesReadOnly => !SessionAccess.CanRecord || LockMessage != null;

        private string? _lockMessage;
        /// <summary>
        /// Why the selected investigation cannot take grades (confirmed or cancelled), naming who can unlock it, or
        /// null. The grid is then read-only and the change buttons disabled, as in a view-only session, so grades are
        /// never typed only to be refused at Save.
        /// </summary>
        public string? LockMessage
        {
            get => _lockMessage;
            private set
            {
                if (!SetField(ref _lockMessage, value)) return;
                OnPropertyChanged(nameof(GradesReadOnly));
                OnPropertyChanged(nameof(IsLocked));
                RefreshEntryProgress();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsLocked => LockMessage != null;

        /// <summary>Re-reads whether the selected investigation is locked (another tab may have confirmed or cancelled it).</summary>
        public void RefreshLockState() => LockMessage = LockMessageFor(_db, SelectedSpecimen?.AccessionNumber);

        /// <summary>The locked-record message for an investigation (the same one a save would be refused with), or null.</summary>
        public static string? LockMessageFor(DatabaseService db, string? accessionNumber)
        {
            if (string.IsNullOrEmpty(accessionNumber)) return null;
            try
            {
                db.EnsureSpecimenUnlocked(accessionNumber);
                return null;
            }
            catch (RecordLockedException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>Called when the signed-in user or role may have changed.</summary>
        public void NotifySessionChanged()
        {
            RefreshLockState(); // the lock message names who can unlock, which depends on the role
            OnPropertyChanged(nameof(GradesReadOnly));
            RefreshEntryProgress();
        }

        public static string FormatEntryProgress(int entered, int total)
        {
            if (total <= 0) return string.Empty;
            if (entered >= total) return $"All {total} cells have grades.";
            return $"Grades entered: {entered} of {total} cells.";
        }

        private void RefreshEntryProgress() => OnPropertyChanged(nameof(EntryProgressText));

        private void OnGradeEdited()
        {
            RefreshEntryProgress();
            if (FilterSort == AntigramRowFilter.SortReactiveFirst
                || FilterSort == AntigramRowFilter.SortAntigenFirst)
                ApplyVisibleRows();
            if (!_restoringDraft) WriteDraft();
            if (_gradesDirty) return;
            _gradesDirty = true;
            OnPropertyChanged(nameof(HasUnsavedGrades));
        }

        private bool _restoringDraft;

        private void WriteDraft()
        {
            if (SelectedSpecimen == null || SelectedRun == null || GradesReadOnly) return;
            Drafts.Save(new ReactionDraft
            {
                SpecimenId = SelectedSpecimen.AccessionNumber,
                RunId = SelectedRun.RunId,
                PanelId = SelectedRun.PanelId,
                RunDescription = UnsavedGradesDescriptionFor(SelectedSpecimen.AccessionNumber),
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                Operator = AnalysisRuleTrace.ResolveOperator(AppSettings.Current),
                Rows = Rows.Select(r => new ReactionDraftRow
                {
                    Cell = r.CellNumber, IS = r.IS, C37 = r.C37, AHG = r.AHG, CC = r.CC,
                    Extra = r.ExtraPhaseValues.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                }).ToList(),
            });
        }

        private string UnsavedGradesDescriptionFor(string accession) =>
            $"{accession} ({SelectedRun?.PanelName ?? SelectedPanel?.Name ?? "panel"}, {SelectedRun?.DisplayLabel ?? "run"})";

        /// <summary>Removes the recovery file: the grades were saved or the user chose to discard them.</summary>
        public void DiscardDraft() => Drafts.Delete();

        /// <summary>
        /// Startup: a recovery file means the application did not close normally. Offers to put
        /// the recovered grades back on the grid, still unsaved, for review and Save.
        /// </summary>
        public void OfferDraftRecovery()
        {
            var draft = Drafts.Load();
            if (draft == null) { Drafts.Delete(); return; }
            var run = _db.GetPanelRun(draft.RunId);
            var specimen = _db.GetSpecimen(draft.SpecimenId);
            var changed = run == null ? new List<ReactionDraftRow>()
                : ReactionDraftStore.ChangedRows(draft, _db.GetReactions(draft.RunId));
            if (run == null || specimen == null || changed.Count == 0)
            {
                Drafts.Delete();
                return;
            }
            var when = DateTime.TryParse(draft.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var at)
                ? at.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : draft.SavedAtUtc;
            if (specimen.HasFinalCall)
            {
                MessageBox.Show(
                    $"HemoIQ did not close normally. Unsaved grades for {draft.RunDescription} (last edited {when}) were found, " +
                    "but the identification has since been confirmed, so they cannot be restored:\n\n" +
                    string.Join("\n", changed.Take(10).Select(DescribeDraftRow)) +
                    "\n\n" + AccessControl.NextStep(_db, Permission.ClearIdentification,
                        "Clear the identification first if these grades must be recorded.",
                        "clear the identification first if these grades must be recorded."),
                    "Recovered grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                Drafts.Delete();
                return;
            }
            var answer = MessageBox.Show(
                $"HemoIQ did not close normally. Unsaved grades for {changed.Count} cell(s) of {draft.RunDescription}, " +
                $"last edited {when}{(string.IsNullOrWhiteSpace(draft.Operator) ? "" : $" by {draft.Operator}")}, were recovered.\n\n" +
                "Yes — put them back on the Reactions tab. They stay unsaved until you review them and Save.\n" +
                "No — discard them.",
                "Recovered grades", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
            {
                Drafts.Delete();
                return;
            }

            _main.OpenSpecimenReactions(draft.SpecimenId);
            SelectedPanel = Panels.FirstOrDefault(p => p.PanelId == run.PanelId) ?? SelectedPanel;
            SelectedRun = Runs.FirstOrDefault(r => r.RunId == run.RunId) ?? SelectedRun;
            if (SelectedRun?.RunId != run.RunId)
            {
                MessageBox.Show($"The run for {draft.RunDescription} could not be opened, so the grades were not restored. " +
                                $"The recovery file is kept at {Drafts.FilePath}.",
                    "Recovered grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _restoringDraft = true;
            try
            {
                foreach (var d in changed)
                {
                    var row = Rows.FirstOrDefault(r => string.Equals(r.CellNumber, d.Cell, StringComparison.OrdinalIgnoreCase));
                    if (row == null) continue;
                    row.IS = d.IS; row.C37 = d.C37; row.AHG = d.AHG; row.CC = d.CC;
                    foreach (var (phase, value) in d.Extra)
                        if (row.Extra.TryGetValue(phase, out var g)) g.Value = value;
                }
            }
            finally { _restoringDraft = false; }
            WriteDraft();
            var msg = $"Restored unsaved grades for {changed.Count} cell(s). Review them, then Save.";
            _main.SetStatus(msg);
            SetSaveStatus(false, msg);
        }

        private static string DescribeDraftRow(ReactionDraftRow r) =>
            $"Cell {r.Cell}: IS {r.IS}, 37°C {r.C37}, AHG {r.AHG}, CC {r.CC}" +
            string.Concat(r.Extra.Select(kv => $", {kv.Key} {kv.Value}"));

        private void MarkGradesClean()
        {
            if (!_gradesDirty) return;
            _gradesDirty = false;
            OnPropertyChanged(nameof(HasUnsavedGrades));
        }

        public static bool NeedsDiscardPrompt(bool dirty, int? currentRunId, int? nextRunId) =>
            dirty && currentRunId != nextRunId;

        private bool ConfirmDiscardUnsaved()
        {
            if (!_gradesDirty) return true;
            // Discarding loses typed grades, so No is the answer under the keyboard.
            if (!DestructiveConfirm.Ask("Discard unsaved reaction grades?", "Unsaved grades")) return false;
            MarkGradesClean();
            Drafts.Delete();
            return true;
        }

        private void SetSaveStatus(bool success, string message)
        {
            SaveStatusIsSuccess = success;
            SaveStatusMessage = message;
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAnalyzeCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand FillNegativesCommand { get; }
        public ICommand AddRunCommand { get; }
        public ICommand DeleteRunCommand { get; }

        public static string[] ReactionValues => AntigenConstants.ReactionValues.ToArray();

        public ReactionsViewModel(DatabaseService db, AntibodyAnalyzer analyzer, MainViewModel main)
        {
            _db = db;
            _analyzer = analyzer;
            _main = main;

            LoadCommand = new RelayCommand(LoadReactions,
                () => SelectedSpecimen != null && SelectedRun != null);
            // Disabled in a view-only session and on a confirmed or cancelled investigation (the GradesReadOnly cases).
            SaveCommand = new RelayCommand(SaveGradesOnly,
                () => SessionAccess.CanRecord && !IsLocked && (SelectedSpecimen != null && SelectedRun != null && Rows.Count > 0));
            SaveAnalyzeCommand = new RelayCommand(SaveAndAnalyze,
                () => SessionAccess.CanRecord && !IsLocked && (SelectedSpecimen != null && SelectedRun != null && Rows.Count > 0));
            ClearCommand = new RelayCommand(ClearReactions,
                () => SessionAccess.CanRecord && !IsLocked && (SelectedSpecimen != null && SelectedRun != null));
            FillNegativesCommand = new RelayCommand(FillRemainingNegatives,
                () => SessionAccess.CanRecord && !IsLocked && (Rows.Count > 0));
            AddRunCommand = new RelayCommand(AddRun,
                () => SessionAccess.CanRecord && !IsLocked && (SelectedSpecimen != null && SelectedPanel != null));
            DeleteRunCommand = new RelayCommand(DeleteRun,
                () => SessionAccess.CanRecord && !IsLocked && (SelectedRun != null && !(SelectedRun?.IsUntreated ?? true)));

            RefreshSpecimens();
        }

        /// <summary>The grid's extra-phase columns: configured phases, then any other phase graded on this run.</summary>
        public List<string> GridExtraPhases =>
            ExtraPhaseParser.ColumnsFor(AppSettings.Current.ExtraPhases, Rows.Select(r => r.Extra.Keys));

        public void ApplyColumnVisibilitySettings()
        {
            OnPropertyChanged(nameof(HideRuledOutAntigenColumns));
            OnPropertyChanged(nameof(ShowAntigramDosage));
            OnPropertyChanged(nameof(RuledOutAntigens));
        }

        public void SelectSpecimen(string accessionNumber)
        {
            if (_allSpecimens.Count == 0)
                LoadAllSpecimens();
            var match = _allSpecimens.FirstOrDefault(x => x.AccessionNumber == accessionNumber);
            if (match != null && !match.MatchesFilter(_specimenFilter))
            {
                _specimenFilter = string.Empty;
                OnPropertyChanged(nameof(SpecimenFilter));
            }
            ApplySpecimenFilter(accessionNumber);
        }

        public void RefreshSpecimens()
        {
            var s = SelectedSpecimen?.AccessionNumber;
            LoadAllSpecimens();
            ApplySpecimenFilter(s);
        }

        private void RefreshPanels()
        {
            var pid = SelectedPanel?.PanelId;
            Panels.Clear();
            if (SelectedSpecimen == null) return;

            // Show only panels linked to this specimen
            foreach (var p in _db.GetSpecimenPanels(SelectedSpecimen.AccessionNumber))
                Panels.Add(p);

            SelectedPanel = Panels.FirstOrDefault(p => p.PanelId == pid)
                ?? Panels.FirstOrDefault();
        }

        private void RefreshRuns()
        {
            var rid = SelectedRun?.RunId;
            Runs.Clear();

            if (SelectedSpecimen == null || SelectedPanel == null)
            {
                SelectedRun = null;
                UpdateTreatmentBanner();
                return;
            }

            // Ensure the default (untreated) run exists
            _db.GetOrCreateDefaultRun(SelectedSpecimen.AccessionNumber, SelectedPanel.PanelId);

            foreach (var r in _db.GetPanelRuns(SelectedSpecimen.AccessionNumber, SelectedPanel.PanelId))
                Runs.Add(r);

            SelectedRun = Runs.FirstOrDefault(r => r.RunId == rid) ?? Runs.FirstOrDefault();
            UpdateTreatmentBanner();
            RefreshCompareRunChoices();
        }

        private void RefreshCompareRunChoices()
        {
            var keep = CompareRun?.RunId;
            CompareRunChoices.Clear();
            foreach (var r in Runs)
            {
                if (SelectedRun != null && r.RunId == SelectedRun.RunId) continue;
                CompareRunChoices.Add(r);
            }
            CompareRun = CompareRunChoices.FirstOrDefault(r => r.RunId == keep)
                ?? CompareRunChoices.FirstOrDefault();
        }

        private void RebuildCompareRows()
        {
            ResetCompareRows();
            if (!CompareMode || SelectedRun == null || CompareRun == null || Rows.Count == 0)
                return;

            var other = _db.GetReactions(CompareRun.RunId).ToDictionary(r => r.CellNumber);
            var extras = ExtraPhaseParser.Parse(AppSettings.Current.ExtraPhases);
            foreach (var row in Rows)
            {
                other.TryGetValue(row.CellNumber, out var rxn);
                CompareRows.Add(CompareReactionRow.Build(
                    row.CellNumber,
                    row.IS, row.C37, row.AHG, row.CC,
                    rxn?.IS ?? "NT", rxn?.C37 ?? "NT", rxn?.AHG ?? "NT", rxn?.CC ?? "NT",
                    row.ExtraPhaseValues,
                    rxn?.ExtraPhases,
                    extras,
                    SelectedRun.DisplayLabel,
                    CompareRun.DisplayLabel));
            }
            CompareExtraPhases = CompareRows
                .SelectMany(r => r.ExtraPhaseNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            OnPropertyChanged(nameof(CompareExtraPhases));
        }

        private void UpdateTreatmentBanner()
        {
            if (SelectedRun == null || SelectedRun.IsUntreated)
            {
                TreatmentBannerText = string.Empty;
                DestroyedAntigens = new HashSet<string>();
                NonInterpretablePhases = new HashSet<string>();
                return;
            }

            var parts = new List<string> { SelectedRun.DisplayLabel };

            var destroyed = VisibleAntigens
                .Where(ag => AntigenTreatmentEffects.IsAntigenDestroyedOnCell(
                    SelectedRun.CellTreatment, ag))
                .ToList();
            if (destroyed.Count > 0)
                parts.Add($"Destroyed antigens: {string.Join(", ", destroyed)}");

            var suppressedPhases = AntigenTreatmentEffects
                .GetNonInterpretablePhases(SelectedRun.SerumTreatment).ToList();
            if (suppressedPhases.Count > 0)
                parts.Add($"Non-interpretable phases: {string.Join(", ", suppressedPhases)}");

            TreatmentBannerText = string.Join("  |  ", parts);

            DestroyedAntigens = new HashSet<string>(destroyed);
            NonInterpretablePhases = new HashSet<string>(suppressedPhases);
        }

        public void RefreshRuledOutAntigens()
        {
            if (SelectedSpecimen == null) { RuledOutAntigens = new(); return; }

            var allReactions = _db.GetAllSpecimenReactions(SelectedSpecimen.AccessionNumber);
            var rules = _db.GetAllRules();
            var observed = new Dictionary<string, int>(StringComparer.Ordinal); // per antigen, case kept

            var byRun = new Dictionary<int, List<Reaction>>();
            foreach (var r in allReactions)
            {
                if (!byRun.ContainsKey(r.RunId)) byRun[r.RunId] = new();
                byRun[r.RunId].Add(r);
            }

            foreach (var (runId, runRxns) in byRun)
            {
                // We need the run's treatment to gate rule-outs
                var run = _db.GetPanelRun(runId);
                if (run == null) continue;
                var ctx = CreateRunContext(run);

                var cellDict = _db.GetPanelCells(run.PanelId).ToDictionary(c => c.CellNumber);
                var antigens = AntigenConstants.GetAnalyzedAntigens(ctx.ExtraAntigens);
                foreach (var rxn in runRxns)
                {
                    if (rxn.CellNumber == "AC" || !ctx.IsNegative(rxn)) continue;
                    if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;

                    foreach (var ag in antigens)
                    {
                        if (!ctx.TypesAntigen(ag)) continue;
                        if (cell.GetAntigen(ag) != "+") continue;
                        if (!ctx.CanContributeRuleout(ag, cell)) continue;
                        if (!CanRuleOutAntigen(ag, cell, rules)) continue;
                        observed.TryGetValue(ag, out var count);
                        observed[ag] = count + 1;
                    }
                }
            }

            var labDefault = AppSettings.Current.DefaultMinRuleoutCount;
            var result = new HashSet<string>();
            foreach (var (ag, count) in observed)
            {
                var required = RuleoutPolicy.ResolveRequiredCount($"anti-{ag}", rules, labDefault);
                if (count >= required)
                    result.Add(ag);
            }
            RuledOutAntigens = result;
        }

        private static bool CanRuleOutAntigen(string antigen, PanelCell cell, List<Rule> rules)
        {
            if (!AntigenConstants.AntitheticalPairs.ContainsKey(antigen))
                return true;
            if (cell.IsHomozygousFor(antigen)) return true;
            return RuleAllowsHeterozygous(antigen, rules);
        }

        internal static bool RuleAllowsHeterozygous(string antigen, IReadOnlyList<Rule> rules) =>
            RuleoutPolicy.HeterozygousAllowed(antigen, rules);

        private void RefreshExtraAntigens()
        {
            if (SelectedPanel == null)
            {
                ExtraAntigens = Array.Empty<string>();
                AntigenDisplayOrder = Array.Empty<string>();
                return;
            }
            ExtraAntigens = _db.GetPanelExtraAntigens(SelectedPanel.PanelId);
            var order = _db.GetPanelDisplayAntigens(SelectedPanel.PanelId);
            var cells = _db.GetPanelCells(SelectedPanel.PanelId);
            AntigenDisplayOrder = AntigensForReactions(order, cells, ShowAntigramDosage);
        }

        /// <summary>
        /// Antigen columns on the reactions grid. A column that is NT or blank
        /// on every cell is omitted. A typed minus keeps the column. Dosage
        /// grouping still applies to the columns that remain.
        /// </summary>
        public static IReadOnlyList<string> AntigensForReactions(
            IReadOnlyList<string> order,
            IReadOnlyList<PanelCell> cells,
            bool groupBySystem)
        {
            var typed = PanelCell.AntigensTypedOnAnyCell(order, cells);
            return groupBySystem ? AntigramDisplay.GroupBySystem(typed) : typed;
        }

        private IReadOnlyList<string> VisibleAntigens =>
            AntigenConstants.GetAnalyzedAntigens(ExtraAntigens);

        private RunContext CreateRunContext(PanelRun run) =>
            new(run, _db.GetPanelExtraAntigens(run.PanelId), _db.GetPanelTypedAntigens(run.PanelId));

        private void LoadAllSpecimens()
        {
            _allSpecimens = _db.GetActiveSpecimens();
        }

        private void ApplySpecimenFilter(string? preferredAccession = null)
        {
            preferredAccession ??= SelectedSpecimen?.AccessionNumber;
            Specimens.Clear();
            foreach (var sp in _allSpecimens.Where(s => s.MatchesFilter(_specimenFilter)))
                Specimens.Add(sp);
            SelectedSpecimen = Specimens.FirstOrDefault(x => x.AccessionNumber == preferredAccession)
                ?? Specimens.FirstOrDefault();
        }

        private void LoadReactions()
        {
            if (SelectedSpecimen == null || SelectedRun == null)
            {
                Rows.Clear();
                ResetCompareRows();
                ApplyVisibleRows();
                RefreshEntryProgress();
                MarkGradesClean();
                return;
            }

            if (SelectedSpecimen.IsActive == false || SelectedPanel?.IsActive == false)
            {
                var inactiveItem = SelectedSpecimen.IsActive == false ? "specimen" : "panel";
                MessageBox.Show(
                    $"Cannot load reactions: the selected {inactiveItem} is inactive. " +
                    "Reactivate it first if you need to work with its reactions.",
                    "Inactive Item", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveStatusMessage = string.Empty;
            Rows.Clear();

            var cells = _db.GetPanelCells(SelectedRun.PanelId);
            var reactions = _db.GetReactions(SelectedRun.RunId)
                .ToDictionary(r => r.CellNumber);
            var rules = _db.GetAllRules();
            var ctx = CreateRunContext(SelectedRun);

            foreach (var cell in cells)
            {
                reactions.TryGetValue(cell.CellNumber, out var rxn);
                Rows.Add(new ReactionRow(cell, rxn, rules, ctx, OnGradeEdited));
            }

            OnPropertyChanged(nameof(GridExtraPhases)); // a stored phase no longer in Preferences gets its column back
            UpdateTreatmentBanner();
            RefreshRuledOutAntigens();
            RebuildCompareRows();
            ApplyVisibleRows();
            RefreshEntryProgress();
            MarkGradesClean();
            if (_main.SelectedTabIndex == 3)
                AnnounceStatus();
        }

        public void AnnounceStatus() =>
            _main.SetStatus(ScreenStatus.Reactions(
                SelectedSpecimen?.AccessionNumber,
                SelectedRun?.PanelName,
                SelectedRun?.DisplayLabel,
                Rows.Count));

        private void SaveGradesOnly()
        {
            if (!TrySaveGrades(out var entered)) return;
            var msg = $"Saved grades for {entered} of {Rows.Count} cells.";
            _main.SetStatus(msg);
            SetSaveStatus(true, msg);
            _main.WorklistVM.Refresh();
        }

        /// <summary>The save prompt for a specimen past its expiration date, or null when it is in date.</summary>
        public static string? ExpiredSpecimenPrompt(Specimen? specimen, DateTime today) =>
            specimen != null && ExpirationStatus.IsExpired(specimen.ExpirationDate, today)
                ? $"Specimen {specimen.AccessionNumber} expired {specimen.ExpirationDate}. Results on an expired specimen may not " +
                  "represent the patient's current antibodies (for example after a recent transfusion or pregnancy). " +
                  "Collect a new specimen if testing must continue.\n\nRecord these reactions anyway?"
                : null;

        private bool TrySaveGrades(out int entered)
        {
            entered = 0;
            if (SelectedSpecimen == null || SelectedRun == null) return false;

            if (SelectedSpecimen.IsActive == false || SelectedPanel?.IsActive == false)
            {
                var inactiveItem = SelectedSpecimen.IsActive == false ? "specimen" : "panel";
                MessageBox.Show(
                    $"Cannot save reactions: the selected {inactiveItem} is inactive. " +
                    "Reactivate it first if you need to work with its reactions.",
                    "Inactive Item", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // A specimen that expired while the application was open is still active until the next start-up
            // deactivates it; warn the same way as for an expired lot rather than saving silently.
            if (Rows.Any(r => r.HasEnteredGrade) && ExpiredSpecimenPrompt(SelectedSpecimen, DateTime.Today) is string specimenPrompt &&
                !OverrideConfirm.Ask(specimenPrompt, "Expired specimen"))
            {
                const string notSaved = "Not saved: the specimen has expired.";
                _main.SetStatus(notSaved);
                SetSaveStatus(false, notSaved);
                return false;
            }

            if (SelectedPanel?.IsExpired == true && Rows.Any(r => r.HasEnteredGrade) &&
                !OverrideConfirm.Ask(
                    $"Panel lot {SelectedPanel.LotNumber} expired {SelectedPanel.ExpirationDate}. Results from expired reagent " +
                    "red cells will be flagged for review in analysis and on the Worklist.\n\nRecord these reactions anyway?",
                    "Expired panel"))
            {
                const string notSaved = "Not saved: the panel lot has expired.";
                _main.SetStatus(notSaved);
                SetSaveStatus(false, notSaved);
                return false;
            }

            try
            {
                var inputs = Rows.Select(r => new ReactionInput(r.CellNumber, r.IS, r.C37, r.AHG, r.CC, r.ExtraPhaseValues))
                    .ToList();
                string? reason = null;
                if (SelectedSpecimen.LastAnalyzedAt != null || _db.GetSpecimen(SelectedSpecimen.AccessionNumber)?.LastAnalyzedAt != null)
                {
                    var changes = _db.DescribeReactionChanges(SelectedRun.RunId, inputs);
                    if (changes.Count > 0)
                    {
                        var dlg = new Views.Dialogs.ReasonDialog(
                            "These recorded results were already analyzed and will be changed:\n\n" +
                            string.Join("\n", changes.Take(12)) + (changes.Count > 12 ? $"\n… and {changes.Count - 12} more" : "") +
                            "\n\nEnter the reason for the change. It is saved in the audit log and shown on the timeline.")
                        {
                            Owner = Application.Current?.MainWindow,
                            Title = "Reason for changing results",
                        };
                        if (dlg.ShowDialog() != true)
                        {
                            const string cancelled = "Not saved: no reason was given for changing recorded results.";
                            _main.SetStatus(cancelled);
                            SetSaveStatus(false, cancelled);
                            return false;
                        }
                        reason = dlg.Reason;
                    }
                }
                _db.SaveReactions(SelectedRun.RunId, inputs, reason);
                entered = Rows.Count(r => r.HasEnteredGrade);
                MarkGradesClean();
                Drafts.Delete();
                return true;
            }
            catch (Exception ex)
            {
                var msg = SaveFailedText(ex.Message);
                _main.SetStatus(msg);
                SetSaveStatus(false, msg);
                return false;
            }
        }

        /// <summary>A failed save writes nothing (it is one transaction); the grades stay on screen to correct or retry.</summary>
        public static string SaveFailedText(string reason) =>
            $"Not saved: {reason} Nothing was written; the grades are still on screen.";

        /// <summary>
        /// Save & Analyze saves first: when the analysis then fails, the grades are already saved, and the message
        /// must say so rather than read as if nothing was kept.
        /// </summary>
        public static string AnalyzeAfterSaveFailedText(string reason) =>
            $"Grades saved, but analysis did not run: {reason} Run Analysis again on the Analysis tab.";

        private void SaveAndAnalyze()
        {
            if (!TrySaveGrades(out _)) return;

            try
            {
                _main.SetStatus("Reactions saved. Running analysis...");
                var result = _analyzer.AnalyzeSpecimen(SelectedSpecimen!.AccessionNumber);
                RefreshRuledOutAntigens();
                var msg = $"Analysis complete — {result.Suspected.Count} suspected, " +
                          $"{result.RuledOut.Count} ruled out.";
                _main.SetStatus(msg);
                _main.SpecimensVM.Refresh();
                _main.WorklistVM.Refresh();
                RebuildCompareRows();
                SetSaveStatus(true, msg);
            }
            catch (Exception ex)
            {
                var msg = AnalyzeAfterSaveFailedText(ex.Message);
                _main.SetStatus(msg);
                SetSaveStatus(false, msg);
            }
        }

        private void FillRemainingNegatives()
        {
            if (Rows.Count == 0) return;
            int changed = 0;
            foreach (var row in Rows)
            {
                if (row.FillRemainingNegatives())
                    changed++;
            }
            RebuildCompareRows();
            ApplyVisibleRows();
            RefreshEntryProgress();
            if (changed == 0)
            {
                _main.SetStatus("No NT phases left to fill.");
                return;
            }
            var msg = $"Filled remaining NT phases as negative on {changed} cell(s). Save to keep.";
            _main.SetStatus(msg);
            SetSaveStatus(true, msg);
        }

        /// <summary>
        /// Confirms a deletion of recorded results. After analysis, when graded cells would
        /// be deleted, asks for a reason instead of a Yes/No. Returns false when cancelled.
        /// </summary>
        private bool TryGetDeletionReason(int runId, string question, string title, out string? reason)
        {
            reason = null;
            var graded = _db.CountGradedReactions(runId);
            var analyzed = SelectedSpecimen != null &&
                           _db.GetSpecimen(SelectedSpecimen.AccessionNumber)?.LastAnalyzedAt != null;
            if (graded > 0 && analyzed)
            {
                var dlg = new Views.Dialogs.ReasonDialog(
                    $"{question}\n\n{graded} graded cell(s) on this run were already analyzed. The deleted grades are kept " +
                    "in the audit log. Enter the reason for deleting them.")
                {
                    Owner = Application.Current?.MainWindow,
                    Title = title,
                };
                if (dlg.ShowDialog() != true) return false;
                reason = dlg.Reason;
                return true;
            }
            return DestructiveConfirm.Ask(question, "Confirm");
        }

        private void ClearReactions()
        {
            if (SelectedSpecimen == null || SelectedRun == null) return;
            if (!TryGetDeletionReason(SelectedRun.RunId, "Clear all reactions for this run?",
                    "Clear reactions", out var reason)) return;
            try
            {
                _db.DeleteReactions(SelectedRun.RunId, reason);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Reactions not cleared", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaveStatusMessage = string.Empty;
            Rows.Clear();
            ResetCompareRows();
            ApplyVisibleRows();
            RefreshRuledOutAntigens();
            RefreshEntryProgress();
            MarkGradesClean();
            _main.SetStatus("Reactions cleared.");
        }

        private void AddRun()
        {
            if (SelectedSpecimen == null || SelectedPanel == null) return;
            var dlg = new Views.Dialogs.PanelRunDialog { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            // Copying is offered only for an untreated repeat (RunTreatmentText.CanCopyGrades), and the copied grades
            // are never saved as results: they pre-fill the new run's grid as unsaved values to check against the test.
            var copyGrades = dlg.CopyGradesFromCurrentRun
                && Services.RunTreatmentText.CanCopyGrades(dlg.SelectedCellTreatment, dlg.SelectedSerumTreatment);
            var prefill = copyGrades
                ? Rows.Select(r => (r.CellNumber, r.IS, r.C37, r.AHG, r.CC, Extra: r.ExtraPhaseValues)).ToList()
                : new List<(string CellNumber, string IS, string C37, string AHG, string CC, IReadOnlyDictionary<string, string> Extra)>();

            try
            {
                var runId = _db.AddPanelRun(
                    SelectedSpecimen.AccessionNumber, SelectedPanel.PanelId,
                    dlg.SelectedCellTreatment, dlg.SelectedSerumTreatment,
                    dlg.RunLabel);

                RefreshRuns();
                SelectedRun = Runs.FirstOrDefault(r => r.RunId == runId);
                LoadReactions();
                var copied = PrefillUnsaved(prefill);
                _main.SetStatus(copied > 0
                    ? $"Added run: {SelectedRun?.DisplayLabel}. {copied} cell(s) pre-filled from the previous run, NOT saved: " +
                      "check each against this test, then Save."
                    : $"Added run: {SelectedRun?.DisplayLabel}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add run:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Puts grades into the grid as unsaved edits (no database write). Returns the cells filled.</summary>
        private int PrefillUnsaved(
            IEnumerable<(string CellNumber, string IS, string C37, string AHG, string CC, IReadOnlyDictionary<string, string> Extra)> grades)
        {
            var filled = 0;
            foreach (var g in grades)
            {
                var row = Rows.FirstOrDefault(r => string.Equals(r.CellNumber, g.CellNumber, StringComparison.OrdinalIgnoreCase));
                if (row == null) continue;
                var any = new[] { g.IS, g.C37, g.AHG, g.CC }.Concat(g.Extra.Values)
                    .Any(v => ReactionGrade.Normalize(v) != ReactionGrade.NotTested);
                if (!any) continue;
                row.IS = g.IS; row.C37 = g.C37; row.AHG = g.AHG; row.CC = g.CC;
                foreach (var kv in g.Extra)
                    if (row.Extra.TryGetValue(kv.Key, out var bindable)) bindable.Value = kv.Value;
                filled++;
            }
            return filled;
        }

        private void DeleteRun()
        {
            if (SelectedRun == null || SelectedRun.IsUntreated)
            {
                MessageBox.Show("The untreated (default) run cannot be deleted.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!TryGetDeletionReason(SelectedRun.RunId, $"Delete run '{SelectedRun.DisplayLabel}' and all its reactions?",
                    "Delete run", out var reason)) return;
            try
            {
                _db.DeletePanelRun(SelectedRun.RunId, reason);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Run not deleted", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Rows.Clear();
            RefreshRuns();
            _main.SetStatus("Run deleted.");
        }
    }

    public class ReactionRow : BaseViewModel
    {
        private readonly IReadOnlyDictionary<string, string> _antigens;
        private readonly IReadOnlyList<Rule> _rules;
        private readonly RunContext _ctx;
        private readonly Action? _onGradeChanged;

        public string CellNumber { get; }
        public IReadOnlyDictionary<string, string> AntigenValues => _antigens;

        public string DisplayAntigen(string antigen) =>
            AntigramDisplay.Format(_antigens, antigen, AppSettings.Current.ShowAntigramDosage);

        public string AntigenNote(string antigen) =>
            AntigramDisplay.Explain(CellNumber, _antigens, antigen);

        private string _IS;
        public string IS
        {
            get => _IS;
            set { if (SetField(ref _IS, value)) NotifyRuleout(); }
        }

        private string _C37;
        public string C37
        {
            get => _C37;
            set { if (SetField(ref _C37, value)) NotifyRuleout(); }
        }

        private string _AHG;
        public string AHG
        {
            get => _AHG;
            set { if (SetField(ref _AHG, value)) NotifyRuleout(); }
        }

        private string _CC;
        public string CC
        {
            get => _CC;
            set { if (SetField(ref _CC, value)) NotifyRuleout(); }
        }

        /// <summary>
        /// True when all interpretable phases are non-reactive.
        /// CC is a check-cell control, not a reactivity phase.
        /// </summary>
        public Dictionary<string, BindableGrade> Extra { get; } = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> ExtraPhaseValues =>
            Extra.ToDictionary(kv => kv.Key, kv => kv.Value.Value, StringComparer.OrdinalIgnoreCase);

        public bool IsNegative =>
            ReactionGrade.IsNegative(AHG) && ReactionGrade.IsAbsent(IS) && ReactionGrade.IsAbsent(C37)
            && Extra.Values.All(g => ReactionGrade.IsAbsent(g.Value));

        public bool IsReactive =>
            ReactionGrade.IsPositive(IS) || ReactionGrade.IsPositive(C37) || ReactionGrade.IsPositive(AHG)
            || Extra.Values.Any(g => ReactionGrade.IsPositive(g.Value));

        /// <summary>
        /// Antigen names on this row whose + boxes should show a rule-out slash.
        /// Empty unless the cell is negative (AC is never slashed).
        /// </summary>
        public IReadOnlyList<string> SlashedAntigens
        {
            get
            {
                if (CellNumber == "AC" || !IsNegative) return Array.Empty<string>();
                var list = new List<string>();
                foreach (var ag in AntigenConstants.GetAnalyzedAntigens(
                             _antigens.Keys.Where(AntigenConstants.IsWarehouse)))
                {
                    if (!_antigens.TryGetValue(ag, out var v) || v != "+") continue;
                    if (_ctx.GetAntigenEffect(ag) == AntigenEffect.Destroyed) continue;
                    if (CanRuleOut(ag)) list.Add(ag);
                }
                return list;
            }
        }

        public string RuledOutNote =>
            SlashedAntigens.Count == 0
                ? string.Empty
                : string.Join(", ", SlashedAntigens.Select(ag => $"anti-{ag}"));

        public bool HasRuleout => SlashedAntigens.Count > 0;

        public bool HasEnteredGrade =>
            IsGradeEntered(IS) || IsGradeEntered(C37) || IsGradeEntered(AHG)
            || Extra.Values.Any(g => IsGradeEntered(g.Value));

        public bool IsIncomplete => !HasEnteredGrade;

        public static bool IsGradeEntered(string? v) =>
            !string.IsNullOrEmpty(v) && v != "NT";

        public static (string IS, string C37, string AHG, string CC) FillNegativeDefaults(
            string? isPhase, string? c37, string? ahg, string? cc)
        {
            var nis = IsGradeEntered(isPhase) ? isPhase! : "0";
            var nc37 = IsGradeEntered(c37) ? c37! : "0";
            var nahg = IsGradeEntered(ahg) ? ahg! : "0";
            var ncc = IsGradeEntered(cc) ? cc! : (ReactionGrade.IsNegative(nahg) ? "2+" : "NT");
            return (nis, nc37, nahg, ncc);
        }

        public bool FillRemainingNegatives()
        {
            var filled = FillNegativeDefaults(IS, C37, AHG, CC);
            var changed = filled.IS != IS || filled.C37 != C37 || filled.AHG != AHG || filled.CC != CC;
            IS = filled.IS;
            C37 = filled.C37;
            AHG = filled.AHG;
            CC = filled.CC;
            foreach (var grade in Extra.Values)
            {
                if (!IsGradeEntered(grade.Value))
                {
                    grade.Value = "0";
                    changed = true;
                }
            }
            return changed;
        }

        public bool IsCcInvalid =>
            ReactionGrade.IsNegative(AHG) && !ReactionGrade.IsPositive(CC);

        public ReactionRow(PanelCell cell, Reaction? existing, IReadOnlyList<Rule> rules, RunContext ctx,
            Action? onGradeChanged = null)
        {
            CellNumber = cell.CellNumber;
            _antigens = cell.Antigens;
            _rules = rules;
            _ctx = ctx;
            _onGradeChanged = onGradeChanged;
            _IS  = existing?.IS  ?? "NT";
            _C37 = existing?.C37 ?? "NT";
            _AHG = existing?.AHG ?? "NT";
            _CC  = existing?.CC  ?? "NT";
            foreach (var phase in ExtraPhaseParser.Parse(AppSettings.Current.ExtraPhases))
            {
                var stored = "NT";
                existing?.ExtraPhases.TryGetValue(phase, out stored);
                Extra[phase] = new BindableGrade(stored ?? "NT", NotifyRuleout);
            }
            if (existing != null)
            {
                foreach (var (phase, value) in existing.ExtraPhases)
                {
                    if (Extra.ContainsKey(phase)) continue;
                    Extra[phase] = new BindableGrade(value, NotifyRuleout);
                }
            }
        }

        private void NotifyRuleout()
        {
            OnPropertyChanged(nameof(IsNegative));
            OnPropertyChanged(nameof(SlashedAntigens));
            OnPropertyChanged(nameof(RuledOutNote));
            OnPropertyChanged(nameof(HasRuleout));
            OnPropertyChanged(nameof(IsCcInvalid));
            OnPropertyChanged(nameof(HasEnteredGrade));
            OnPropertyChanged(nameof(IsIncomplete));
            _onGradeChanged?.Invoke();
        }

        private bool CanRuleOut(string antigen)
        {
            if (!AntigenConstants.AntitheticalPairs.TryGetValue(antigen, out var antithetical))
                return true;
            if (!_antigens.ContainsKey(antithetical))
                return false;
            var antitheticalVal = _antigens.TryGetValue(antithetical, out var av) ? av : "-";
            if (antitheticalVal == "-") return true;
            return ReactionsViewModel.RuleAllowsHeterozygous(antigen, _rules);
        }

    }

    public class BindableGrade : BaseViewModel
    {
        private string _value;
        private readonly Action? _changed;

        public BindableGrade(string value, Action? changed = null)
        {
            _value = value;
            _changed = changed;
        }

        public string Value
        {
            get => _value;
            set
            {
                if (SetField(ref _value, value))
                    _changed?.Invoke();
            }
        }
    }

    public class CompareReactionRow
    {
        public string CellNumber { get; set; } = string.Empty;
        public string LeftLabel { get; set; } = string.Empty;
        public string RightLabel { get; set; } = string.Empty;
        public string LeftIS { get; set; } = "NT";
        public string LeftC37 { get; set; } = "NT";
        public string LeftAHG { get; set; } = "NT";
        public string LeftCC { get; set; } = "NT";
        public string RightIS { get; set; } = "NT";
        public string RightC37 { get; set; } = "NT";
        public string RightAHG { get; set; } = "NT";
        public string RightCC { get; set; } = "NT";
        public List<string> ExtraPhaseNames { get; } = new();
        public Dictionary<string, string> LeftExtra { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> RightExtra { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool AhgChanged => !SameGrade(LeftAHG, RightAHG);
        public bool AnyChanged => Diffs().Count > 0;
        public string Explanation
        {
            get
            {
                var diffs = Diffs();
                if (diffs.Count == 0)
                    return $"Cell {CellNumber} matches on all compared phases.";
                return $"Cell {CellNumber} differs: {string.Join("; ", diffs)}.";
            }
        }

        public string GetLeftExtra(string phase) => GradeOrNt(LeftExtra, phase);
        public string GetRightExtra(string phase) => GradeOrNt(RightExtra, phase);
        public bool ExtraChanged(string phase) => !SameGrade(GetLeftExtra(phase), GetRightExtra(phase));

        public static CompareReactionRow Build(
            string cellNumber,
            string leftIS, string leftC37, string leftAHG, string leftCC,
            string rightIS, string rightC37, string rightAHG, string rightCC,
            IReadOnlyDictionary<string, string>? leftExtra = null,
            IReadOnlyDictionary<string, string>? rightExtra = null,
            IEnumerable<string>? extraPhases = null,
            string? leftLabel = null,
            string? rightLabel = null)
        {
            var row = new CompareReactionRow
            {
                CellNumber = cellNumber,
                LeftIS = leftIS ?? "NT",
                LeftC37 = leftC37 ?? "NT",
                LeftAHG = leftAHG ?? "NT",
                LeftCC = leftCC ?? "NT",
                RightIS = rightIS ?? "NT",
                RightC37 = rightC37 ?? "NT",
                RightAHG = rightAHG ?? "NT",
                RightCC = rightCC ?? "NT",
                LeftLabel = leftLabel ?? "",
                RightLabel = rightLabel ?? "",
            };
            foreach (var name in UnionExtraPhases(extraPhases, leftExtra, rightExtra))
            {
                row.ExtraPhaseNames.Add(name);
                row.LeftExtra[name] = GradeOrNt(leftExtra, name);
                row.RightExtra[name] = GradeOrNt(rightExtra, name);
            }
            return row;
        }

        private List<string> Diffs()
        {
            var diffs = new List<string>();
            AddDiff(diffs, "IS", LeftIS, RightIS);
            AddDiff(diffs, "37°C", LeftC37, RightC37);
            AddDiff(diffs, "AHG", LeftAHG, RightAHG);
            foreach (var phase in ExtraPhaseNames)
                AddDiff(diffs, phase, GetLeftExtra(phase), GetRightExtra(phase));
            AddDiff(diffs, "CC", LeftCC, RightCC);
            return diffs;
        }

        public static string RunName(string? label, string fallback) =>
            string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();

        public static string GradeHeader(string? runLabel, string phase, string fallback) =>
            $"{RunName(runLabel, fallback)} {phase}";

        private void AddDiff(List<string> diffs, string phase, string left, string right)
        {
            if (SameGrade(left, right)) return;
            diffs.Add($"{phase} ({RunName(LeftLabel, "this")} {left} vs {RunName(RightLabel, "other")} {right})");
        }

        private static bool SameGrade(string? left, string? right) =>
            string.Equals(string.IsNullOrWhiteSpace(left) ? "NT" : left,
                string.IsNullOrWhiteSpace(right) ? "NT" : right,
                StringComparison.OrdinalIgnoreCase);

        private static string GradeOrNt(IReadOnlyDictionary<string, string>? extras, string phase)
        {
            if (extras != null && extras.TryGetValue(phase, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
            return "NT";
        }

        private static List<string> UnionExtraPhases(
            IEnumerable<string>? configured,
            IReadOnlyDictionary<string, string>? leftExtra,
            IReadOnlyDictionary<string, string>? rightExtra)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in (configured ?? Array.Empty<string>())
                .Concat(leftExtra?.Keys ?? Array.Empty<string>())
                .Concat(rightExtra?.Keys ?? Array.Empty<string>()))
            {
                if (string.IsNullOrWhiteSpace(name) || ExtraPhaseParser.IsReserved(name)) continue;
                if (!seen.Add(name)) continue;
                names.Add(name.Trim());
            }
            return names;
        }
    }
}
