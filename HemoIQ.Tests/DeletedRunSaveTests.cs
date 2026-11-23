using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A treated run can be deleted (for example from another workstation sharing the database) while grades typed for it
/// on the Reactions tab are still unsaved. Saving them must be refused with a message the user can act on, not a raw
/// database constraint error, and nothing may be written.
/// </summary>
public class DeletedRunSaveTests
{
    [Fact]
    public void SavingOnADeletedRun_IsRefusedInPlainWords_AndNothingIsWritten()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Del", "L-DEL", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("DR-1", "serum", null);
        iso.Db.LinkSpecimenPanel("DR-1", panel);
        var run = iso.Db.AddPanelRun("DR-1", panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin panel");
        iso.Db.DeletePanelRun(run);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            iso.Db.SaveReaction(run, "1", "0", "0", "2+", "NT"));
        Assert.Contains("no longer exists", ex.Message);
        Assert.DoesNotContain("FOREIGN KEY", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(iso.Db.GetReactions(run));
    }

    [Fact]
    public void SavingOnAnExistingRun_StillWorks()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Ok", "L-OK", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("DR-2", "serum", null);
        iso.Db.LinkSpecimenPanel("DR-2", panel);
        var run = iso.Db.AddPanelRun("DR-2", panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin panel");
        iso.Db.SaveReaction(run, "1", "0", "0", "2+", "NT");
        Assert.Contains(iso.Db.GetReactions(run), r => r.CellNumber == "1" && r.AHG == "2+");
    }
}
