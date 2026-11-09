using System.Windows;
using System.Windows.Controls;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Views.Dialogs;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class SpecimenDialogSectionsTests
{
    private static void OnSta(Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
    }

    [Fact]
    public void Summary_NamesRecordedContext_AndSkipsUnknownOrNotTested()
    {
        Assert.Equal(SpecimenContextSummary.Nothing, SpecimenContextSummary.Describe(new Specimen()));
        Assert.Equal(SpecimenContextSummary.Nothing, SpecimenContextSummary.Describe(new Specimen
        {
            TransfusionStatus = TransfusionStatus.NotTransfused, PregnancyStatus = PregnancyStatus.NotPregnant, DatResult = "NT",
        }));

        var text = SpecimenContextSummary.Describe(new Specimen
        {
            TransfusionStatus = TransfusionStatus.Transfused, LastTransfusionDate = "2026-10-30",
            PregnancyStatus = PregnancyStatus.Recent, PregnancyEndDate = "2026-09-01", RhigDate = "2026-09-02",
            DatResult = "2+", DatIgg = "2+", Phenotype = "R1r K-", PreviousAntibodies = "anti-K",
        });
        Assert.StartsWith("Context: Transfused (last 2026-10-30)", text);
        Assert.Contains("(ended 2026-09-01)", text);
        Assert.Contains("RhIG 2026-09-02", text);
        Assert.Contains("DAT ", text);
        Assert.Contains("IgG 2+", text);
        Assert.Contains("phenotype R1r K-", text);
        Assert.Contains("previous anti-K", text);
    }

    [Fact]
    public void Dialog_HasFourSections_AndShowsContextFromHiddenTabs() => OnSta(() =>
    {
        var dlg = new SpecimenDialog(new Specimen
        {
            AccessionNumber = "SD-TAB", Type = "serum", CreatedDate = "2026-11-01",
            TransfusionStatus = TransfusionStatus.Transfused, LastTransfusionDate = "2026-10-30",
            PreviousAntibodies = "anti-E",
        });
        var tabs = (TabControl)dlg.FindName("Sections");
        Assert.Equal(new[] { "Specimen", "Clinical context", "Serology", "Notes" },
            tabs.Items.Cast<TabItem>().Select(t => (string)t.Header));
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Contains("Transfused (last 2026-10-30)", dlg.ContextSummary);
        Assert.Contains("previous anti-E", dlg.ContextSummary);

        // Editing a field on another tab updates the summary.
        ((TextBox)dlg.FindName("PhenotypeBox")).Text = "R2R2";
        Assert.Contains("phenotype R2R2", dlg.ContextSummary);
        dlg.Close();
    });

    [Fact]
    public void SelectSectionContaining_OpensTheTabThatHoldsTheField() => OnSta(() =>
    {
        var dlg = new SpecimenDialog();
        var tabs = (TabControl)dlg.FindName("Sections");

        Assert.Equal("Clinical context",
            dlg.SelectSectionContaining((FrameworkElement)dlg.FindName("PregnancyEndPicker"))?.Header);
        Assert.Equal(1, tabs.SelectedIndex);

        Assert.Equal("Specimen",
            dlg.SelectSectionContaining((FrameworkElement)dlg.FindName("ReceivedTimeBox"))?.Header);
        Assert.Equal(0, tabs.SelectedIndex);

        Assert.Equal("Serology",
            dlg.SelectSectionContaining((FrameworkElement)dlg.FindName("DatIggBox"))?.Header);
        Assert.Equal(2, tabs.SelectedIndex);
        dlg.Close();
    });

    [Fact]
    public void NewSpecimen_ValuesFromEveryTabAreReturned() => OnSta(() =>
    {
        var dlg = new SpecimenDialog();
        ((TextBox)dlg.FindName("AccessionBox")).Text = " NEW-1 ";
        ((TextBox)dlg.FindName("OrderingLocationBox")).Text = "ICU";
        ((DatePicker)dlg.FindName("RhigPicker")).SelectedDate = new DateTime(2026, 10, 1);
        ((TextBox)dlg.FindName("GenotypeBox")).Text = "FY*02/FY*02";
        ((TextBox)dlg.FindName("NotesBox")).Text = "hemolyzed";

        Assert.Equal("NEW-1", dlg.AccessionNumber);
        Assert.Equal("ICU", dlg.OrderingLocation);
        Assert.Equal("2026-10-01", dlg.RhigDate);
        Assert.Equal("FY*02/FY*02", dlg.Genotype);
        Assert.Equal("hemolyzed", dlg.Notes);
        Assert.Contains("RhIG 2026-10-01", dlg.ContextSummary);
        dlg.Close();
    });

    /// <summary>
    /// Every tab's fields fit inside the fixed-height tab area, so adding a field cannot push others
    /// out of sight. Rendered off-screen with the most content (transfused with products, recent pregnancy).
    /// </summary>
    [Fact]
    public void EveryTab_FitsWithoutClipping() => OnSta(() =>
    {
        var dlg = new SpecimenDialog(new Specimen
        {
            AccessionNumber = "SD-FIT", Type = "serum", CreatedDate = "2026-11-01", Mrn = "MRN-1", EncounterNumber = "V100",
            TransfusionStatus = TransfusionStatus.Transfused, LastTransfusionDate = "2026-10-30",
            TransfusionProducts = "rbc,platelets,plasma", TransfusionUnits = 3,
            PregnancyStatus = PregnancyStatus.Recent, PregnancyEndDate = "2026-09-01",
        })
        {
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
            ShowInTaskbar = false, ShowActivated = false,
        };
        dlg.Show();
        try
        {
            var tabs = (TabControl)dlg.FindName("Sections");
            foreach (TabItem tab in tabs.Items)
            {
                tabs.SelectedItem = tab;
                dlg.UpdateLayout();
                var content = (FrameworkElement)tab.Content;
                var host = (FrameworkElement)System.Windows.Media.VisualTreeHelper.GetParent(content);
                content.Measure(new Size(content.ActualWidth, double.PositiveInfinity));
                var needed = content.DesiredSize.Height;
                Assert.True(needed <= host.ActualHeight + 0.5,
                    $"Tab '{tab.Header}' needs {needed:0} px but has {host.ActualHeight:0} px; its last fields would be clipped.");
            }
        }
        finally
        {
            dlg.Close();
        }
    });
}
