using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Crash recovery puts unsaved grades back only where they can be saved. A confirmed or cancelled investigation
/// cannot take them (the draft is discarded and the message says who can unlock it); a view-only session cannot save
/// them (the draft is kept for a login that can record).
/// </summary>
public class RecoveryBlockTests
{
    [Fact]
    public void OpenInvestigation_InARecordingSession_IsRestored()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RB-1", "serum", null);
        Assert.Null(ReactionsViewModel.RecoveryBlock(iso.Db, iso.Db.GetSpecimen("RB-1")!, canRecord: true));
    }

    [Fact]
    public void ConfirmedInvestigation_DiscardsTheDraft_AndSaysToClearFirst()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RB-2", "serum", null);
        iso.Db.SetSpecimenFinalCall("RB-2", "anti-K", null, "TE");
        var block = ReactionsViewModel.RecoveryBlock(iso.Db, iso.Db.GetSpecimen("RB-2")!, canRecord: true)!;
        Assert.Contains("confirmed", block.Why);
        Assert.Contains("Clear the identification first", block.NextStep);
        Assert.True(block.DeleteDraft);
    }

    [Fact]
    public void CancelledInvestigation_DiscardsTheDraft_AndSaysToReinstateFirst()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RB-3", "serum", null);
        iso.Db.CancelInvestigation("RB-3", "duplicate");
        var block = ReactionsViewModel.RecoveryBlock(iso.Db, iso.Db.GetSpecimen("RB-3")!, canRecord: true)!;
        Assert.Contains("cancelled", block.Why);
        Assert.Contains("Reinstate the investigation first", block.NextStep);
        Assert.True(block.DeleteDraft);
    }

    [Fact]
    public void ViewOnlySession_KeepsTheDraft_ForALoginThatCanRecord()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RB-4", "serum", null);
        var block = ReactionsViewModel.RecoveryBlock(iso.Db, iso.Db.GetSpecimen("RB-4")!, canRecord: false)!;
        Assert.Contains("view only", block.Why);
        Assert.Contains("login that can record", block.NextStep);
        Assert.False(block.DeleteDraft);
    }
}
