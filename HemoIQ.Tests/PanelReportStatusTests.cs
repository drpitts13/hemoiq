using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Panel reports state whether the lot may be used, so a printed antigram of an imported lot awaiting review,
/// an expired lot or a deactivated lot is not mistaken for one in use.
/// </summary>
public class PanelReportStatusTests
{
    private static string Day(int offset) => DateTime.Today.AddDays(offset).ToString("yyyy-MM-dd");

    [Theory]
    [InlineData(ReportType.PanelSummary)]
    [InlineData(ReportType.PanelAntigram)]
    public void ActiveLot_IsMarkedActive(ReportType type)
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Active", "ACT-1", "V", 2, Day(90), false);

        var text = iso.Reports.GeneratePreviewText(type, panelId: id);

        Assert.Contains("Status:     Active", text);
        Assert.DoesNotContain("INACTIVE", text);
    }

    [Theory]
    [InlineData(ReportType.PanelSummary)]
    [InlineData(ReportType.PanelAntigram)]
    public void ImportedLotAwaitingReview_SaysItIsNotOfferedUntilActivated(ReportType type)
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Imported", "IMP-1", "V", 2, Day(90), false, isActive: false,
            sourceFormat: "csv", importedAt: "2026-11-20 09:00:00");

        var text = iso.Reports.GeneratePreviewText(type, panelId: id);

        Assert.Contains("Status:     INACTIVE - imported 2026-11-20 09:00:00 from csv; not offered for testing until " +
                        "reviewed and activated on the Panels tab.", text);
    }

    [Theory]
    [InlineData(ReportType.PanelSummary)]
    [InlineData(ReportType.PanelAntigram)]
    public void ExpiredLot_IsMarkedExpired(ReportType type)
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Old", "OLD-1", "V", 2, Day(-3), false);

        var text = iso.Reports.GeneratePreviewText(type, panelId: id);

        Assert.Contains($"Status:     EXPIRED {Day(-3)}; not offered for new testing.", text);
    }

    [Fact]
    public void DeactivatedLot_WithoutImport_IsMarkedInactive()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddPanel("Off", "OFF-1", "V", 2, Day(90), false, isActive: false);

        Assert.Contains("Status:     INACTIVE; not offered for new testing.",
            iso.Reports.GeneratePreviewText(ReportType.PanelSummary, panelId: id));
    }
}
