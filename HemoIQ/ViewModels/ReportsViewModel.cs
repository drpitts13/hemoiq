using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly ReportService _reportService;
        private readonly MainViewModel? _main;

        public ObservableCollection<Specimen> Specimens { get; } = new();
        public ObservableCollection<Panel> Panels { get; } = new();

        public ObservableCollection<string> ReportTypes { get; } = new()
        {
            "Specimen Summary",
            "Panel Summary",
            "Analysis Results",
            "All Specimens",
            "All Panels",
            "Clinical Identification",
            "Panel Antigram",
            "Pending Work"
        };

        private string _selectedReportType = "All Specimens";
        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                var changed = SetField(ref _selectedReportType, value);
                UpdatePreview();
                if (changed && _main?.SelectedTabIndex == 5)
                    AnnounceStatus();
            }
        }

        private Specimen? _selectedSpecimen;
        public Specimen? SelectedSpecimen
        {
            get => _selectedSpecimen;
            set { SetField(ref _selectedSpecimen, value); UpdatePreview(); }
        }

        private Panel? _selectedPanel;
        public Panel? SelectedPanel
        {
            get => _selectedPanel;
            set { SetField(ref _selectedPanel, value); UpdatePreview(); }
        }

        private string _previewText = string.Empty;
        public string PreviewText
        {
            get => _previewText;
            set
            {
                if (SetField(ref _previewText, value))
                {
                    (CopyCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PrintCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _specimenFilter = string.Empty;
        public string SpecimenFilter
        {
            get => _specimenFilter;
            set { if (SetField(ref _specimenFilter, value)) ApplySpecimenFilter(); }
        }

        public bool NeedsSpecimen => SelectedReportType is
            "Specimen Summary" or "Analysis Results" or "Clinical Identification";

        public bool NeedsPanel => SelectedReportType is "Panel Summary" or "Panel Antigram";

        public ICommand ExportCsvCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PrintCommand { get; }

        private List<Specimen> _allSpecimens = new();
        private List<Panel> _allPanels = new();

        public ReportsViewModel(DatabaseService db, MainViewModel? main = null)
        {
            _db = db;
            _main = main;
            _reportService = new ReportService(db);

            ExportCsvCommand = new RelayCommand(ExportCsv);
            ExportPdfCommand = new RelayCommand(ExportPdf);
            CopyCommand = new RelayCommand(CopyPreview, () => CanSharePreview(PreviewText));
            PrintCommand = new RelayCommand(PrintPreview, () => CanSharePreview(PreviewText));

            _allSpecimens = _db.GetAllSpecimens();
            _allPanels = _db.GetAllPanels();
            ApplySpecimenFilter();
            foreach (var p in _allPanels) Panels.Add(p);
            SelectedPanel = Panels.FirstOrDefault();
            UpdatePreview();
        }

        /// <summary>
        /// Selects the specimen (and report type), or returns why it cannot be shown here and leaves the selection
        /// alone: a report previewed for another specimen would carry another patient's results.
        /// </summary>
        public string? SelectSpecimen(string accessionNumber, string? reportType = null)
        {
            if (!_allSpecimens.Any(s => s.AccessionNumber == accessionNumber))
                _allSpecimens = _db.GetAllSpecimens();
            var match = _allSpecimens.FirstOrDefault(s => s.AccessionNumber == accessionNumber);
            if (match == null)
                return SpecimenNavigation.CannotOpen(_db, accessionNumber, "Reports", activeOnly: false)
                    ?? $"Specimen {accessionNumber} could not be shown on the Reports tab.";

            if (!string.IsNullOrWhiteSpace(reportType) && ReportTypes.Contains(reportType))
                SelectedReportType = reportType;
            if (!match.MatchesFilter(_specimenFilter))
            {
                _specimenFilter = string.Empty;
                OnPropertyChanged(nameof(SpecimenFilter));
            }
            ApplySpecimenFilter(accessionNumber);
            return null;
        }

        public void AnnounceStatus() =>
            _main?.SetStatus(ScreenStatus.Reports(SelectedReportType));

        /// <summary>
        /// Opening the Reports tab rebuilds the lists and the preview, so a report never shows an analysis,
        /// identification or grades that another tab has since changed.
        /// </summary>
        public void OnTabSelected() => Refresh();

        /// <summary>
        /// The report text to copy or print, rebuilt from the database at that moment. The preview can be older
        /// than the data (it is built when the selection changes), and the printed copy is recorded in the audit
        /// log as issued, so it must be the current report.
        /// </summary>
        public string CurrentShareText()
        {
            UpdatePreview();
            return PreviewText;
        }

        public void Refresh()
        {
            var selSpecimen = SelectedSpecimen?.AccessionNumber;
            var selPanel = SelectedPanel?.PanelId;

            _allSpecimens = _db.GetAllSpecimens();
            ApplySpecimenFilter(selSpecimen);

            Panels.Clear();
            _allPanels = _db.GetAllPanels();
            foreach (var p in _allPanels) Panels.Add(p);
            SelectedPanel = selPanel != null
                ? Panels.FirstOrDefault(p => p.PanelId == selPanel) ?? Panels.FirstOrDefault()
                : Panels.FirstOrDefault();

            UpdatePreview();
        }

        private void ApplySpecimenFilter(string? preferredAccession = null)
        {
            preferredAccession ??= SelectedSpecimen?.AccessionNumber;
            Specimens.Clear();
            foreach (var s in _allSpecimens.Where(x => x.MatchesFilter(_specimenFilter)))
                Specimens.Add(s);
            SelectedSpecimen = preferredAccession != null
                ? Specimens.FirstOrDefault(s => s.AccessionNumber == preferredAccession)
                    ?? Specimens.FirstOrDefault()
                : Specimens.FirstOrDefault();
        }

        private ReportType GetReportType() => SelectedReportType switch
        {
            "Specimen Summary" => ReportType.SpecimenSummary,
            "Panel Summary" => ReportType.PanelSummary,
            "Analysis Results" => ReportType.AnalysisResults,
            "Clinical Identification" => ReportType.ClinicalIdentification,
            "Panel Antigram" => ReportType.PanelAntigram,
            "All Panels" => ReportType.AllPanels,
            "Pending Work" => ReportType.PendingWork,
            _ => ReportType.AllSpecimens
        };

        private void UpdatePreview()
        {
            OnPropertyChanged(nameof(NeedsSpecimen));
            OnPropertyChanged(nameof(NeedsPanel));
            try
            {
                PreviewText = _reportService.GeneratePreviewText(
                    GetReportType(),
                    SelectedSpecimen?.AccessionNumber,
                    SelectedPanel?.PanelId);
            }
            catch (System.Exception ex)
            {
                PreviewText = $"Error generating report: {ex.Message}";
            }
        }

        public static bool CanSharePreview(string? text) =>
            !string.IsNullOrWhiteSpace(text) &&
            !text.StartsWith("Error generating", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<string> PrintLines(string title, string body)
        {
            var lines = new List<string> { title };
            foreach (var line in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
                lines.Add(line.TrimEnd());
            return lines;
        }

        /// <summary>
        /// Refuses Copy, Print and Export when the selected specimen or lot was deleted elsewhere since the list
        /// loaded, and reloads the list so it is no longer offered. True when refused.
        /// </summary>
        private bool RefusedMissingSubject()
        {
            if (_reportService.MissingSubject(GetReportType(), SelectedSpecimen?.AccessionNumber, SelectedPanel?.PanelId)
                is not { } why)
                return false;
            MessageBox.Show(why, "Report not issued", MessageBoxButton.OK, MessageBoxImage.Warning);
            Refresh();
            _main?.SetStatus(why);
            return true;
        }

        private void CopyPreview()
        {
            if (RefusedMissingSubject()) return;
            var text = CurrentShareText();
            if (!CanSharePreview(text)) return;
            try
            {
                Clipboard.SetText(text);
                _main?.SetStatus("Report copied to the clipboard.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not copy the report: {ex.Message}", "Copy",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPreview()
        {
            if (RefusedMissingSubject()) return;
            var text = CurrentShareText();
            if (!CanSharePreview(text)) return;
            try
            {
                var dialog = new System.Windows.Controls.PrintDialog();
                if (dialog.ShowDialog() != true) return;
                var width = dialog.PrintableAreaWidth > 0 ? dialog.PrintableAreaWidth : 816;
                var height = dialog.PrintableAreaHeight > 0 ? dialog.PrintableAreaHeight : 1056;
                var doc = BuildPrintDocument(SelectedReportType, text, width, height);
                dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                    $"{SelectedReportType} report");
                _reportService.RecordPrint(GetReportType(), SelectedSpecimen?.AccessionNumber,
                    SelectedPanel?.PanelId, text);
                _main?.SetStatus($"Sent {SelectedReportType} to the printer.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed: {ex.Message}", "Print",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static FlowDocument BuildPrintDocument(string title, string body, double pageWidth, double pageHeight)
        {
            var doc = new FlowDocument
            {
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                PagePadding = new Thickness(48),
                ColumnWidth = Math.Max(96, pageWidth - 96),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };
            doc.Blocks.Add(new Paragraph(new Run(title))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 12)
            });
            foreach (var line in PrintLines(title, body).Skip(1))
                doc.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0) });
            return doc;
        }

        private void ExportCsv()
        {
            if (RefusedMissingSubject()) return;
            var dlg = new SaveFileDialog { Filter = "CSV Files|*.csv", DefaultExt = "csv" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _reportService.ExportToCsv(GetReportType(), dlg.FileName,
                    SelectedSpecimen?.AccessionNumber, SelectedPanel?.PanelId);
                _main?.SetStatus($"CSV exported: {dlg.FileName}");
                MessageBox.Show("CSV exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdf()
        {
            if (RefusedMissingSubject()) return;
            var dlg = new SaveFileDialog { Filter = "PDF Files|*.pdf", DefaultExt = "pdf" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _reportService.ExportToPdf(GetReportType(), dlg.FileName,
                    SelectedSpecimen?.AccessionNumber, SelectedPanel?.PanelId);
                _main?.SetStatus($"PDF exported: {dlg.FileName}");
                MessageBox.Show("PDF exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
