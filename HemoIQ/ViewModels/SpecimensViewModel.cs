using HemoIQ.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.ViewModels
{
    public class SpecimensViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly MainViewModel _main;

        public ObservableCollection<Specimen> Specimens { get; } = new();
        public ObservableCollection<Panel> AllPanels { get; } = new();
        public ObservableCollection<Panel> LinkedPanels { get; } = new();
        public ObservableCollection<SpecimenAntibody> Antibodies { get; } = new();
        public ObservableCollection<SpecimenRuleout> Ruleouts { get; } = new();

        private Specimen? _selectedSpecimen;
        /// <summary>
        /// Why the selected investigation cannot be edited or have panels attached or detached (it is confirmed or
        /// cancelled), naming who can unlock it, or null. Edit, Attach and Detach are disabled with this as their
        /// tooltip, rather than refusing after the dialog has been filled in or a panel chosen.
        /// </summary>
        public string? InvestigationLock =>
            SelectedSpecimen is { } s && (s.HasFinalCall || s.IsCancelled)
                ? ReactionsViewModel.LockMessageFor(_db, s.AccessionNumber)
                : null;

        /// <summary>Why Delete is unavailable for the selected specimen (a confirmed identification), or null.</summary>
        public string? DeleteLock =>
            SelectedSpecimen is { HasFinalCall: true } s ? _db.DeleteRefusal(s) : null;

        public Specimen? SelectedSpecimen
        {
            get => _selectedSpecimen;
            set
            {
                var previous = _selectedSpecimen?.AccessionNumber;
                if (!SetField(ref _selectedSpecimen, value)) return;
                OnPropertyChanged(nameof(InvestigationLock));
                OnPropertyChanged(nameof(DeleteLock));
                var failed = LoadSpecimenDetails();
                if (!failed
                    && _main.SelectedTabIndex == 1
                    && !string.Equals(previous, _selectedSpecimen?.AccessionNumber, StringComparison.Ordinal))
                    AnnounceStatus();
            }
        }

        private Panel? _selectedLinkedPanel;
        public Panel? SelectedLinkedPanel
        {
            get => _selectedLinkedPanel;
            set => SetField(ref _selectedLinkedPanel, value);
        }

        private bool _showInactive = false;
        public bool ShowInactive
        {
            get => _showInactive;
            set { if (SetField(ref _showInactive, value)) ApplyFilter(); }
        }

        private string _listFilter = string.Empty;
        public string ListFilter
        {
            get => _listFilter;
            set { if (SetField(ref _listFilter, value)) ApplyFilter(); }
        }

        private string _patternEvidence = string.Empty;
        public string PatternEvidence
        {
            get => _patternEvidence;
            private set
            {
                if (SetField(ref _patternEvidence, value))
                    OnPropertyChanged(nameof(HasPatternEvidence));
            }
        }

        public bool HasPatternEvidence => !string.IsNullOrWhiteSpace(PatternEvidence);

        private string _analysisReadProblem = string.Empty;
        /// <summary>Shown above the Suspected and Ruled-out panes when they hold saved lists, not the current analysis.</summary>
        public string AnalysisReadProblem
        {
            get => _analysisReadProblem;
            private set
            {
                if (SetField(ref _analysisReadProblem, value))
                    OnPropertyChanged(nameof(HasAnalysisReadProblem));
            }
        }

        public bool HasAnalysisReadProblem => !string.IsNullOrWhiteSpace(AnalysisReadProblem);

        private string _ruleoutNote = "";
        public string RuleoutNote
        {
            get => _ruleoutNote;
            private set => SetField(ref _ruleoutNote, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AttachPanelCommand { get; }
        public ICommand DetachPanelCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand EditPatientCommand { get; }
        public ICommand SupervisorReviewCommand { get; }
        public ICommand CancelInvestigationCommand { get; }
        public ICommand ReinstateInvestigationCommand { get; }
        public ICommand OpenReactionsCommand { get; }
        public ICommand OpenAnalysisCommand { get; }
        public ICommand OpenReportCommand { get; }

        public SpecimensViewModel(DatabaseService db, MainViewModel main)
        {
            _db = db;
            _main = main;

            AddCommand = new RelayCommand(AddSpecimen, () => SessionAccess.CanRecord);
            CancelInvestigationCommand = new RelayCommand(CancelInvestigation,
                () => SessionAccess.CanRecord && (SelectedSpecimen is { IsCancelled: false, HasFinalCall: false }));
            ReinstateInvestigationCommand = new RelayCommand(ReinstateInvestigation,
                () => SelectedSpecimen is { IsCancelled: true });
            SupervisorReviewCommand = new RelayCommand(SupervisorReview,
                () => SelectedSpecimen is { HasFinalCall: true, IsReviewed: false });
            EditPatientCommand = new RelayCommand(EditPatient,
                () => SessionAccess.CanRecord && (!string.IsNullOrWhiteSpace(SelectedSpecimen?.Mrn)));
            EditCommand = new RelayCommand(EditSpecimen,
                () => SessionAccess.CanRecord && SelectedSpecimen != null && !(SelectedSpecimen.HasFinalCall || SelectedSpecimen.IsCancelled));
            DeleteCommand = new RelayCommand(DeleteSpecimen,
                () => SessionAccess.CanRecord && SelectedSpecimen != null && !SelectedSpecimen.HasFinalCall);
            AttachPanelCommand = new RelayCommand(AttachPanel,
                () => SessionAccess.CanRecord && SelectedSpecimen != null && !(SelectedSpecimen.HasFinalCall || SelectedSpecimen.IsCancelled));
            DetachPanelCommand = new RelayCommand(DetachPanel,
                () => SessionAccess.CanRecord && (SelectedSpecimen != null && SelectedLinkedPanel != null) &&
                      !(SelectedSpecimen.HasFinalCall || SelectedSpecimen.IsCancelled));
            RefreshCommand = new RelayCommand(Refresh);
            OpenReactionsCommand = new RelayCommand(OpenReactions, () => SelectedSpecimen != null);
            OpenAnalysisCommand = new RelayCommand(OpenAnalysis, () => SelectedSpecimen != null);
            OpenReportCommand = new RelayCommand(OpenReport, () => SelectedSpecimen != null);

            Refresh();
        }

        private List<Specimen> _allSpecimens = new();

        public void Refresh()
        {
            var selected = SelectedSpecimen?.AccessionNumber;
            _allSpecimens = _db.GetAllSpecimens();
            ApplyFilter(selected);
        }

        public void AnnounceStatus() =>
            _main.SetStatus(ScreenStatus.Specimens(Specimens.Count, SelectedSpecimen?.AccessionNumber));

        /// <summary>Selects the specimen, or returns why it cannot be shown here and leaves the selection alone.</summary>
        public string? SelectSpecimen(string accessionNumber)
        {
            if (!_allSpecimens.Any(s => s.AccessionNumber == accessionNumber))
                Refresh();
            EnsureVisible(accessionNumber);
            var match = Specimens.FirstOrDefault(s => s.AccessionNumber == accessionNumber);
            if (match == null)
                return SpecimenNavigation.CannotOpen(_db, accessionNumber, "Specimens", activeOnly: false)
                    ?? $"Specimen {accessionNumber} could not be shown on the Specimens tab.";
            SelectedSpecimen = match;
            return null;
        }

        private void ApplyFilter(string? preferredAccession = null)
        {
            preferredAccession ??= SelectedSpecimen?.AccessionNumber;
            Specimens.Clear();
            foreach (var s in _allSpecimens)
            {
                if (!_showInactive && !s.IsActive) continue;
                if (!s.MatchesFilter(_listFilter)) continue;
                Specimens.Add(s);
            }
            SelectedSpecimen = Specimens.FirstOrDefault(s => s.AccessionNumber == preferredAccession)
                ?? Specimens.FirstOrDefault();
        }

        private void EnsureVisible(string accessionNumber)
        {
            var match = _allSpecimens.FirstOrDefault(s => s.AccessionNumber == accessionNumber);
            if (match == null) return;
            if (!_showInactive && !match.IsActive)
            {
                _showInactive = true;
                OnPropertyChanged(nameof(ShowInactive));
            }
            if (!match.MatchesFilter(_listFilter))
            {
                _listFilter = string.Empty;
                OnPropertyChanged(nameof(ListFilter));
            }
            ApplyFilter(accessionNumber);
        }

        public ObservableCollection<Services.TimelineEvent> Timeline { get; } = new();

        private Services.InvestigationStatus? _status;
        /// <summary>Derived investigation status for the selected specimen.</summary>
        public Services.InvestigationStatus? Status
        {
            get => _status;
            private set => SetField(ref _status, value);
        }

        private string _patientText = string.Empty;
        /// <summary>Permanent patient information for the selected specimen's MRN.</summary>
        public string PatientText
        {
            get => _patientText;
            private set => SetField(ref _patientText, value);
        }

        private void CancelInvestigation()
        {
            var s = SelectedSpecimen;
            if (s == null) return;
            var dlg = new Views.Dialogs.ReasonDialog(
                $"Cancel investigation {s.AccessionNumber}? The record is kept and locked, and it leaves the work queues.\n" +
                "Enter the reason (for example: mislabeled specimen, hemolyzed, order cancelled).");
            if (dlg.ShowDialog() != true) return;
            try
            {
                _db.CancelInvestigation(s.AccessionNumber, dlg.Reason);
                _main.SetStatus($"Investigation {s.AccessionNumber} cancelled.");
                NotifySpecimensChanged();
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.InvalidOperationException or System.UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Cancel investigation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ReinstateInvestigation()
        {
            var s = SelectedSpecimen;
            if (s == null) return;
            if (!RoleGate.AllowedOrExplained(_db, Permission.ReinstateInvestigation, "Reinstate investigation")) return;
            var dlg = new Views.Dialogs.ReasonDialog(
                $"Reinstate investigation {s.AccessionNumber} (cancelled {s.CancelledAt}: {s.CancelReason})?\nEnter the reason.");
            if (dlg.ShowDialog() != true) return;
            try
            {
                _db.ReinstateInvestigation(s.AccessionNumber, dlg.Reason);
                _main.SetStatus($"Investigation {s.AccessionNumber} reinstated.");
                NotifySpecimensChanged();
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.InvalidOperationException or System.UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Reinstate investigation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SupervisorReview()
        {
            var s = SelectedSpecimen;
            if (s == null) return;
            if (!RoleGate.AllowedOrExplained(_db, Permission.SupervisorReview, "Supervisor review")) return;
            var dlg = new Views.Dialogs.ReasonDialog(
                $"Supervisor review of {s.AccessionNumber}: confirmed {s.FinalAntibodies} by {s.IdentifiedBy ?? "—"}.\n" +
                "Enter your initials. You must not be the person who confirmed the identification.");
            if (dlg.ShowDialog() != true) return;
            try
            {
                _db.SetSpecimenSupervisorReview(s.AccessionNumber, dlg.Reason);
                _main.SetStatus($"Supervisor review recorded for {s.AccessionNumber}.");
                NotifySpecimensChanged();
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.InvalidOperationException or System.UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Supervisor review", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditPatient()
        {
            var mrn = SelectedSpecimen?.Mrn;
            if (string.IsNullOrWhiteSpace(mrn)) return;
            var dlg = new Views.Dialogs.PatientDialog(mrn, _db.GetPatient(mrn));
            if (dlg.ShowDialog() != true) return;
            try
            {
                _db.SavePatient(dlg.Result);
                _main.SetStatus($"Patient {mrn} saved.");
                LoadSpecimenDetails();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving patient: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string FormatPatient(Patient? p, string? mrn)
        {
            if (string.IsNullOrWhiteSpace(mrn)) return "No MRN recorded.";
            if (p == null) return $"No patient record for MRN {mrn}. Use Edit Patient to add one.";
            var lines = new System.Collections.Generic.List<string> { p.Display };
            lines.Add($"Antigen-negative units: {Services.AntigenNegativeRequirement.Display(p.AntigenNegative)}");
            lines.Add($"Transfusion requirements: {p.TransfusionRequirements ?? "None recorded"}");
            lines.Add($"HPC transplant: {Services.TransplantDisplay.Text(p)}");
            if (!string.IsNullOrWhiteSpace(p.BloodBankComments))
                lines.Add($"Blood bank comments: {p.BloodBankComments}");
            return string.Join(System.Environment.NewLine, lines);
        }

        private string _datingText = string.Empty;
        /// <summary>Collection/receipt times and any specimen-dating review note.</summary>
        public string DatingText
        {
            get => _datingText;
            private set => SetField(ref _datingText, value);
        }

        private string _receiptText = string.Empty;
        /// <summary>Receipt checklist summary: complete, or what is missing.</summary>
        public string ReceiptText
        {
            get => _receiptText;
            private set => SetField(ref _receiptText, value);
        }

        private string _receiptDetail = string.Empty;
        /// <summary>Each missing receipt item and why it matters (tooltip).</summary>
        public string ReceiptDetail
        {
            get => _receiptDetail;
            private set => SetField(ref _receiptDetail, value);
        }

        private bool _receiptIncomplete;
        public bool ReceiptIncomplete
        {
            get => _receiptIncomplete;
            private set => SetField(ref _receiptIncomplete, value);
        }

        private string _eluateLinkText = string.Empty;
        /// <summary>"Eluate of X" for an eluate, or "Eluates: A, B" for a source specimen.</summary>
        public string EluateLinkText
        {
            get => _eluateLinkText;
            private set => SetField(ref _eluateLinkText, value);
        }

        private string _patientHistoryText = string.Empty;
        /// <summary>Other specimens with the same MRN, newest first.</summary>
        public string PatientHistoryText
        {
            get => _patientHistoryText;
            private set => SetField(ref _patientHistoryText, value);
        }

        private bool LoadSpecimenDetails()
        {
            PatientHistoryText = string.Empty;
            PatientText = string.Empty;
            Status = null;
            Timeline.Clear();
            LinkedPanels.Clear();
            Antibodies.Clear();
            Ruleouts.Clear();
            PatternEvidence = string.Empty;
            RuleoutNote = "";
            AnalysisReadProblem = string.Empty;

            if (_selectedSpecimen == null) return false;
            var sid = _selectedSpecimen.AccessionNumber;

            foreach (var p in _db.GetSpecimenPanels(sid)) LinkedPanels.Add(p);
            var failed = LoadAnalysisSummary(sid);

            PatientText = FormatPatient(_db.GetPatient(_selectedSpecimen.Mrn), _selectedSpecimen.Mrn);
            Status = Services.InvestigationStatusService.Compute(_db, _selectedSpecimen);
            var times = $"Collected {_selectedSpecimen.CollectedAt ?? "not recorded"}; received {_selectedSpecimen.ReceivedAt ?? _selectedSpecimen.CreatedDate}." +
                        (_selectedSpecimen.OriginDisplay.Length == 0 ? "" : $" {_selectedSpecimen.OriginDisplay}.");
            DatingText = Services.SpecimenDating.Check(_selectedSpecimen, Services.AppSettings.Current.DefaultSpecimenDatingDays,
                Services.AppSettings.Current.DatingTransfusionLookbackDays) is string note
                ? times + " REVIEW: " + note
                : times;
            var eluates = _db.GetEluatesFor(sid);
            var receipt = Services.ReceiptChecklist.Evaluate(_db, _selectedSpecimen);
            ReceiptText = Services.ReceiptChecklist.Summary(receipt);
            ReceiptDetail = Services.ReceiptChecklist.Detail(receipt);
            ReceiptIncomplete = !Services.ReceiptChecklist.RequiredComplete(receipt);
            EluateLinkText = _selectedSpecimen.SourceAccession is string src
                ? $"Eluate of {src}"
                : eluates.Count > 0 ? "Eluates: " + string.Join(", ", eluates.Select(e => e.AccessionNumber)) : "None";
            foreach (var e in Services.InvestigationTimeline.Build(_db, _selectedSpecimen)) Timeline.Add(e);
            if (string.IsNullOrWhiteSpace(_selectedSpecimen.Mrn))
                PatientHistoryText = "No MRN recorded; history from other specimens is not linked.";
            else
            {
                var lines = Services.PatientHistory.Describe(
                    Services.PatientHistory.OtherSpecimens(_db, _selectedSpecimen));
                PatientHistoryText = lines.Count == 0
                    ? $"No other specimens for MRN {_selectedSpecimen.Mrn}."
                    : string.Join(System.Environment.NewLine, lines);
            }
            return failed;
        }

        /// <summary>
        /// Suspected and ruled-out rows and pattern evidence from the current analysis (not an older saved
        /// list). Falls back to the saved lists and returns true when the analysis cannot be read.
        /// </summary>
        private bool LoadAnalysisSummary(string specimenId)
        {
            var failed = false;
            string? failure = null;
            try
            {
                var result = _main.Analyzer.AnalyzeSpecimen(specimenId, updateDb: false);
                PatternEvidence = SpecimenPatternEvidence.Format(result);
                foreach (var row in SpecimenPatternEvidence.SuspectedRows(result))
                    Antibodies.Add(row);
                foreach (var row in SpecimenPatternEvidence.RuleoutRows(result))
                    Ruleouts.Add(row);
            }
            catch (System.Exception ex)
            {
                failed = true;
                failure = ex.Message;
                AppLog.Error($"Reading the analysis of {specimenId} for the Specimens tab failed", ex);
                PatternEvidence = string.Empty;
                foreach (var a in _db.GetSpecimenAntibodies(specimenId)) Antibodies.Add(a);
                foreach (var r in _db.GetSpecimenRuleouts(specimenId)) Ruleouts.Add(r);
                _main.SetStatus($"Pattern evidence for {specimenId} could not be read: {ex.Message}");
            }
            // A failure keeps a lasting note on the panes: the saved lists shown instead may predate later grades,
            // and "no specificity meets the criteria" would describe an analysis that did not run.
            AnalysisReadProblem = failure != null ? SavedListsNote(failure) : string.Empty;
            RuleoutNote = failure != null ? ""
                : Ruleouts.Count == 0 ? "No specificity meets the current rule-out criteria."
                : "";
            return failed;
        }

        /// <summary>The note on the Suspected and Ruled-out panes when the current analysis could not be read.</summary>
        public static string SavedListsNote(string reason) =>
            $"The current analysis could not be read ({reason.Trim().TrimEnd('.')}). These are the lists saved at the " +
            "last Run Analysis and may be out of date; open the Analysis tab and click Run Analysis.";

        /// <summary>
        /// After any specimen CRUD, refresh both this tab and any other tabs that hold a specimen list.
        /// </summary>
        private void NotifySpecimensChanged()
        {
            Refresh();
            _main.ReactionsVM.RefreshSpecimens();
            _main.AnalysisVM.Refresh();
            _main.ReportsVM.Refresh();
            _main.WorklistVM.Refresh();
        }

        private void AddSpecimen()
        {
            var dlg = new Views.Dialogs.SpecimenDialog();
            if (dlg.ShowDialog() != true) return;
            var existing = _db.GetSpecimen(dlg.AccessionNumber);
            if (existing != null)
            {
                var open = MessageBox.Show(
                    $"Accession {dlg.AccessionNumber} already exists. Open that specimen instead?",
                    "Duplicate accession", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (open == MessageBoxResult.Yes)
                    SelectSpecimen(dlg.AccessionNumber);
                return;
            }
            var savedPast = new System.Collections.Generic.List<string>();
            if (!ConfirmEncounter(dlg.AccessionNumber, dlg.EncounterNumber, dlg.Mrn, savedPast)) return;
            if (!ConfirmPregnancy(dlg.Mrn, dlg.PregnancyStatusCode, savedPast)) return;
            try
            {
                Services.SpecimenSaveService.Save(_db, ToEdit(dlg, dlg.AccessionNumber, savedPast), isNew: true);
                _main.SetStatus($"Specimen {dlg.AccessionNumber} added.");
                NotifySpecimensChanged();
                SelectedSpecimen = Specimens.FirstOrDefault(s => s.AccessionNumber == dlg.AccessionNumber);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"The specimen was not added. Nothing was saved.\n\n{ex.Message}", "Specimen not saved",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Warns, without blocking, when the encounter number is already on another patient's specimen.</summary>
        private bool ConfirmEncounter(string accession, string? encounter, string? mrn,
            System.Collections.Generic.List<string> savedPast)
        {
            string? warning;
            try { warning = Services.EncounterCheck.Describe(_db, accession, encounter, mrn); }
            catch (System.ArgumentException) { return true; } // the save reports the invalid value
            if (warning == null) return true;
            if (!OverrideConfirm.Ask(warning + "\n\nSave anyway?", "Encounter on another patient")) return false;
            savedPast.Add(warning);
            return true;
        }

        /// <summary>Warns, without blocking, when a pregnancy is recorded for a patient recorded as male.</summary>
        private bool ConfirmPregnancy(string? mrn, string pregnancyStatus,
            System.Collections.Generic.List<string> savedPast)
        {
            if (mrn == null) return true;
            var patient = _db.GetPatient(mrn);
            if (Services.PatientSexCheck.PregnancyConflict(patient?.Sex, pregnancyStatus, mrn) is not string warning) return true;
            if (!OverrideConfirm.Ask(warning + "\n\nSave anyway?", "Pregnancy and patient sex")) return false;
            savedPast.Add(warning);
            return true;
        }

        private static Services.SpecimenEdit ToEdit(Views.Dialogs.SpecimenDialog dlg, string accession,
            System.Collections.Generic.IReadOnlyList<string>? savedPast = null) => new()
        {
            AccessionNumber = accession,
            SavedPastWarnings = savedPast ?? System.Array.Empty<string>(),
            Type = dlg.SpecimenType,
            ExpirationDate = dlg.ExpirationDate,
            IsActive = dlg.ItemIsActive,
            Notes = dlg.Notes,
            Phenotype = dlg.Phenotype,
            Genotype = dlg.Genotype,
            PreviousAntibodies = dlg.PreviousAntibodies,
            DatResult = dlg.DatResult,
            DatIgg = dlg.DatIgg,
            DatC3 = dlg.DatC3,
            Hemolysis = dlg.Hemolysis,
            TransfusionStatus = dlg.TransfusionStatusCode,
            LastTransfusionDate = dlg.LastTransfusionDate,
            TransfusionProducts = dlg.TransfusionProductCodes,
            TransfusionUnits = dlg.TransfusionUnits,
            Mrn = dlg.Mrn,
            SourceAccession = dlg.SourceAccession,
            CollectedAt = dlg.CollectedAt,
            ReceivedAt = dlg.ReceivedAt,
            Collector = dlg.Collector,
            CollectionLocation = dlg.CollectionLocation,
            OrderingLocation = dlg.OrderingLocation,
            OrderingProvider = dlg.OrderingProvider,
            EncounterNumber = dlg.EncounterNumber,
            PregnancyStatus = dlg.PregnancyStatusCode,
            PregnancyEndDate = dlg.PregnancyEndDate,
            RhigDate = dlg.RhigDate,
        };

        private void EditSpecimen()
        {
            if (SelectedSpecimen == null) return;
            var dlg = new Views.Dialogs.SpecimenDialog(SelectedSpecimen);
            if (dlg.ShowDialog() != true) return;
            var savedPast = new System.Collections.Generic.List<string>();
            if (!ConfirmEncounter(SelectedSpecimen.AccessionNumber, dlg.EncounterNumber, dlg.Mrn, savedPast)) return;
            if (!ConfirmPregnancy(dlg.Mrn, dlg.PregnancyStatusCode, savedPast)) return;
            try
            {
                Services.SpecimenSaveService.Save(_db, ToEdit(dlg, SelectedSpecimen.AccessionNumber, savedPast), isNew: false);
                _main.SetStatus($"Specimen {SelectedSpecimen.AccessionNumber} updated.");
                NotifySpecimensChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"No changes were saved.\n\n{ex.Message}", "Specimen not saved",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSpecimen()
        {
            if (SelectedSpecimen == null) return;
            var acc = SelectedSpecimen.AccessionNumber;
            if (!DestructiveConfirm.Ask($"Delete specimen {acc}? This will also delete all reactions and analysis results.",
                "Confirm Delete")) return;
            try
            {
                _db.DeleteSpecimen(acc);
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Specimen not deleted", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _main.SetStatus($"Specimen {acc} deleted.");
            NotifySpecimensChanged();
        }

        private void OpenReactions()
        {
            if (SelectedSpecimen == null) return;
            _main.OpenSpecimenReactions(SelectedSpecimen.AccessionNumber);
        }

        private void OpenAnalysis()
        {
            if (SelectedSpecimen == null) return;
            _main.OpenSpecimenAnalysis(SelectedSpecimen.AccessionNumber);
        }

        private void OpenReport()
        {
            if (SelectedSpecimen == null) return;
            _main.OpenSpecimenReport(SelectedSpecimen.AccessionNumber);
        }

        private void AttachPanel()
        {
            if (SelectedSpecimen == null) return;
            var allPanels = _db.GetAllPanels();
            var linked = _db.GetSpecimenPanels(SelectedSpecimen.AccessionNumber)
                .Select(p => p.PanelId).ToHashSet();
            var available = allPanels.Where(p => !linked.Contains(p.PanelId) && p.IsActive).ToList();

            var dlg = new Views.Dialogs.SelectPanelDialog(available);
            if (dlg.ShowDialog() != true || dlg.SelectedPanel == null) return;
            try
            {
                _db.LinkSpecimenPanel(SelectedSpecimen.AccessionNumber, dlg.SelectedPanel.PanelId);
            }
            catch (System.Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Panel not attached", MessageBoxButton.OK, MessageBoxImage.Warning);
                NotifySpecimensChanged();
                return;
            }
            _main.SetStatus($"Panel '{dlg.SelectedPanel.Name}' attached.");
            LoadSpecimenDetails();
            _main.ReactionsVM.RefreshSpecimens();
        }

        private void DetachPanel()
        {
            if (SelectedSpecimen == null || SelectedLinkedPanel == null) return;
            if (!DestructiveConfirm.Ask($"Detach panel '{SelectedLinkedPanel.Name}'?", "Confirm")) return;
            try
            {
                _db.UnlinkSpecimenPanel(SelectedSpecimen.AccessionNumber, SelectedLinkedPanel.PanelId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Panel not detached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _main.SetStatus("Panel detached.");
            LoadSpecimenDetails();
            _main.ReactionsVM.RefreshSpecimens();
        }
    }
}
