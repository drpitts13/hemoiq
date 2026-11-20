using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class ConfirmationOverrideTests
{
    [Fact]
    public void OverrideTexts_DescribeEachWarning_AndPromptsOnlyWhenTestingIsOpen()
    {
        Assert.Equal("Confirmed without an analysis.", ConfirmationOverrides.StaleAnalysis(null, neverAnalyzed: true));
        Assert.Equal("Confirmed with an out-of-date analysis: Grades changed.", ConfirmationOverrides.StaleAnalysis("Grades changed.", false));
        Assert.Null(ConfirmationOverrides.StaleAnalysis(null, false));
        Assert.Null(ConfirmationOverrides.OpenTestingPrompt(null));
        Assert.Null(ConfirmationOverrides.OpenTestingPrompt(Array.Empty<string>()));
        Assert.Contains("• Not yet ruled out: anti-K.", ConfirmationOverrides.OpenTestingPrompt(new[] { "Not yet ruled out: anti-K." }));
        Assert.Equal("Confirmed although the multiple-antibody separation criterion was not met for anti-E, anti-K.",
            ConfirmationOverrides.SeparationShortfall(new[] { "anti-E", "anti-K" }));
    }

    [Fact]
    public void Confirmation_RecordsOverrides_InItsAuditEntry_AndShowsThemOnTimelineWorksheetAndWorklist()
    {
        using var iso = new IsolatedDatabase();
        AppSettings.Current.RequireSupervisorReview = true;
        try
        {
            iso.Db.AddSpecimen("OV-1", "serum", null);
            var overrides = new[] { "Confirmed without an analysis.", "Confirmed with testing still open: Not yet ruled out: anti-Jka." };
            iso.Db.SetSpecimenFinalCall("OV-1", "anti-E", null, "TE", overrides: overrides);

            var audit = Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "OV-1"), e => e.Action == "confirm_final_call");
            Assert.Equal("Confirmed past 2 warning(s)", audit.Reason);
            Assert.Equal(overrides, ConfirmationOverrides.ForCurrentConfirmation(iso.Db, "OV-1"));

            var s = iso.Db.GetSpecimen("OV-1")!;
            Assert.Contains(InvestigationTimeline.Build(iso.Db, s),
                e => e.What.Contains("WARNINGS OVERRIDDEN: Confirmed without an analysis."));
            var report = new ReportService(iso.Db).GeneratePreviewText(ReportType.ClinicalIdentification, "OV-1");
            Assert.Contains("Warning overridden at confirmation: Confirmed with testing still open: Not yet ruled out: anti-Jka.", report);
            var review = Assert.Single(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.AwaitingSupervisorReview);
            Assert.Contains("Confirmed past 2 warning(s)", review.Detail);
        }
        finally
        {
            AppSettings.Current.RequireSupervisorReview = false;
        }
    }

    [Fact]
    public void ConfirmingWithoutWarnings_RecordsNoOverrides_AndARecleanConfirmationReplacesOldOnes()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("OV-2", "serum", null);
        iso.Db.SetSpecimenFinalCall("OV-2", "anti-K", null, "TE", overrides: new[] { "Confirmed without an analysis." });
        Assert.Single(ConfirmationOverrides.ForCurrentConfirmation(iso.Db, "OV-2"));

        iso.Db.ClearSpecimenFinalCall("OV-2", "re-test");
        Assert.Empty(ConfirmationOverrides.ForCurrentConfirmation(iso.Db, "OV-2"));

        iso.Db.SetSpecimenFinalCall("OV-2", "anti-K", null, "TE");
        Assert.Empty(ConfirmationOverrides.ForCurrentConfirmation(iso.Db, "OV-2"));
        var latest = iso.Db.GetAuditEventsForEntity("specimen", "OV-2").Where(e => e.Action == "confirm_final_call").OrderBy(e => e.Id).Last();
        Assert.Null(latest.Reason);
        Assert.DoesNotContain("overrides", latest.AfterJson);
        Assert.DoesNotContain(InvestigationTimeline.Build(iso.Db, iso.Db.GetSpecimen("OV-2")!).Where(e => e.What.StartsWith("Identification confirmed")).Skip(1),
            e => e.What.Contains("OVERRIDDEN"));
    }

    [Fact]
    public void Read_IgnoresMissingOrMalformedJson()
    {
        Assert.Empty(ConfirmationOverrides.Read(null));
        Assert.Empty(ConfirmationOverrides.Read("not json"));
        Assert.Empty(ConfirmationOverrides.Read("""{"antibodies":"anti-E"}"""));
        Assert.Equal(new[] { "x" }, ConfirmationOverrides.Read("""{"overrides":["x", "", 3]}"""));
    }
}
