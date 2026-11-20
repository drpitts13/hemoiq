using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class SaveOverrideTests
{
    [Fact]
    public void Save_PastWarnings_IsAudited_OnTimeline_AndCountedOnAnalytics()
    {
        using var iso = new IsolatedDatabase();
        const string warning = "Encounter E100 is already recorded on EN-5 (MRN MRN-A), but this specimen has MRN MRN-B.";
        SpecimenSaveService.Save(iso.Db, new SpecimenEdit
        {
            AccessionNumber = "SO-1", Type = "serum", Mrn = "MRN-B", EncounterNumber = "E100",
            SavedPastWarnings = new[] { warning, " " },
        }, isNew: true);

        var audit = Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "SO-1"), e => e.Action == "save_override");
        Assert.Equal(warning, audit.Reason);
        Assert.Contains(InvestigationTimeline.Build(iso.Db, iso.Db.GetSpecimen("SO-1")!),
            e => e.What == "Saved past a warning: " + warning);

        var c = Assert.Single(OverrideAnalytics.Cases(iso.Db, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)));
        Assert.Equal("SO-1", c.Accession);
        Assert.StartsWith("Specimen saved past a warning by ", c.Detail);
        Assert.EndsWith(warning, c.Detail);
    }

    [Fact]
    public void Save_WithoutWarnings_RecordsNoOverride()
    {
        using var iso = new IsolatedDatabase();
        SpecimenSaveService.Save(iso.Db, new SpecimenEdit { AccessionNumber = "SO-2", Type = "serum" }, isNew: true);
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("specimen", "SO-2"), e => e.Action == "save_override");
        Assert.Empty(OverrideAnalytics.Cases(iso.Db, null, null));
    }

    [Fact]
    public void FailedSave_RecordsNoOverride()
    {
        using var iso = new IsolatedDatabase();
        Assert.ThrowsAny<Exception>(() => SpecimenSaveService.Save(iso.Db, new SpecimenEdit
        {
            AccessionNumber = "SO-3", Type = "serum", EncounterNumber = new string('x', 65),
            SavedPastWarnings = new[] { "some warning" },
        }, isNew: true));
        Assert.DoesNotContain(iso.Db.GetAuditEvents(), e => e.Action == "save_override");
    }

    [Fact]
    public void Analytics_ListsConfirmationAndSaveOverrides_Together()
    {
        using var iso = new IsolatedDatabase();
        SpecimenSaveService.Save(iso.Db, new SpecimenEdit
        {
            AccessionNumber = "SO-4", Type = "serum", SavedPastWarnings = new[] { "The specimen records pregnant, but patient M is recorded as male." },
        }, isNew: true);
        iso.Db.SetSpecimenFinalCall("SO-4", "anti-E", null, "TE", overrides: new[] { "Confirmed without an analysis." });
        var cases = OverrideAnalytics.Cases(iso.Db, null, null);
        Assert.Equal(2, cases.Count);
        Assert.Contains(cases, c => c.Detail.Contains("recorded as male"));
        Assert.Contains(cases, c => c.Detail.Contains("Confirmed without an analysis."));
    }
}
