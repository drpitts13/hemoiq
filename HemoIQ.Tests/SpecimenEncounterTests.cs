using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class SpecimenEncounterTests
{
    [Fact]
    public void Encounter_IsStoredWithoutSpaces_Audited_OnTimeline_AndNoOpWhenUnchanged()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EN-1", "serum", null);
        iso.Db.UpdateSpecimenEncounter("EN-1", "  V 1234 ");

        var s = iso.Db.GetSpecimen("EN-1")!;
        Assert.Equal("V1234", s.EncounterNumber);
        Assert.Equal("Encounter V1234", s.OriginDisplay);
        iso.Db.UpdateSpecimenEncounter("EN-1", "V1234");
        Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "EN-1"), e => e.Action == "update_encounter");
        Assert.Contains(InvestigationTimeline.Build(iso.Db, s), e => e.What == "Encounter / account recorded: V1234");

        iso.Db.UpdateSpecimenEncounter("EN-1", " ");
        s = iso.Db.GetSpecimen("EN-1")!;
        Assert.Null(s.EncounterNumber);
        Assert.Contains(InvestigationTimeline.Build(iso.Db, s), e => e.What == "Encounter / account cleared");
    }

    [Fact]
    public void Encounter_TooLong_IsRejected_AndLockedAfterFinalCall()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EN-2", "serum", null);
        var ex = Assert.Throws<ArgumentException>(() => iso.Db.UpdateSpecimenEncounter("EN-2", new string('9', 65)));
        Assert.Contains("longer than 64", ex.Message);

        iso.Db.UpdateSpecimenEncounter("EN-2", "A1");
        iso.Db.SetSpecimenFinalCall("EN-2", "anti-E", null, "TE");
        Assert.Throws<RecordLockedException>(() => iso.Db.UpdateSpecimenEncounter("EN-2", "A2"));
        iso.Db.UpdateSpecimenEncounter("EN-2", "A1"); // unchanged: allowed, writes nothing
    }

    [Fact]
    public void Search_FindsByEncounter_AndWorksheetPrintsIt()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EN-3", "serum", null);
        iso.Db.AddSpecimen("EN-4", "serum", null);
        iso.Db.UpdateSpecimenEncounter("EN-3", "ACCT-778899");
        iso.Db.UpdateSpecimenOrigin("EN-3", null, null, "ICU", null);

        Assert.Equal("EN-3", Assert.Single(InvestigationSearch.Search(iso.Db, "778899")).Accession);
        var report = new ReportService(iso.Db).GeneratePreviewText(ReportType.ClinicalIdentification, "EN-3");
        Assert.Contains("Origin: Encounter ACCT-778899; Ordered from ICU", report);
    }

    [Fact]
    public void Check_WarnsOnlyWhenTheEncounterIsOnAnotherPatientsSpecimen()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EN-5", "serum", null);
        iso.Db.UpdateSpecimenMrn("EN-5", "MRN-A");
        iso.Db.UpdateSpecimenEncounter("EN-5", "E100");
        iso.Db.AddSpecimen("EN-6", "serum", null);

        Assert.Null(EncounterCheck.Describe(iso.Db, "EN-6", "e100", "MRN-A"));  // same patient, case ignored
        Assert.Null(EncounterCheck.Describe(iso.Db, "EN-6", "E100", null));     // no MRN to compare
        Assert.Null(EncounterCheck.Describe(iso.Db, "EN-6", "E200", "MRN-B"));  // different encounter
        Assert.Null(EncounterCheck.Describe(iso.Db, "EN-5", "E100", "MRN-B"));  // only itself carries it

        var warning = EncounterCheck.Describe(iso.Db, "EN-6", " E100 ", "MRN-B");
        Assert.NotNull(warning);
        Assert.Contains("EN-5 (MRN MRN-A)", warning);
        Assert.Contains("this specimen has MRN MRN-B", warning);
    }

    [Fact]
    public void SaveService_SavesEncounter_AndRollsBackWithTheRestOnError()
    {
        using var iso = new IsolatedDatabase();
        SpecimenSaveService.Save(iso.Db, new SpecimenEdit { AccessionNumber = "EN-7", Type = "serum", EncounterNumber = "V77" }, isNew: true);
        Assert.Equal("V77", iso.Db.GetSpecimen("EN-7")!.EncounterNumber);

        Assert.ThrowsAny<Exception>(() => SpecimenSaveService.Save(iso.Db,
            new SpecimenEdit { AccessionNumber = "EN-8", Type = "serum", EncounterNumber = new string('x', 65) }, isNew: true));
        Assert.Null(iso.Db.GetSpecimen("EN-8"));
    }
}
