using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A confirmed identification is never replaced in place: changing it means clearing it with a reason,
/// which records the earlier identification in the amendment history, and then confirming again.
/// </summary>
public class FinalCallAmendmentTests
{
    private static IsolatedDatabase Confirmed(string acc, string antibodies = "anti-E")
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.SetSpecimenFinalCall(acc, antibodies, "first call", "TE");
        return iso;
    }

    [Fact]
    public void ConfirmingOverAConfirmedIdentification_IsRefused_AndLeavesItUnchanged()
    {
        using var iso = Confirmed("FC-1");
        iso.Db.SetSpecimenSupervisorReview("FC-1", "SU");

        var ex = Assert.Throws<RecordLockedException>(() => iso.Db.SetSpecimenFinalCall("FC-1", "anti-c", null, "XY"));

        Assert.Equal("Specimen FC-1 already has a confirmed identification (anti-E). To change it, click Clear on the Analysis tab " +
                     "and give a reason, then confirm the new identification. The earlier identification stays in the amendment history.",
            ex.Message);
        var s = iso.Db.GetSpecimen("FC-1")!;
        Assert.Equal("anti-E", s.FinalAntibodies);
        Assert.Equal("first call", s.FinalComment);
        Assert.Equal("TE", s.IdentifiedBy);
        Assert.True(s.IsReviewed);
        Assert.Equal(1, iso.Db.CountAuditEvents("confirm_final_call", "FC-1"));
    }

    [Fact]
    public void ClearingThroughTheConfirmMethod_IsRefused()
    {
        using var iso = Confirmed("FC-2");

        Assert.Throws<ArgumentException>(() => iso.Db.SetSpecimenFinalCall("FC-2", null, null, null));

        Assert.Equal("anti-E", iso.Db.GetSpecimen("FC-2")!.FinalAntibodies);
        Assert.Equal(0, iso.Db.CountAuditEvents("clear_final_call", "FC-2"));
    }

    [Fact]
    public void ClearWithReasonThenConfirm_ChangesTheIdentification_AndKeepsTheEarlierOneInTheHistory()
    {
        using var iso = Confirmed("FC-3");

        iso.Db.ClearSpecimenFinalCall("FC-3", "Anti-c also present on repeat testing");
        iso.Db.SetSpecimenFinalCall("FC-3", "anti-E, anti-c", null, "TE");

        Assert.Equal("anti-E, anti-c", iso.Db.GetSpecimen("FC-3")!.FinalAntibodies);
        var entry = Assert.Single(AmendmentHistory.For(iso.Db, "FC-3"));
        Assert.Equal("anti-E", entry.PreviousIdentification);
        Assert.Equal("Anti-c also present on repeat testing", entry.Reason);
        Assert.Equal(2, iso.Db.CountAuditEvents("confirm_final_call", "FC-3"));
    }

    [Fact]
    public void ClearWithoutAReason_IsRefused()
    {
        using var iso = Confirmed("FC-4");
        Assert.Throws<ArgumentException>(() => iso.Db.ClearSpecimenFinalCall("FC-4", "  "));
        Assert.Equal("anti-E", iso.Db.GetSpecimen("FC-4")!.FinalAntibodies);
    }

    [Fact]
    public void FirstConfirmation_StillWorks()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("FC-5", "serum", null);
        iso.Db.SetSpecimenFinalCall("FC-5", "anti-K", null, "TE");
        Assert.Equal("anti-K", iso.Db.GetSpecimen("FC-5")!.FinalAntibodies);
    }
}
