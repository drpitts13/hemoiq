using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

public class AnalysisCurrencyTests
{
    private static Specimen S(string? analyzed, string? reactions = null, string? final = null) => new()
    {
        AccessionNumber = "AC-1", LastAnalyzedAt = analyzed, ReactionsUpdatedAt = reactions, FinalAntibodies = final,
    };

    [Fact]
    public void StaleReason_CoversReactionsAndConfiguration_ButNotNeverAnalyzedOrConfirmed()
    {
        Assert.Null(AnalysisCurrency.StaleReason(S(null, "2026-11-01T10:00:00"), "2026-11-02T10:00:00"));
        Assert.Null(AnalysisCurrency.StaleReason(S("2026-11-01T10:00:00", "2026-11-01T09:00:00"), "2026-10-01T10:00:00"));

        Assert.Equal("Reactions, panel typing or specimen details changed since last analysis.",
            AnalysisCurrency.StaleReason(S("2026-11-01T10:00:00", "2026-11-01T11:00:00"), null));
        Assert.Equal("Analysis rules or Preferences changed 2026-11-02 08:00:00, after the last analysis.",
            AnalysisCurrency.StaleReason(S("2026-11-01T10:00:00"), "2026-11-02T08:00:00"));
        var both = AnalysisCurrency.StaleReason(S("2026-11-01T10:00:00", "2026-11-01T11:00:00"), "2026-11-02T08:00:00")!;
        Assert.Contains("Reactions, panel typing or specimen details changed", both);
        Assert.Contains("rules or Preferences", both);

        Assert.Null(AnalysisCurrency.StaleReason(S("2026-11-01T10:00:00", final: "anti-K"), "2026-11-02T08:00:00"));
    }

    [Fact]
    public void ConfirmPrompt_OnlyWhenNeverAnalyzedOrStale()
    {
        Assert.Null(AnalysisViewModel.ConfirmStalePrompt(null, neverAnalyzed: false));
        Assert.Contains("has not been analyzed", AnalysisViewModel.ConfirmStalePrompt(null, neverAnalyzed: true));
        var stale = AnalysisViewModel.ConfirmStalePrompt("Reactions, panel typing or specimen details changed since last analysis.", neverAnalyzed: false)!;
        Assert.Contains("The analysis is out of date: Reactions, panel typing or specimen details changed since last analysis.", stale);
    }

    [Fact]
    public void StatusStrip_WorklistAndPendingReport_AgreeAfterRuleChange()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AC-S", "serum", null);
        var panel = iso.Db.AddPanel("AC P", "ACL", "V", 1, null, false, startTypedNegative: true);
        iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(panel)[0].Id, "k", "+");
        iso.Db.LinkSpecimenPanel("AC-S", panel);
        iso.Db.SaveReaction("AC-S", panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen("AC-S", updateDb: true);

        var current = InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("AC-S")!, requireReview: false);
        Assert.Equal(InvestigationStatusService.AwaitingAdditionalTesting, current.Label);
        Assert.Null(iso.Db.AnalysisStaleText("AC-S"));

        Thread.Sleep(1100); // last_analyzed_at has one-second resolution
        iso.Db.AddRule("Later rule", null, "anti-K", null, false, 2);

        var reason = iso.Db.AnalysisStaleText("AC-S");
        Assert.Contains("Analysis rules or Preferences changed", reason);
        var status = InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("AC-S")!, requireReview: false);
        Assert.Equal(InvestigationStatusService.AnalysisNeeded, status.Label);
        Assert.Contains("Analysis rules or Preferences changed", status.NextAction);
        Assert.False(status.Steps.Single(st => st.Name == "Analysis current").Done);
        Assert.Contains(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.StaleAnalysis && i.Detail == reason);
        var pending = iso.Reports.GeneratePreviewText(ReportType.PendingWork);
        Assert.Contains("STALE", pending.ToUpperInvariant());
        Assert.Contains("AC-S", pending);
    }
}
