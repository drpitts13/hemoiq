using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Specimen and patient details feed the analysis (transfusion and transplant decide whether autoadsorbed negatives
/// count, previous antibodies exempt separation, typing and DAT drive evidence and notes). Changing one after an
/// analysis marks it out of date, as a grade change does; untouched details, never-analysed, confirmed and
/// cancelled investigations are left alone.
/// </summary>
// Staleness compares each analysis with the global settings, so run apart from tests that change them.
[Collection("PersistentDatabase")]
public class SpecimenInputStalenessTests
{
    private const string Reason = "Reactions, panel typing or specimen details changed since last analysis.";

    private static IsolatedDatabase Analyzed(string acc, string? mrn = null)
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("SI", "SI-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, "serum", null);
        if (mrn != null) iso.Db.UpdateSpecimenMrn(acc, mrn);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale(acc));
        return iso;
    }

    private static void AssertStale(IsolatedDatabase iso, string acc)
    {
        Assert.True(iso.Db.IsSpecimenAnalysisStale(acc));
        Assert.Contains(Reason, iso.Db.AnalysisStaleText(acc));
    }

    [Fact]
    public void TransfusionChange_MarksTheAnalysisStale()
    {
        using var iso = Analyzed("SI-1");
        iso.Db.UpdateSpecimenTransfusion("SI-1", TransfusionStatus.Transfused, DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd"));
        AssertStale(iso, "SI-1");
    }

    [Fact]
    public void PhenotypeOrPreviousAntibodiesChange_MarksTheAnalysisStale()
    {
        using var iso = Analyzed("SI-2");
        iso.Db.UpdateSpecimen("SI-2", "serum", null, phenotype: "K-");
        AssertStale(iso, "SI-2");
    }

    [Fact]
    public void ExpirationOnlyChange_LeavesTheAnalysisCurrent()
    {
        using var iso = Analyzed("SI-3");
        iso.Db.UpdateSpecimen("SI-3", "serum", DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"));
        Assert.False(iso.Db.IsSpecimenAnalysisStale("SI-3"));
    }

    [Fact]
    public void DatAndPregnancyChanges_MarkTheAnalysisStale()
    {
        using var iso = Analyzed("SI-4");
        iso.Db.UpdateSpecimenDatSplit("SI-4", "1+", null);
        AssertStale(iso, "SI-4");

        using var iso2 = Analyzed("SI-5");
        iso2.Db.UpdateSpecimenPregnancy("SI-5", PregnancyStatus.Pregnant, null, DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"));
        AssertStale(iso2, "SI-5");
    }

    [Fact]
    public void PatientTransplantRecorded_MarksOpenInvestigationsOnTheMrnStale()
    {
        using var iso = Analyzed("SI-6", mrn: "MRN-SI");
        iso.Db.SavePatient(new Patient { Mrn = "MRN-SI", HpcTransplant = HpcTransplant.Allogeneic,
            HpcTransplantDate = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd") });
        AssertStale(iso, "SI-6");
    }

    [Fact]
    public void ConfirmedInvestigation_IsNotReopenedByAPatientChange()
    {
        using var iso = Analyzed("SI-7", mrn: "MRN-SC");
        iso.Db.SetSpecimenFinalCall("SI-7", "NAD", null, "TE");
        var before = iso.Db.GetSpecimen("SI-7")!.ReactionsUpdatedAt;

        iso.Db.SavePatient(new Patient { Mrn = "MRN-SC", AntigenNegative = "K" });

        Assert.Equal(before, iso.Db.GetSpecimen("SI-7")!.ReactionsUpdatedAt);
    }

    [Fact]
    public void NeverAnalyzedInvestigation_IsNotMarked()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("SI-8", "serum", null);
        iso.Db.UpdateSpecimenTransfusion("SI-8", TransfusionStatus.NotTransfused, null);
        Assert.Null(iso.Db.GetSpecimen("SI-8")!.ReactionsUpdatedAt);
    }
}
