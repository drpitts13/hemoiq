using HemoIQ.Models;

namespace HemoIQ.Tests;

/// <summary>
/// A run's treatment decides how its negatives can be read, so a custom label never hides it: a label that does not
/// name the treatment gets it in parentheses on screen and in reports.
/// </summary>
public class RunLabelTreatmentTests
{
    private static PanelRun Run(string label, CellTreatment cell = CellTreatment.None, SerumTreatment serum = SerumTreatment.None) =>
        new() { Label = label, CellTreatment = cell, SerumTreatment = serum };

    [Fact]
    public void CustomLabel_WithoutTheTreatment_GetsItAppended()
    {
        Assert.Equal("Repeat (Ficin-treated)", Run("Repeat", CellTreatment.Ficin).DisplayLabel);
        Assert.Equal("Second look (DTT-treated + Absorbed: rr)",
            Run("Second look", CellTreatment.DTT, SerumTreatment.AlloAdsorptionRr).DisplayLabel);
    }

    [Theory]
    [InlineData("Ficin repeat", CellTreatment.Ficin, SerumTreatment.None)]
    [InlineData("Absorbed: rr (ce/ce)", CellTreatment.None, SerumTreatment.AlloAdsorptionRr)]
    [InlineData("Autoadsorbed x2", CellTreatment.None, SerumTreatment.AutoAdsorption)]
    [InlineData("DTT + R1R1 absorbed", CellTreatment.DTT, SerumTreatment.AlloAdsorptionR1R1)]
    [InlineData("Prewarm 37C", CellTreatment.None, SerumTreatment.Prewarmed)]
    public void CustomLabel_ThatNamesTheTreatment_IsUnchanged(string label, CellTreatment cell, SerumTreatment serum) =>
        Assert.Equal(label, Run(label, cell, serum).DisplayLabel);

    [Fact]
    public void CustomLabel_NamesOnlyOneOfTwoTreatments_GetsTheOtherAppended() =>
        Assert.Equal("Ficin run (Absorbed: R2R2)",
            Run("Ficin run", CellTreatment.Ficin, SerumTreatment.AlloAdsorptionR2R2).DisplayLabel);

    [Fact]
    public void UntreatedRun_KeepsItsCustomLabel() =>
        Assert.Equal("Initial", Run("Initial").DisplayLabel);

    [Fact]
    public void NoLabel_IsGeneratedFromTheTreatments() =>
        Assert.Equal("Papain-treated + Prewarmed serum", Run("", CellTreatment.Papain, SerumTreatment.Prewarmed).DisplayLabel);

    [Fact]
    public void RrKeyword_IsNotMatchedInsideAnotherWord() =>
        // "Error" contains "rr" but does not name the rr absorption.
        Assert.Equal("Error recheck (Absorbed: rr)", Run("Error recheck", serum: SerumTreatment.AlloAdsorptionRr).DisplayLabel);
}
