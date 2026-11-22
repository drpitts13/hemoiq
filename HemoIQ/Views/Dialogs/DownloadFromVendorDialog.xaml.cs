using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HemoIQ.Data;
using HemoIQ.Services.Vendors;
using Microsoft.Win32;

namespace HemoIQ.Views.Dialogs
{
    public partial class DownloadFromVendorDialog : Window
    {
        private readonly DatabaseService _db;
        private readonly VendorCatalogService _catalog;
        private readonly VendorPanelImportService _importer;
        private readonly VendorDialogGate _gate = new();

        public int? ImportedPanelId { get; private set; }
        public string? ImportedPanelName { get; private set; }

        public DownloadFromVendorDialog(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _catalog = new VendorCatalogService();
            _importer = new VendorPanelImportService(db);
            VendorBox.ItemsSource = _catalog.Sources.Select(s => s.DisplayName).ToList();
            VendorBox.SelectedIndex = 0;
            // Closing cancels a download in progress; its result is then discarded, not stored.
            Closing += (_, _) => _gate.Close();
            Closed += (_, _) => { _gate.Dispose(); _catalog.Dispose(); };
        }

        /// <summary>Only one download or import at a time: everything that starts one is disabled while it runs.</summary>
        private void SetBusy(bool busy)
        {
            if (busy && !_gate.TryBegin()) return;
            if (!busy) _gate.End();
            var source = SelectedSource;
            var canList = !busy && source?.CanListLots == true;
            VendorBox.IsEnabled = !busy;
            RefreshButton.IsEnabled = canList;
            ImportSelectedButton.IsEnabled = canList;
            ImportFileButton.IsEnabled = !busy;
        }

        private IVendorPanelSource? SelectedSource
        {
            get
            {
                if (VendorBox.SelectedItem is not string name) return null;
                return _catalog.Sources.FirstOrDefault(s => s.DisplayName == name);
            }
        }

        private void VendorBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LotGrid.ItemsSource = null;
            var source = SelectedSource;
            if (source == null) return;
            HintText.Text = source.ListLotsUnavailableReason ?? "";
            RefreshButton.IsEnabled = source.CanListLots;
            ImportSelectedButton.IsEnabled = source.CanListLots;
            StatusText.Text = source.CanListLots
                ? "Click Refresh lots to load the public catalog."
                : source.ListLotsUnavailableReason;
        }

        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            var source = SelectedSource;
            if (source == null || !source.CanListLots || _gate.IsBusy || _gate.IsClosed) return;
            SetBusy(true);
            StatusText.Text = "Loading public lot catalog…";
            try
            {
                var lots = await source.ListLotsAsync(_gate.Token).ConfigureAwait(true);
                if (!_gate.MayApply) return;
                LotGrid.ItemsSource = lots;
                StatusText.Text = lots.Count == 0
                    ? "No public lots were listed. Import a vendor file instead."
                    : $"Found {lots.Count} public lot(s). Select one and import.";
            }
            catch (Exception ex) when (_gate.MayApply)
            {
                StatusText.Text = "Could not list lots: " + ex.Message;
            }
            catch (Exception) { /* the dialog was closed: nothing to show */ }
            finally
            {
                if (!_gate.IsClosed) SetBusy(false);
            }
        }

        private async void ImportSelectedClick(object sender, RoutedEventArgs e)
        {
            if (_gate.IsBusy || _gate.IsClosed) return;
            if (LotGrid.SelectedItem is not VendorLotListing lot)
            {
                MessageBox.Show("Select a lot from the catalog.", "Download",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(lot.DownloadUrl))
            {
                MessageBox.Show("That listing has no public download URL. Import a vendor file instead.",
                    "Download", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SetBusy(true);
            StatusText.Text = $"Downloading {lot.LotNumber}…";
            try
            {
                var parsed = await _catalog.DownloadAndParseAsync(lot, _gate.Token).ConfigureAwait(true);
                // A download that finishes after the dialog was closed is discarded, never stored.
                if (!_gate.MayApply) return;
                PersistParsed(parsed);
            }
            catch (Exception ex) when (_gate.MayApply)
            {
                MessageBox.Show(this, ex.Message, "Download", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception) { /* the dialog was closed: nothing was stored */ }
            finally
            {
                if (!_gate.IsClosed) SetBusy(false);
            }
        }

        private void ImportFileClick(object sender, RoutedEventArgs e)
        {
            var source = SelectedSource;
            if (source == null || _gate.IsBusy || _gate.IsClosed) return;
            var open = new OpenFileDialog
            {
                Title = "Import vendor panel file",
                Filter = "Vendor files|*.pdf;*.csv;*.json;*.xml;*.xlsx|PDF|*.pdf|CSV|*.csv|JSON|*.json|XML|*.xml|Excel|*.xlsx|All files|*.*"
            };
            if (open.ShowDialog() != true) return;
            try
            {
                var parsed = _catalog.ImportFile(source.VendorId, open.FileName);
                parsed.SourceUrl ??= open.FileName;
                PersistParsed(parsed);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PersistParsed(VendorParseResult parsed)
        {
            if (!parsed.Success)
            {
                MessageBox.Show(
                    "Could not parse the vendor file:\n" + string.Join("\n", parsed.Errors.Take(12)),
                    "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = _db.FindPanelByVendorLot(parsed.Vendor, parsed.LotNumber);
            var replace = false;
            if (existing != null)
            {
                if (!ViewModels.OverrideConfirm.Ask(
                        $"Lot {parsed.LotNumber} from {parsed.Vendor} is already stored as '{existing.Name}'. Replace it?",
                        "Duplicate lot", this)) return;
                replace = true;
            }

            var outcome = _importer.Import(parsed, replace);
            ImportedPanelId = outcome.PanelId;
            ImportedPanelName = parsed.Name;
            var reviewDlg = new ImportReviewDialog(outcome.Review) { Owner = this };
            if (reviewDlg.ShowDialog() == true && reviewDlg.ActivateRequested
                && ViewModels.PanelsViewModel.ConfirmExpiredActivation(_db.GetPanel(outcome.PanelId)?.ExpirationDate))
                _db.SetPanelActive(outcome.PanelId, true, Services.PanelActivationCoverage.Check(_db, _db.GetPanel(outcome.PanelId)!));
            StatusText.Text = $"Imported '{parsed.Name}' ({parsed.Cells.Count} cells). {outcome.Review.Explanation}";
            DialogResult = true;
        }
    }
}
