using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// The Add run dialog must describe each treatment exactly as analysis applies it, and must never record one run's
/// grades as another run's results: a treated run pre-filled with untreated grades would claim the treatment
/// changed nothing.
/// </summary>
public class RunTreatmentTextTests
{
    [Fact]
    public void Ficin_ListsDestroyedAndEnhancedAntigens_FromTheEngineTables()
    {
        var text = RunTreatmentText.Describe(CellTreatment.Ficin, SerumTreatment.None);
        var destroys = text[(text.IndexOf("destroys ", StringComparison.Ordinal) + 9)..text.IndexOf(". enhances", StringComparison.Ordinal)]
            .Split(", ");
        var enhances = text[(text.IndexOf("enhances ", StringComparison.Ordinal) + 9)..].TrimEnd('.').Split(", ");

        Assert.Equal(AntigenTreatmentEffects.AntigensWithEffect(CellTreatment.Ficin, AntigenEffect.Destroyed), destroys);
        Assert.Contains("Fya", destroys);
        Assert.Contains("Inb", destroys);
        // Lewis antigens are enhanced by enzymes, not destroyed (Iteration 166).
        Assert.DoesNotContain("Lea", destroys);
        Assert.Contains("Lea", enhances);
        Assert.Contains("Leb", enhances);
        Assert.Equal(text, RunTreatmentText.Describe(CellTreatment.Papain, SerumTreatment.None));
    }

    [Fact]
    public void Dtt_AndSerumTreatments_UseTheAnalysisLists()
    {
        Assert.Contains("destroys K, k, Kpa, Kpb", RunTreatmentText.Describe(CellTreatment.DTT, SerumTreatment.None));
        Assert.Contains("Removes anti-c, anti-e, anti-f", RunTreatmentText.Describe(CellTreatment.None, SerumTreatment.AlloAdsorptionRr));
        Assert.Contains("Removes anti-D, anti-C, anti-e", RunTreatmentText.Describe(CellTreatment.None, SerumTreatment.AlloAdsorptionR1R1));
        Assert.Contains("IS phase not interpretable", RunTreatmentText.Describe(CellTreatment.None, SerumTreatment.Prewarmed));
        Assert.Equal("", RunTreatmentText.Describe(CellTreatment.None, SerumTreatment.None));
    }

    [Theory]
    [InlineData(CellTreatment.None, SerumTreatment.None, true)]
    [InlineData(CellTreatment.Ficin, SerumTreatment.None, false)]
    [InlineData(CellTreatment.DTT, SerumTreatment.None, false)]
    [InlineData(CellTreatment.None, SerumTreatment.Prewarmed, false)]
    [InlineData(CellTreatment.None, SerumTreatment.AutoAdsorption, false)]
    public void Grades_AreCopiedOnlyIntoAnUntreatedRepeat(CellTreatment cell, SerumTreatment serum, bool allowed) =>
        Assert.Equal(allowed, RunTreatmentText.CanCopyGrades(cell, serum));

    [Fact]
    public void AddRun_NeverSavesCopiedGrades_AndTheOptionStartsOff()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var vm = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var addRun = vm[vm.IndexOf("private void AddRun()", StringComparison.Ordinal)..];
        addRun = addRun[..addRun.IndexOf("private int PrefillUnsaved(", StringComparison.Ordinal)];
        Assert.DoesNotContain("SaveReaction(", addRun);
        Assert.Contains("RunTreatmentText.CanCopyGrades(", addRun);
        Assert.Contains("PrefillUnsaved(", addRun);

        var xaml = File.ReadAllText(Path.Combine(dir.FullName, "HemoIQ", "Views", "Dialogs", "PanelRunDialog.xaml"));
        Assert.Matches("x:Name=\"CopyGradesCheck\"[^>]*IsChecked=\"False\"", xaml);
    }
}
