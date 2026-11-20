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

        public AuditLogDialog(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _events = _db.GetAuditEvents(MaxEvents);
            ApplyFilter();
        }

        private void FindChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var shown = _events.Where(ev => ev.MatchesFilter(FindBox.Text)).ToList();
            EventsGrid.ItemsSource = shown;
            CountText.Text = shown.Count == _events.Count
                ? $"{shown.Count} event(s)"
                : $"Showing {shown.Count} of {_events.Count}";
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "audit-log.csv"
            };
            if (dlg.ShowDialog() != true) return;

            var shown = EventsGrid.ItemsSource as IEnumerable<AuditEvent> ?? _events;
            var sb = new StringBuilder();
            sb.AppendLine("OccurredAtUtc,Operator,Action,EntityType,EntityId,Reason,BeforeJson,AfterJson");
            foreach (var ev in shown)
            {
                sb.AppendLine(string.Join(",",
                    Csv(ev.OccurredAtUtc), Csv(ev.Operator), Csv(ev.Action),
                    Csv(ev.EntityType), Csv(ev.EntityId), Csv(ev.Reason),
                    Csv(ev.BeforeJson), Csv(ev.AfterJson)));
            }
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            Services.ExportAudit.Record(_db, "audit_log", dlg.FileName);
        }

        private static string Csv(string? value) => Services.CsvText.Field(value);
    }
}
