using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// The Reports preview is built when the selection changes, so another tab can change the data under it.
/// Opening the tab rebuilds it, and Copy and Print rebuild the text at that moment, so a printed (and audited)
/// report is never an earlier version of the investigation.
/// </summary>
public class ReportPreviewFreshnessTests
{
    private static (IsolatedDatabase Iso, ReportsViewModel Vm) Open()
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RPF-1", "serum", null);
        var vm = new ReportsViewModel(iso.Db);
        vm.SelectSpecimen("RPF-1", "Clinical Identification");
        Assert.DoesNotContain("anti-K", vm.PreviewText);
        return (iso, vm);
    }

    [Fact]
    public void ChangeOnAnotherTab_LeavesThePreviewOld_UntilTheTabIsOpened()
    {
        var (iso, vm) = Open();
        using (iso)
        {
            iso.Db.SetSpecimenFinalCall("RPF-1", "anti-K", null, "TE");
            Assert.DoesNotContain("anti-K", vm.PreviewText); // built before the change

            vm.OnTabSelected();
            Assert.Contains("anti-K", vm.PreviewText);
            Assert.Equal("RPF-1", vm.SelectedSpecimen?.AccessionNumber);
            Assert.Equal("Clinical Identification", vm.SelectedReportType);
        }
    }

    [Fact]
    public void CopyAndPrintText_IsRebuiltFromTheDatabase()
    {
        var (iso, vm) = Open();
        using (iso)
        {
            iso.Db.SetSpecimenFinalCall("RPF-1", "anti-K", null, "TE");
            var text = vm.CurrentShareText();
            Assert.Contains("anti-K", text);
            Assert.Equal(text, vm.PreviewText); // what is printed is what the preview now shows
        }
    }

    [Fact]
    public void DeletedSpecimen_IsDroppedFromTheListWhenTheTabIsOpened()
    {
        var (iso, vm) = Open();
        using (iso)
        {
            iso.Db.DeleteSpecimen("RPF-1");
            vm.OnTabSelected();
            Assert.DoesNotContain(vm.Specimens, s => s.AccessionNumber == "RPF-1");
        }
    }
}
