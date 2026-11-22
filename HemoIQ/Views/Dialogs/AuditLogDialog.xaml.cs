using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using HemoIQ.Data;
using HemoIQ.Models;
using Microsoft.Win32;

namespace HemoIQ.Views.Dialogs
{
    public partial class AuditLogDialog : Window
    {
        public const int MaxEvents = 5000;

        private readonly DatabaseService _db;
        private List<AuditEvent> _events = new();
        private int _total;

        public AuditLogDialog(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _events = _db.GetAuditEvents(MaxEvents);
            _total = _db.CountAuditEvents();
            ApplyFilter();
        }

        private void FindChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var shown = _events.Where(ev => ev.MatchesFilter(FindBox.Text)).ToList();
            EventsGrid.ItemsSource = shown;
            CountText.Text = Services.AuditLogExport.CountLabel(shown.Count, _events.Count, _total);
            var note = Services.AuditLogExport.TruncationNote(_events.Count, _total);
            TruncationText.Text = note ?? "";
            TruncationText.Visibility = note == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "audit-log.csv"
            };
            if (dlg.ShowDialog() != true) return;

            int written;
            try
            {
                // The whole log (filtered by Find), read from the database, not only the events loaded on screen.
                using (var writer = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
                    written = Services.AuditLogExport.Write(_db, writer, FindBox.Text);
            }
            catch (System.Exception ex) when (ex is IOException or System.UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"The audit log was not exported: {ex.Message}", "Export audit log",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Services.ExportAudit.Record(_db, "audit_log", dlg.FileName);
            MessageBox.Show(this, $"Exported {written} event(s) to {Path.GetFileName(dlg.FileName)}" +
                                  (string.IsNullOrWhiteSpace(FindBox.Text) ? " (the whole audit log)." : $" matching \"{FindBox.Text.Trim()}\"."),
                "Export audit log", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
