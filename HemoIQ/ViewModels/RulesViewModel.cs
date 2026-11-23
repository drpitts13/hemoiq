using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    public class RulesViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly MainViewModel _main;

        public ObservableCollection<Rule> Rules { get; } = new();

        private Rule? _selectedRule;
        public Rule? SelectedRule
        {
            get => _selectedRule;
            set
            {
                var previous = _selectedRule?.RuleId;
                if (!SetField(ref _selectedRule, value)) return;
                if (_main.SelectedTabIndex == 8 && previous != _selectedRule?.RuleId)
                    AnnounceStatus();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }

        public RulesViewModel(DatabaseService db, MainViewModel main)
        {
            _db = db;
            _main = main;
            AddCommand = new RelayCommand(AddRule, () => SessionAccess.CanRecord);
            EditCommand = new RelayCommand(EditRule, () => SessionAccess.CanRecord && (SelectedRule != null));
            DeleteCommand = new RelayCommand(DeleteRule, () => SessionAccess.CanRecord && (SelectedRule != null));
            RefreshCommand = new RelayCommand(Refresh);
            ExportCommand = new RelayCommand(ExportRules, () => Rules.Count > 0);
            ImportCommand = new RelayCommand(ImportRules, () => SessionAccess.CanRecord);
            Refresh();
        }

        public void AnnounceStatus() =>
            _main.SetStatus(ScreenStatus.Rules(Rules.Count, SelectedRule?.Name));

        public void Refresh()
        {
            var sid = SelectedRule?.RuleId;
            Rules.Clear();
            foreach (var r in _db.GetAllRules()) Rules.Add(r);
            SelectedRule = Rules.FirstOrDefault(r => r.RuleId == sid) ?? Rules.FirstOrDefault();
        }

        /// <summary>Rules change rule-outs, so they need the supervisor or administrator role once users are registered.</summary>
        private bool CanConfigure()
        {
            try
            {
                AccessControl.Demand(_db, Permission.ConfigureRules);
                return true;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Rules", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private const string StaleNote = " Open investigations analyzed before this change are listed on the Worklist as Stale analysis.";

        private void AddRule()
        {
            if (!CanConfigure()) return;
            var dlg = new Views.Dialogs.RuleDialog();
            if (dlg.ShowDialog() != true) return;
            int id;
            try
            {
                id = _db.AddRule(dlg.RuleName, dlg.Description, dlg.Antibody,
                    dlg.ExceptionAntigen, dlg.HeterozygousOk, dlg.MinRuleoutCount,
                    dlg.SeparationExempt, dlg.SeparationExemptFrom);
            }
            catch (System.ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Rule not added", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _main.WorklistVM.Refresh();
            Refresh();
            SelectedRule = Rules.FirstOrDefault(r => r.RuleId == id);
            _main.SetStatus($"Rule '{dlg.RuleName}' added." + StaleNote);
        }

        private void EditRule()
        {
            if (SelectedRule == null || !CanConfigure()) return;
            var dlg = new Views.Dialogs.RuleDialog(SelectedRule);
            if (dlg.ShowDialog() != true) return;
            DatabaseService.RuleEditResult outcome;
            try
            {
                outcome = _db.UpdateRule(SelectedRule.RuleId, dlg.RuleName, dlg.Description, dlg.Antibody,
                    dlg.ExceptionAntigen, dlg.HeterozygousOk, dlg.MinRuleoutCount,
                    dlg.SeparationExempt, dlg.SeparationExemptFrom);
            }
            catch (System.ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Rule not saved", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _main.WorklistVM.Refresh();
            Refresh();
            _main.SetStatus(EditStatus(dlg.RuleName, outcome));
        }

        /// <summary>The status after Edit: only an edit to what analysis reads says open analyses are now stale.</summary>
        public static string EditStatus(string ruleName, DatabaseService.RuleEditResult outcome) => outcome switch
        {
            DatabaseService.RuleEditResult.Unchanged => $"Rule '{ruleName}' unchanged; nothing was saved.",
            DatabaseService.RuleEditResult.LabelOnly => $"Rule '{ruleName}' updated (name or description only; analyses are not affected).",
            _ => $"Rule '{ruleName}' updated." + StaleNote,
        };

        private void DeleteRule()
        {
            if (SelectedRule == null || !CanConfigure()) return;
            if (!DestructiveConfirm.Ask($"Delete rule '{SelectedRule.Name}'?", "Confirm")) return;
            _db.DeleteRule(SelectedRule.RuleId);
            _main.WorklistVM.Refresh();
            Refresh();
            _main.SetStatus("Rule deleted. Its settings are kept in the audit log." + StaleNote);
        }

        private void ExportRules()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export rules",
                Filter = "HemoIQ rules (*.json)|*.json",
                FileName = $"hemoiq-rules-{System.DateTime.Now:yyyyMMdd}.json",
            };
            if (dlg.ShowDialog() != true) return;
            System.IO.File.WriteAllText(dlg.FileName, RuleTransfer.Export(_db.GetAllRules(), System.DateTime.Now));
            _main.SetStatus($"{Rules.Count} rule(s) exported to {System.IO.Path.GetFileName(dlg.FileName)}.");
        }

        private void ImportRules()
        {
            if (!CanConfigure()) return;
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Import rules", Filter = "HemoIQ rules (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;
            System.Collections.Generic.List<RuleImportLine> preview;
            try
            {
                preview = RuleTransfer.Preview(System.IO.File.ReadAllText(dlg.FileName), _db.GetAllRules(), _db.GetKnownAntigens());
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.IO.IOException)
            {
                MessageBox.Show(ex.Message, "Rules not imported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var summary = RuleTransfer.Summarize(preview);
            if (!preview.Any(l => l.Action is RuleImportAction.Add or RuleImportAction.Update))
            {
                MessageBox.Show("Nothing to import.\n\n" + summary,"Import rules", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(summary + "\n\nRefused entries are skipped. Rules not in the file are kept. Apply the adds and updates?",
                    "Import rules", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                var (added, updated) = RuleTransfer.Apply(_db, preview);
                _main.SetStatus($"Rules imported: {added} added, {updated} updated." + StaleNote);
            }
            catch (System.Exception ex) when (ex is System.ArgumentException or System.UnauthorizedAccessException)
            {
                MessageBox.Show("No rules were changed.\n\n" + ex.Message,"Rules not imported", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _main.WorklistVM.Refresh();
            Refresh();
        }
    }
}
