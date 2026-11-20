using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class OverrideAnalyticsTests
{
    private static AuditEvent Confirm(long id, string acc, string when, string? overridesJson) => new()
    {
        Id = id, Action = "confirm_final_call", EntityType = "specimen", EntityId = acc,
        OccurredAtUtc = when,
        AfterJson = overridesJson == null
            ? """{"antibodies":"anti-E","comment":null,"identifiedBy":"TE"}"""
            : $$"""{"antibodies":"anti-E","comment":null,"identifiedBy":"TE","overrides":{{overridesJson}}}""",
    };

    [Fact]
    public void Cases_CountOnlyOverriddenConfirmations_InRange_NewestFirst()
    {
        var events = new[]
        {
            Confirm(1, "A", "2026-11-02T12:00:00.0000000Z", """["Confirmed without an analysis."]"""),
            Confirm(2, "B", "2026-11-03T12:00:00.0000000Z", null),
            Confirm(3, "C", "2026-11-04T12:00:00.0000000Z", """["x", "y"]"""),
            Confirm(4, "D", "2026-09-01T12:00:00.0000000Z", """["old"]"""),
        };
        Specimen Current(string acc) => new() { AccessionNumber = acc, FinalAntibodies = "anti-E" };
        var cases = OverrideAnalytics.Cases(events, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), Current);

        Assert.Equal(new[] { "C", "A" }, cases.Select(c => c.Accession));
        Assert.Equal("anti-E", cases[0].FinalAntibodies);
        Assert.StartsWith("2 warning(s) overridden by TE: x y", cases[0].Detail);
        Assert.Equal(3, OverrideAnalytics.Cases(events, null, null, Current).Count);
    }

    [Fact]
    public void Cases_MarkConfirmationsThatAreNoLongerCurrent()
    {
        var events = new[]
        {
            Confirm(1, "A", "2026-11-02T12:00:00.0000000Z", """["first"]"""),
            Confirm(2, "A", "2026-11-05T12:00:00.0000000Z", null),
            Confirm(3, "B", "2026-11-05T12:00:00.0000000Z", """["cleared later"]"""),
        };
        Specimen? Lookup(string acc) => acc == "A"
            ? new Specimen { AccessionNumber = "A", FinalAntibodies = "anti-E" }
            : new Specimen { AccessionNumber = "B" }; // B was cleared
        var cases = OverrideAnalytics.Cases(events, null, null, Lookup);
        Assert.All(cases, c => Assert.StartsWith("No longer the current identification.", c.Detail));
        Assert.Equal(2, cases.Count);
    }

    [Fact]
    public void Cases_FromTheDatabase_FollowConfirmations()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("OA-1", "serum", null);
        iso.Db.AddSpecimen("OA-2", "serum", null);
        iso.Db.SetSpecimenFinalCall("OA-1", "anti-K", null, "TE", overrides: new[] { "Confirmed without an analysis." });
        iso.Db.SetSpecimenFinalCall("OA-2", "anti-E", null, "TE");
        var cases = OverrideAnalytics.Cases(iso.Db, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
        var c = Assert.Single(cases);
        Assert.Equal("OA-1", c.Accession);
        Assert.Equal("anti-K", c.FinalAntibodies);
        Assert.Contains("Confirmed without an analysis.", c.Detail);

        var csv = HemoIQ.ViewModels.AnalyticsViewModel.OverrideCsvLines(cases).ToList();
        Assert.Contains("Accession,Date,Final ID,Detail", csv);
        Assert.Contains(csv, l => l.StartsWith("\"OA-1\","));
    }

    [Fact]
    public void Cases_ForOneLogin_ListOnlyTheirOverrides_ButJudgeCurrencyAcrossEveryone()
    {
        AuditEvent By(AuditEvent e, string login) { e.Operator = login; return e; }
        var events = new[]
        {
            By(Confirm(1, "A", "2026-11-02T12:00:00.0000000Z", """["by tech"]"""), "TECH"),
            By(Confirm(2, "A", "2026-11-03T12:00:00.0000000Z", null), "super"), // later clean re-confirmation by someone else
            By(Confirm(3, "B", "2026-11-04T12:00:00.0000000Z", """["by other"]"""), "other"),
            new AuditEvent { Id = 4, Action = "save_override", EntityType = "specimen", EntityId = "C", Operator = "tech",
                             OccurredAtUtc = "2026-11-05T12:00:00.0000000Z", Reason = "Pregnancy for a male patient." },
        };
        Specimen Lookup(string acc) => new() { AccessionNumber = acc, FinalAntibodies = "anti-E" };

        var tech = OverrideAnalytics.Cases(events, null, null, Lookup, "tech");
        Assert.Equal(new[] { "C", "A" }, tech.Select(c => c.Accession));
        Assert.StartsWith("No longer the current identification.", tech.Single(c => c.Accession == "A").Detail);
        Assert.Equal("B", Assert.Single(OverrideAnalytics.Cases(events, null, null, Lookup, "other")).Accession);
        Assert.Equal(3, OverrideAnalytics.Cases(events, null, null, Lookup).Count);
    }
}
