using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class SpecimenHemolysisTests
{
    private static Reaction R(string cell, string ahg, string c37 = "0") =>
        new() { CellNumber = cell, IS = "0", C37 = c37, AHG = ahg, CC = "NT" };

    [Fact]
    public void ReviewNotes_GrossAlways_SlightOrModerateOnlyWithHGrades_NoneOrBlankNever()
    {
        var plain = new[] { R("1", "2+"), R("2", "0") };
        var withH = new[] { R("1", "2+"), R("3", "0", c37: "H") };

        var gross = Assert.Single(SpecimenHemolysis.ReviewNotes("gross", plain));
        Assert.Contains("grossly hemolyzed", gross);
        Assert.Contains("non-hemolyzed specimen", gross);
        Assert.Empty(SpecimenHemolysis.ReviewNotes("moderate", plain));
        Assert.Contains("cell(s) 3", Assert.Single(SpecimenHemolysis.ReviewNotes("moderate", withH)));
        Assert.Contains("slight hemolyzed", Assert.Single(SpecimenHemolysis.ReviewNotes("Slight", withH)));
        Assert.Empty(SpecimenHemolysis.ReviewNotes("none", withH));
        Assert.Empty(SpecimenHemolysis.ReviewNotes(null, withH));
    }

    [Fact]
    public void Normalize_AcceptsCommonWordings_UnknownIsNotRecorded()
    {
        Assert.Equal(SpecimenHemolysis.Gross, SpecimenHemolysis.Normalize("Severe"));
        Assert.Equal(SpecimenHemolysis.Slight, SpecimenHemolysis.Normalize("mild"));
        Assert.Equal(SpecimenHemolysis.NotRecorded, SpecimenHemolysis.Normalize("pink-ish"));
        Assert.Equal("Moderate", SpecimenHemolysis.Label("moderate"));
        Assert.Equal("Not recorded", SpecimenHemolysis.Label(null));
    }

    [Fact]
    public void Update_PersistsAudits_NoOpWritesNothing_LockedAfterConfirmation()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("HM-1", "serum", null);
        iso.Db.UpdateSpecimenHemolysis("HM-1", "Gross");
        Assert.Equal(SpecimenHemolysis.Gross, iso.Db.GetSpecimen("HM-1")!.Hemolysis);
        iso.Db.UpdateSpecimenHemolysis("HM-1", "gross");
        var audit = Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "HM-1"), e => e.Action == "update_hemolysis");
        Assert.Contains("\"hemolysis\":\"gross\"", audit.AfterJson);
        Assert.Contains(InvestigationTimeline.Build(iso.Db, iso.Db.GetSpecimen("HM-1")!),
            e => e.What == "Visual hemolysis recorded: Gross");

        iso.Db.SetSpecimenFinalCall("HM-1", "anti-K", null, "TE");
        Assert.Throws<RecordLockedException>(() => iso.Db.UpdateSpecimenHemolysis("HM-1", "none"));
    }

    [Fact]
    public void Analysis_AddsReviewNote_ForGrossHemolysis_AndReportPrintsIt()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("HM-2", "serum", null);
        var panel = iso.Db.AddPanel("HM P", "HML", "V", 2, null, false, startTypedNegative: true);
        foreach (var c in iso.Db.GetPanelCells(panel)) iso.Db.UpdatePanelCellAntigen(c.Id, "k", "+");
        iso.Db.LinkSpecimenPanel("HM-2", panel);
        iso.Db.SaveReaction("HM-2", panel, "1", "0", "0", "0", "2+");

        Assert.DoesNotContain(iso.Analyzer.AnalyzeSpecimen("HM-2", updateDb: false).TransfusionRequirementNotes,
            n => n.Contains("hemolyzed"));
        iso.Db.UpdateSpecimenHemolysis("HM-2", "gross");
        var result = iso.Analyzer.AnalyzeSpecimen("HM-2", updateDb: false);
        Assert.Contains(result.TransfusionRequirementNotes, n => n.Contains("grossly hemolyzed"));
        Assert.Contains(result.Suggestions, s => s.StartsWith("REVIEW:") && s.Contains("grossly hemolyzed"));

        Assert.Contains("Visual hemolysis: Gross",
            iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "HM-2"));
    }
}
