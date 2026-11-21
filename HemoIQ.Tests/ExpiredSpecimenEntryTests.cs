using HemoIQ.Models;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Expired specimens are deactivated at start-up, but one that expires while the application is open stays active.
/// Saving grades on it must ask first, as an expired panel lot does.
/// </summary>
public class ExpiredSpecimenEntryTests
{
    private static readonly DateTime Today = new(2026, 11, 21);

    [Fact]
    public void ExpiredSpecimen_GetsAPromptNamingTheDate()
    {
        var prompt = ReactionsViewModel.ExpiredSpecimenPrompt(
            new Specimen { AccessionNumber = "EXP-1", ExpirationDate = "2026-11-20" }, Today);
        Assert.NotNull(prompt);
        Assert.Contains("Specimen EXP-1 expired 2026-11-20", prompt);
        Assert.Contains("Collect a new specimen", prompt);
        Assert.EndsWith("Record these reactions anyway?", prompt);
    }

    [Theory]
    [InlineData("2026-11-21")] // expires today: still in date
    [InlineData("2026-12-01")]
    [InlineData(null)]
    public void InDateOrUndatedSpecimen_HasNoPrompt(string? expiration) =>
        Assert.Null(ReactionsViewModel.ExpiredSpecimenPrompt(
            new Specimen { AccessionNumber = "OK-1", ExpirationDate = expiration }, Today));

    [Fact]
    public void NoSpecimen_HasNoPrompt() =>
        Assert.Null(ReactionsViewModel.ExpiredSpecimenPrompt(null, Today));

    [Fact]
    public void SavePath_ChecksTheSpecimenBeforeThePanel()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "ReactionsViewModel.cs"));
        var save = src[src.IndexOf("private bool TrySaveGrades(", StringComparison.Ordinal)..];
        var specimenCheck = save.IndexOf("ExpiredSpecimenPrompt(SelectedSpecimen", StringComparison.Ordinal);
        var panelCheck = save.IndexOf("SelectedPanel?.IsExpired == true", StringComparison.Ordinal);
        Assert.True(specimenCheck > 0 && specimenCheck < panelCheck);
        Assert.Contains("Not saved: the specimen has expired.", save);
    }
}
