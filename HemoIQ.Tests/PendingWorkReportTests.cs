using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The Pending Work report must not list a cancelled investigation as work to do, and must not say "No pending work"
/// while confirmed identifications still wait for a required supervisor review (the Worklist lists those).
/// </summary>
[Collection("PersistentDatabase")]
public class PendingWorkReportTests
{
    [Fact]
    public void CancelledInvestigation_IsCountedSeparately_NotAsPending()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("PW-OPEN", "serum", null);
        iso.Db.AddSpecimen("PW-CXL", "serum", null);
        iso.Db.CancelInvestigation("PW-CXL", "Duplicate order");

        var text = iso.Reports.GeneratePreviewText(ReportType.PendingWork);

        Assert.Contains("Pending:           1", text);
        Assert.Contains("Cancelled:         1 (closed without a call; not pending)", text);
        Assert.Contains("PW-OPEN", text);
        Assert.DoesNotContain("PW-CXL", text);
    }

    [Fact]
    public void OnlyCancelledLeft_SaysNoPendingWork_WithoutClaimingEveryoneIsConfirmed()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("PW-CXL", "serum", null);
        iso.Db.CancelInvestigation("PW-CXL", "Specimen hemolyzed");

        var text = iso.Reports.GeneratePreviewText(ReportType.PendingWork);

        Assert.Contains("Every active specimen has a confirmed identification or a cancelled investigation. No pending work.", text);
        Assert.DoesNotContain("All active specimens have a confirmed antibody identification", text);
    }

    [Fact]
    public void RequiredReviewOutstanding_IsListed_AndBlocksTheNoPendingMessage()
    {
        var previous = AppSettings.Current.RequireSupervisorReview;
        AppSettings.Current.RequireSupervisorReview = true;
        try
        {
            using var iso = new IsolatedDatabase();
            iso.Db.AddSpecimen("PW-REV", "serum", null);
            iso.Db.SetSpecimenFinalCall("PW-REV", "anti-K", null, "TE");

            var text = iso.Reports.GeneratePreviewText(ReportType.PendingWork);
            Assert.Contains("Awaiting review:   1", text);
            Assert.Contains("CONFIRMED — AWAITING SUPERVISOR REVIEW (1):", text);
            Assert.Contains("PW-REV", text);
            Assert.DoesNotContain("No pending work", text);

            iso.Db.SetSpecimenSupervisorReview("PW-REV", "SV");
            var after = iso.Reports.GeneratePreviewText(ReportType.PendingWork);
            Assert.DoesNotContain("AWAITING SUPERVISOR REVIEW", after);
            Assert.Contains("No pending work.", after);
        }
        finally
        {
            AppSettings.Current.RequireSupervisorReview = previous;
        }
    }

    [Fact]
    public void ReviewNotRequired_ConfirmedCallsAreFinished()
    {
        var previous = AppSettings.Current.RequireSupervisorReview;
        AppSettings.Current.RequireSupervisorReview = false;
        try
        {
            using var iso = new IsolatedDatabase();
            iso.Db.AddSpecimen("PW-DONE", "serum", null);
            iso.Db.SetSpecimenFinalCall("PW-DONE", "anti-K", null, "TE");
            var text = iso.Reports.GeneratePreviewText(ReportType.PendingWork);
            Assert.DoesNotContain("Awaiting review", text);
            Assert.Contains("All active specimens have a confirmed antibody identification. No pending work.", text);
        }
        finally
        {
            AppSettings.Current.RequireSupervisorReview = previous;
        }
    }
}
