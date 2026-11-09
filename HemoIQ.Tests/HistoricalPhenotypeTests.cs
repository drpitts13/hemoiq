using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class HistoricalPhenotypeTests
{
    private static string Day(int daysAgo) => DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

    /// <summary>Current specimen transfused 5 days ago with a now-unreliable phenotype.</summary>
    private static Specimen Current(IsolatedDatabase iso, string acc = "HP-NOW", string? lastTransfusion = null)
    {
        iso.Db.AddSpecimen(acc, "serum", null, phenotype: "E-", createdDate: Day(0));
        iso.Db.UpdateSpecimenMrn(acc, "MRN-H");
        iso.Db.UpdateSpecimenTransfusion(acc, TransfusionStatus.Transfused, lastTransfusion ?? Day(5));
        return iso.Db.GetSpecimen(acc)!;
    }

    private static void Earlier(IsolatedDatabase iso, string acc, string phenotype, int daysAgo, string? transfusedOn = null)
    {
        iso.Db.AddSpecimen(acc, "serum", null, phenotype: phenotype, createdDate: Day(daysAgo));
        iso.Db.UpdateSpecimenMrn(acc, "MRN-H");
        if (transfusedOn != null) iso.Db.UpdateSpecimenTransfusion(acc, TransfusionStatus.Transfused, transfusedOn);
        else iso.Db.UpdateSpecimenTransfusion(acc, TransfusionStatus.NotTransfused, null);
    }

    private static PatientTyping TypingFor(Specimen s) =>
        PatientTypingParser.Parse(s.Phenotype, null, null, null,
            TransfusionContext.Assess(s.TransfusionStatus, s.LastTransfusionDate, s.CreatedDate, 90, s.Phenotype, s.Notes));

    [Fact]
    public void PreTransfusionPhenotype_IsUsed_WhenTheCurrentOneIsUnreliable()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "HP-OLD", "E+ K-", daysAgo: 200);
        var now = Current(iso);

        var found = HistoricalPhenotype.Find(iso.Db, now, TypingFor(now), 90);
        Assert.NotNull(found);
        Assert.Equal("HP-OLD", found!.Specimen.AccessionNumber);
        var e = Assert.Single(found.Considerations, c => c.Antibody == "anti-E");
        Assert.Equal(PatientTypingKind.Against, e.Kind);
        Assert.StartsWith("Earlier phenotype from HP-OLD (" + Day(200), e.Explanation);
        Assert.Equal(PatientTypingKind.Supporting, found.Considerations.Single(c => c.Antibody == "anti-K").Kind);
    }

    [Fact]
    public void Analysis_ListsTheEarlierTyping_AndExplainsItsUse()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "HP-OLD2", "E+", daysAgo: 300);
        Current(iso, "HP-NOW2");
        var panel = iso.Db.AddPanel("HP P", "HPL", "V", 1, null, false, startTypedNegative: true);
        iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(panel)[0].Id, "k", "+");
        iso.Db.LinkSpecimenPanel("HP-NOW2", panel);
        iso.Db.SaveReaction("HP-NOW2", panel, "1", "0", "0", "0", "0");

        var result = iso.Analyzer.AnalyzeSpecimen("HP-NOW2", updateDb: false);
        Assert.True(result.PatientPhenotypeUnreliable);
        Assert.Contains(result.PatientTypingConsiderations, c => c.Antibody == "anti-E" && c.Kind == PatientTypingKind.Against
                                                                && c.Explanation.Contains("HP-OLD2"));
        Assert.Contains("earlier phenotype from HP-OLD2", result.TransfusionContextNote);
        Assert.Contains("is used as patient-typing evidence instead", result.TransfusionContextNote);
    }

    [Fact]
    public void NotUsed_WhenTheEarlierSpecimenFollowsTheTransfusion_OrWasItselfRecentlyTransfused()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "HP-AFTER", "E+", daysAgo: 2);                                   // after the transfusion 5 days ago
        Earlier(iso, "HP-TX", "E+", daysAgo: 30, transfusedOn: Day(35));             // its own typing was unreliable
        var now = Current(iso);
        Assert.Null(HistoricalPhenotype.Find(iso.Db, now, TypingFor(now), 90));

        Earlier(iso, "HP-GOOD", "E-", daysAgo: 400);
        var found = HistoricalPhenotype.Find(iso.Db, now, TypingFor(now), 90);
        Assert.Equal("HP-GOOD", found!.Specimen.AccessionNumber);
    }

    [Fact]
    public void NotUsed_WithoutATransfusionDate_AfterAllogeneicTransplant_OrWhenCurrentTypingIsReliable()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "HP-OLD3", "E+", daysAgo: 200);
        iso.Db.AddSpecimen("HP-NODATE", "serum", null, phenotype: "E-", createdDate: Day(0));
        iso.Db.UpdateSpecimenMrn("HP-NODATE", "MRN-H");
        iso.Db.UpdateSpecimenTransfusion("HP-NODATE", TransfusionStatus.Transfused, null);
        var noDate = iso.Db.GetSpecimen("HP-NODATE")!;
        Assert.Null(HistoricalPhenotype.Find(iso.Db, noDate, TypingFor(noDate), 90));

        iso.Db.AddSpecimen("HP-OK", "serum", null, phenotype: "E-", createdDate: Day(0));
        iso.Db.UpdateSpecimenMrn("HP-OK", "MRN-H");
        iso.Db.UpdateSpecimenTransfusion("HP-OK", TransfusionStatus.NotTransfused, null);
        var ok = iso.Db.GetSpecimen("HP-OK")!;
        Assert.Null(HistoricalPhenotype.Find(iso.Db, ok, TypingFor(ok), 90)); // current E- is usable

        var now = Current(iso, "HP-HPC");
        iso.Db.SavePatient(new Patient { Mrn = "MRN-H", HpcTransplant = HpcTransplant.Allogeneic });
        Assert.Null(HistoricalPhenotype.Find(iso.Db, now, TypingFor(now), 90));
    }
}
