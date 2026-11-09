using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class ReceiptChecklistTests
{
    private static Specimen Complete() => new()
    {
        AccessionNumber = "RC-1",
        Mrn = "M-1",
        CollectedAt = "2026-11-08 07:00",
        ReceivedAt = "2026-11-08 08:00",
        TransfusionStatus = TransfusionStatus.Transfused,
        LastTransfusionDate = "2026-10-01",
        Hemolysis = SpecimenHemolysis.None,
        PregnancyStatus = PregnancyStatus.NotPregnant,
        DatResult = "Negative",
    };

    [Fact]
    public void FullyRecordedSpecimen_IsComplete()
    {
        var items = ReceiptChecklist.Evaluate(Complete());
        Assert.True(ReceiptChecklist.RequiredComplete(items));
        Assert.Equal("Complete (8 of 8).", ReceiptChecklist.Summary(items));
        Assert.Equal("", ReceiptChecklist.Detail(items));
    }

    [Fact]
    public void NewSpecimen_ListsRequiredGapsFirst_ThenAdvisory()
    {
        var items = ReceiptChecklist.Evaluate(new Specimen { AccessionNumber = "RC-2" });
        Assert.False(ReceiptChecklist.RequiredComplete(items));
        Assert.Equal("0 of 7: missing Patient MRN, Collection date/time, Transfusion status; " +
                     "not recorded Received date/time, Visual hemolysis, Pregnancy status, DAT.", ReceiptChecklist.Summary(items));
        Assert.Contains("Required: Transfusion status — decides whether the serologic phenotype can be trusted.", ReceiptChecklist.Detail(items));
        Assert.Contains("Advisory: DAT — ", ReceiptChecklist.Detail(items));
    }

    [Fact]
    public void Transfused_WithoutADate_IsIncomplete_NotTransfused_NeedsNoDate()
    {
        var s = Complete();
        s.LastTransfusionDate = null;
        var items = ReceiptChecklist.Evaluate(s);
        Assert.False(ReceiptChecklist.RequiredComplete(items));
        Assert.Contains("missing Last transfusion date", ReceiptChecklist.Summary(items));

        s.TransfusionStatus = TransfusionStatus.NotTransfused;
        items = ReceiptChecklist.Evaluate(s);
        Assert.True(ReceiptChecklist.RequiredComplete(items));
        Assert.DoesNotContain(items, i => i.Name == "Last transfusion date");
    }

    [Fact]
    public void AdvisoryGaps_DoNotMakeReceiptIncomplete()
    {
        var s = Complete();
        s.Hemolysis = SpecimenHemolysis.NotRecorded;
        s.DatResult = null;
        var items = ReceiptChecklist.Evaluate(s);
        Assert.True(ReceiptChecklist.RequiredComplete(items));
        Assert.Equal("6 of 8: not recorded Visual hemolysis, DAT.", ReceiptChecklist.Summary(items));
    }

    [Fact]
    public void InvestigationStatus_ReceivedStep_AndNextAction_FollowTheChecklist()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RC-3", "serum", null);
        var st = InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("RC-3")!, requireReview: false);
        Assert.False(st.Steps[0].Done);
        Assert.Equal("Received", st.Steps[0].Name);
        Assert.Equal("Complete receipt (Patient MRN, Collection date/time, Transfusion status) with Edit, then attach a panel on the Specimens tab.",
            st.NextAction);

        iso.Db.UpdateSpecimenMrn("RC-3", "M-3");
        iso.Db.UpdateSpecimenCollection("RC-3", DateTime.Today.ToString("yyyy-MM-dd") + " 07:00", null);
        iso.Db.UpdateSpecimenTransfusion("RC-3", TransfusionStatus.NotTransfused, null);
        st = InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("RC-3")!, requireReview: false);
        Assert.True(st.Steps[0].Done);
        Assert.Equal("Attach a panel on the Specimens tab.", st.NextAction);
    }

    [Fact]
    public void ClinicalReport_ShowsTheReceiptChecklist()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RC-4", "serum", null);
        var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "RC-4");
        Assert.Contains("Receipt checklist: 0 of 7: missing Patient MRN, Collection date/time, Transfusion status;", text);
    }
}
