using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The input fingerprint printed with each analysis must change whenever any input the analysis reads changes,
/// so two reports with the same fingerprint were computed from the same data.
/// </summary>
[Collection("PersistentDatabase")]
public class InputFingerprintTests
{
    private static (List<Reaction> Rx, List<PanelRun> Runs, Dictionary<int, List<PanelCell>> Cells, Specimen S, Patient P) Inputs()
    {
        var runs = new List<PanelRun> { new() { RunId = 1, PanelId = 7, CellTreatment = CellTreatment.None, SerumTreatment = SerumTreatment.None } };
        var rx = new List<Reaction>
        {
            new() { RunId = 1, CellNumber = "1", IS = "0", C37 = "0", AHG = "2+", CC = "NT",
                    ExtraPhases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["LISS"] = "1+" } },
        };
        var cell = new PanelCell { CellNumber = "1" };
        cell.Antigens["E"] = "+";
        cell.Antigens["e"] = "-";
        var cells = new Dictionary<int, List<PanelCell>> { [7] = new() { cell } };
        var specimen = new Specimen { AccessionNumber = "FP-1", Mrn = "M1", DatIgg = "0", TransfusionStatus = TransfusionStatus.NotTransfused };
        var patient = new Patient { Mrn = "M1", AntigenNegative = "K" };
        return (rx, runs, cells, specimen, patient);
    }

    private static string Fp((List<Reaction> Rx, List<PanelRun> Runs, Dictionary<int, List<PanelCell>> Cells, Specimen S, Patient P) i) =>
        AntibodyAnalyzer.ComputeInputFingerprint(i.Rx, i.Runs, i.Cells, i.S, i.P);

    [Fact]
    public void SameInputs_GiveTheSameFingerprint()
    {
        Assert.Equal(Fp(Inputs()), Fp(Inputs()));
        Assert.Equal(64, Fp(Inputs()).Length);
    }

    [Fact]
    public void EachInputTheAnalysisReads_ChangesTheFingerprint()
    {
        var baseline = Fp(Inputs());

        var extra = Inputs(); extra.Rx[0].ExtraPhases["LISS"] = "2+";
        var typing = Inputs(); typing.Cells[7][0].Antigens["e"] = "+";
        var antigenCase = Inputs(); antigenCase.Cells[7][0].Antigens.Remove("e"); antigenCase.Cells[7][0].Antigens["E2"] = "-";
        var dat = Inputs(); dat.S.DatIgg = "2+";
        var transfusion = Inputs(); transfusion.S.TransfusionStatus = TransfusionStatus.Transfused;
        var patient = Inputs(); patient.P.AntigenNegative = "K, E";
        var grade = Inputs(); grade.Rx[0].AHG = "1+";

        foreach (var changed in new[] { extra, typing, antigenCase, dat, transfusion, patient, grade })
            Assert.NotEqual(baseline, Fp(changed));
    }

    [Fact]
    public void ReactionsOnlyForm_StillWorks_AndDiffersFromTheFullForm()
    {
        var i = Inputs();
        var reactionsOnly = AntibodyAnalyzer.ComputeInputFingerprint(i.Rx, i.Runs);
        Assert.Equal(64, reactionsOnly.Length);
        Assert.NotEqual(reactionsOnly, Fp(i));
    }

    [Fact]
    public void EditingPanelTyping_ChangesTheNextAnalysisFingerprint()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("FP-2", "serum", null);
        var panel = iso.Db.AddPanel("P", "L-FP", "V", 2, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel("FP-2", panel);
        iso.Db.SaveReaction("FP-2", panel, "1", "0", "0", "2+", "NT");
        iso.Analyzer.AnalyzeSpecimen("FP-2", updateDb: true);
        var first = iso.Db.GetLatestAnalysisSnapshot("FP-2")!.InputFingerprint;

        iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(panel)[0].Id, "K", "+");
        iso.Analyzer.AnalyzeSpecimen("FP-2", updateDb: true);

        Assert.NotEqual(first, iso.Db.GetLatestAnalysisSnapshot("FP-2")!.InputFingerprint);
    }
}
