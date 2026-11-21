using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The analysis reads the patient's record and history (prior confirmed antibodies on other specimens), so moving a
/// specimen to another MRN, or confirming or clearing an identification on another specimen of the same patient,
/// marks an analysed, open specimen out of date.
/// </summary>
// Staleness compares each analysis with the global settings, so run apart from tests that change them.
[Collection("PersistentDatabase")]
public class PatientContextStalenessTests
{
    private static void Analyzed(IsolatedDatabase iso, string acc, string mrn)
    {
        var panel = iso.Db.AddPanel("PC " + acc, "PC-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, "serum", null);
        iso.Db.UpdateSpecimenMrn(acc, mrn);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
    }

    [Fact]
    public void MovingASpecimenToAnotherMrn_MarksItsAnalysisStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "PC-1", "MRN-A");
        Assert.False(iso.Db.IsSpecimenAnalysisStale("PC-1"));

        iso.Db.UpdateSpecimenMrn("PC-1", "MRN-B");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("PC-1"));
    }

    [Fact]
    public void ConfirmingAnotherSpecimenOfThePatient_MarksTheOpenOneStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "PC-OLD", "MRN-C");
        Analyzed(iso, "PC-NEW", "MRN-C");
        Assert.False(iso.Db.IsSpecimenAnalysisStale("PC-NEW"));

        iso.Db.SetSpecimenFinalCall("PC-OLD", "anti-K", null, "TE");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("PC-NEW"));
    }

    [Fact]
    public void ClearingAnotherSpecimensIdentification_MarksTheOpenOneStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "PC-A", "MRN-D");
        iso.Db.SetSpecimenFinalCall("PC-A", "anti-E", null, "TE");
        Analyzed(iso, "PC-B", "MRN-D");
        Assert.False(iso.Db.IsSpecimenAnalysisStale("PC-B"));

        iso.Db.ClearSpecimenFinalCall("PC-A", "Wrong patient sample");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("PC-B"));
    }

    [Fact]
    public void ConfirmingASpecimenOfADifferentPatient_LeavesOthersCurrent()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "PC-X", "MRN-E");
        Analyzed(iso, "PC-Y", "MRN-F");

        iso.Db.SetSpecimenFinalCall("PC-X", "anti-K", null, "TE");

        Assert.False(iso.Db.IsSpecimenAnalysisStale("PC-Y"));
    }
}
