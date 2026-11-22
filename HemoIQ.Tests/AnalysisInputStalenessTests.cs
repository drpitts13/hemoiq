using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Every input the analysis reads must mark an analysed, open investigation out of date when it changes, including
/// inputs read from the same patient's other specimens (transfusion history, collection dates, earlier phenotype).
/// </summary>
[Collection("PersistentDatabase")]
public class AnalysisInputStalenessTests
{
    private static void Analyzed(IsolatedDatabase iso, string acc, string mrn)
    {
        var panel = iso.Db.AddPanel("P " + acc, "L-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.UpdateSpecimenMrn(acc, mrn);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale(acc));
    }

    [Fact]
    public void Hemolysis_MarksItsOwnAnalysisStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "ST-H", "MRN-H");
        iso.Db.UpdateSpecimenHemolysis("ST-H", SpecimenHemolysis.Moderate);
        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-H"));
    }

    [Fact]
    public void Collection_MarksItsOwnAndThePatientsOtherAnalysesStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "ST-C1", "MRN-C");
        Analyzed(iso, "ST-C2", "MRN-C");
        Analyzed(iso, "ST-X", "MRN-OTHER");
        var at = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm");
        iso.Db.UpdateSpecimenCollection("ST-C2", at, at);

        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-C2"));
        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-C1"));
        Assert.False(iso.Db.IsSpecimenAnalysisStale("ST-X"));
    }

    [Fact]
    public void Transfusion_OnAnotherSpecimen_MarksThePatientsOpenAnalysisStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "ST-T1", "MRN-T");
        Analyzed(iso, "ST-T2", "MRN-T");
        iso.Db.UpdateSpecimenTransfusion("ST-T2", TransfusionStatus.Transfused, DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"));
        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-T1"));
    }

    [Fact]
    public void Phenotype_OnAnotherSpecimen_MarksThePatientsOpenAnalysisStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "ST-P1", "MRN-P");
        Analyzed(iso, "ST-P2", "MRN-P");
        iso.Db.UpdateSpecimen("ST-P2", "serum", null, true, null, "D+C+E-c+e+");
        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-P1"));
    }

    [Fact]
    public void FreeTextTransfusionRequirements_MarkTheMrnsAnalysesStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "ST-R", "MRN-R");
        iso.Db.SavePatient(new Patient { Mrn = "MRN-R" });
        iso.Analyzer.AnalyzeSpecimen("ST-R", updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale("ST-R"));

        iso.Db.SavePatient(new Patient { Mrn = "MRN-R", TransfusionRequirements = "K-negative, irradiated" });
        Assert.True(iso.Db.IsSpecimenAnalysisStale("ST-R"));
    }

    [Fact]
    public void Fingerprint_IncludesHemolysis()
    {
        var runs = new List<PanelRun> { new() { RunId = 1, PanelId = 1 } };
        var none = new Specimen { AccessionNumber = "F", Hemolysis = SpecimenHemolysis.None };
        var gross = new Specimen { AccessionNumber = "F", Hemolysis = SpecimenHemolysis.Gross };
        Assert.NotEqual(
            AntibodyAnalyzer.ComputeInputFingerprint(new List<Reaction>(), runs, null, none),
            AntibodyAnalyzer.ComputeInputFingerprint(new List<Reaction>(), runs, null, gross));
    }
}
