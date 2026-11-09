using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class TransfusionProductsTests
{
    private static readonly string Specimen = DateTime.Today.ToString("yyyy-MM-dd");
    private static readonly string Recent = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd");

    private static TransfusionAssessment Assess(string? products) =>
        TransfusionContext.Assess(TransfusionStatus.Transfused, Recent, Specimen, 90, null, null, products);

    [Fact]
    public void PlateletsOrPlasmaOnly_LeavePhenotypeUsable_WithPassiveAntibodyNote()
    {
        var a = Assess("platelets,plasma");
        Assert.False(a.PhenotypeUnreliable);
        Assert.Contains("only Platelets, Plasma were recorded", a.Explanation);
        Assert.Contains("passively transfer antibodies", a.Explanation);

        var cryo = Assess("cryo");
        Assert.False(cryo.PhenotypeUnreliable);
        Assert.DoesNotContain("passively", cryo.Explanation);
    }

    [Fact]
    public void RedCellProducts_OrNoneRecorded_KeepPhenotypeUnreliable()
    {
        var rbc = Assess("rbc,plasma");
        Assert.True(rbc.PhenotypeUnreliable);
        Assert.Contains("Products recorded: Red cells, Plasma.", rbc.Explanation);
        Assert.True(Assess("granulocytes").PhenotypeUnreliable);
        Assert.True(Assess(null).PhenotypeUnreliable);
        Assert.True(Assess("unknown-thing").PhenotypeUnreliable);
    }

    [Fact]
    public void Parse_KeepsKnownCodesInOrder_Format_ReturnsNullWhenEmpty()
    {
        Assert.Equal(new[] { TransfusionProducts.RedCells, TransfusionProducts.Platelets },
            TransfusionProducts.Parse(" Platelets ; rbc , bogus"));
        Assert.Null(TransfusionProducts.Format(Array.Empty<string>()));
        Assert.Equal("rbc,cryo", TransfusionProducts.Format(new[] { "cryo", "rbc", "rbc" }));
    }

    [Fact]
    public void Update_PersistsAudits_ClearsWhenNotTransfused_RejectsBadUnits_AndDisplays()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("TP-1", "serum", null);
        iso.Db.UpdateSpecimenTransfusion("TP-1", TransfusionStatus.Transfused, Recent);
        iso.Db.UpdateSpecimenTransfusionProducts("TP-1", new[] { "platelets" }, 2);

        var s = iso.Db.GetSpecimen("TP-1")!;
        Assert.Equal("platelets", s.TransfusionProducts);
        Assert.Equal(2, s.TransfusionUnits);
        Assert.Equal($"Transfused (last {Recent}; Platelets; 2 unit(s))", s.TransfusionDisplay);
        Assert.Contains(InvestigationTimeline.Build(iso.Db, s), e => e.What == "Transfused products recorded: Platelets, 2 unit(s)");

        iso.Db.UpdateSpecimenTransfusionProducts("TP-1", new[] { "platelets" }, 2);
        Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "TP-1"), e => e.Action == "update_transfusion_products");
        Assert.Throws<ArgumentException>(() => iso.Db.UpdateSpecimenTransfusionProducts("TP-1", null, 0));

        iso.Db.UpdateSpecimenTransfusion("TP-1", TransfusionStatus.NotTransfused, null);
        iso.Db.UpdateSpecimenTransfusionProducts("TP-1", new[] { "rbc" }, 1);
        s = iso.Db.GetSpecimen("TP-1")!;
        Assert.Null(s.TransfusionProducts);
        Assert.Null(s.TransfusionUnits);
    }

    [Fact]
    public void Analysis_UsesRecordedProducts_ForPatientTyping()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("TP-2", "serum", null, phenotype: "E-");
        var panel = iso.Db.AddPanel("TP P", "TPL", "V", 1, null, false, startTypedNegative: true);
        iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(panel)[0].Id, "k", "+");
        iso.Db.LinkSpecimenPanel("TP-2", panel);
        iso.Db.SaveReaction("TP-2", panel, "1", "0", "0", "0", "2+");
        iso.Db.UpdateSpecimenTransfusion("TP-2", TransfusionStatus.Transfused, Recent);

        Assert.True(iso.Analyzer.AnalyzeSpecimen("TP-2", updateDb: false).PatientPhenotypeUnreliable);
        iso.Db.UpdateSpecimenTransfusionProducts("TP-2", new[] { "plasma" }, null);
        var after = iso.Analyzer.AnalyzeSpecimen("TP-2", updateDb: false);
        Assert.False(after.PatientPhenotypeUnreliable);
        Assert.Contains("passively transfer antibodies", after.TransfusionContextNote);
        Assert.Contains(after.PatientTypingConsiderations, c => c.Antibody == "anti-E" && c.Kind == PatientTypingKind.Supporting);
    }
}
