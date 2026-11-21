using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HemoIQ.Models;

namespace HemoIQ.Views.Dialogs
{
    public partial class PanelRunDialog : Window
    {
        private static readonly Dictionary<string, CellTreatment> CellTreatmentOptions = new()
        {
            { "Untreated (None)", CellTreatment.None },
            { "Ficin-treated",    CellTreatment.Ficin  },
            { "Papain-treated",   CellTreatment.Papain },
            { "DTT-treated",      CellTreatment.DTT    },
        };

        private static readonly Dictionary<string, SerumTreatment> SerumTreatmentOptions = new()
        {
            { "None",                    SerumTreatment.None               },
            { "Prewarmed serum",         SerumTreatment.Prewarmed           },
            { "Absorbed: R1R1 (DCe/DCe)", SerumTreatment.AlloAdsorptionR1R1  },
            { "Absorbed: R2R2 (DcE/DcE)", SerumTreatment.AlloAdsorptionR2R2  },
            { "Absorbed: rr (ce/ce)",    SerumTreatment.AlloAdsorptionRr    },
            { "Autoadsorption",          SerumTreatment.AutoAdsorption       },
        };

        public CellTreatment SelectedCellTreatment =>
            CellTreatmentOptions.TryGetValue(CellTreatmentBox.SelectedItem?.ToString() ?? "", out var ct)
                ? ct : CellTreatment.None;

        public SerumTreatment SelectedSerumTreatment =>
            SerumTreatmentOptions.TryGetValue(SerumTreatmentBox.SelectedItem?.ToString() ?? "", out var st)
                ? st : SerumTreatment.None;

        public string RunLabel => LabelBox.Text.Trim();
        public bool CopyGradesFromCurrentRun => CopyGradesCheck.IsChecked == true;

        public PanelRunDialog()
        {
            InitializeComponent();

            CellTreatmentBox.ItemsSource = CellTreatmentOptions.Keys.ToList();
            CellTreatmentBox.SelectedIndex = 0;
            SerumTreatmentBox.ItemsSource = SerumTreatmentOptions.Keys.ToList();
            SerumTreatmentBox.SelectedIndex = 0;

            CellTreatmentBox.SelectionChanged += (_, _) => UpdateInfoText();
            SerumTreatmentBox.SelectionChanged += (_, _) => UpdateInfoText();
            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            var ct = SelectedCellTreatment;
            var st = SelectedSerumTreatment;
            InfoText.Text = Services.RunTreatmentText.Describe(ct, st);

            // A treated run's grades must come from the treated test, never from the run being viewed.
            var canCopy = Services.RunTreatmentText.CanCopyGrades(ct, st);
            CopyGradesCheck.IsEnabled = canCopy;
            if (!canCopy) CopyGradesCheck.IsChecked = false;
            CopyGradesCheck.ToolTip = canCopy
                ? "Pre-fill this untreated repeat with the grades on the run you are viewing, as unsaved values to check against the test"
                : "A treated run's grades must be read from the treated test, so they are not copied";
        }

        private void SaveClick(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
