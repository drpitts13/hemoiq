using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A message that names a next step must not send the user to a command their role refuses. A Technologist told to
/// "click Clear" or "Reinstate" gets a refusal; the message names who can do it instead.
/// </summary>
[Collection("PersistentDatabase")]
public class RoleAwareNextStepTests
{
    private static T As<T>(string login, Func<T> body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; return body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static void As(string login, Action body) => As(login, () => { body(); return 0; });

    /// <summary>Registry with admin "adm", supervisor "sup" and technologist "tech"; NS-1 confirmed, NS-2 cancelled.</summary>
    private static IsolatedDatabase WithRegistry()
    {
        var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("NS-1", "serum", null);
        iso.Db.AddSpecimen("NS-2", "serum", null);
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "sup", Initials = "SU", Name = "Sup", Role = UserRole.Supervisor });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tech", Role = UserRole.Technologist });
        });
        As("tech", () =>
        {
            iso.Db.SetSpecimenFinalCall("NS-1", "anti-E", null, "TE");
            var panel = iso.Db.AddPanel("NS", "L-NS", "V", 2, null, false, startTypedNegative: true);
            iso.Db.LinkSpecimenPanel("NS-2", panel);
            iso.Db.SaveReaction("NS-2", panel, "1", "0", "0", "0", "2+");
            iso.Db.CancelInvestigation("NS-2", "duplicate request");
        });
        return iso;
    }

    [Theory]
    [InlineData(Permission.ClearIdentification, "a Supervisor or Administrator")]
    [InlineData(Permission.ManageUsers, "an Administrator")]
    [InlineData(Permission.RecordResults, "a Technologist or Supervisor or Administrator")]
    public void WhoCan_NamesTheRolesWithTheRightArticle(Permission p, string expected) =>
        Assert.Equal(expected, AccessControl.WhoCan(p));

    [Fact]
    public void NoRegistry_KeepsTheDirectStep()
    {
        using var iso = new IsolatedDatabase();
        Assert.Equal("Click Clear.", AccessControl.NextStep(iso.Db, Permission.ClearIdentification, "Click Clear.", "clear it."));
    }

    [Fact]
    public void ConfirmingOverAConfirmedCall_TellsATechnologistWhoToAsk()
    {
        using var iso = WithRegistry();
        var tech = As("tech", () => Assert.Throws<RecordLockedException>(() => iso.Db.SetSpecimenFinalCall("NS-1", "anti-c", null, "TE")));
        Assert.Contains("Ask a Supervisor or Administrator to clear it with a reason", tech.Message);
        Assert.DoesNotContain("click Clear", tech.Message);
        Assert.Contains("amendment history", tech.Message);

        var sup = As("sup", () => Assert.Throws<RecordLockedException>(() => iso.Db.SetSpecimenFinalCall("NS-1", "anti-c", null, "SU")));
        Assert.Contains("click Clear on the Analysis tab", sup.Message);
    }

    [Fact]
    public void EditingAConfirmedRecord_TellsATechnologistWhoToAsk()
    {
        using var iso = WithRegistry();
        var tech = As("tech", () => Assert.Throws<RecordLockedException>(() => iso.Db.EnsureSpecimenUnlocked("NS-1")));
        Assert.Contains("Ask a Supervisor or Administrator to clear the confirmation", tech.Message);
        var sup = As("sup", () => Assert.Throws<RecordLockedException>(() => iso.Db.EnsureSpecimenUnlocked("NS-1")));
        Assert.StartsWith("Specimen NS-1 has a confirmed identification and is locked. Clear the confirmation", sup.Message);
    }

    [Fact]
    public void AnalyzingACancelledInvestigation_NamesTheCancellation_AndWhoCanReinstate()
    {
        using var iso = WithRegistry();
        var tech = As("tech", () => Assert.Throws<RecordLockedException>(() => iso.Analyzer.AnalyzeSpecimen("NS-2", updateDb: true)));
        Assert.Contains("is cancelled and locked", tech.Message);
        Assert.DoesNotContain("confirmed identification", tech.Message);
        Assert.Contains("Ask a Supervisor or Administrator to reinstate it", tech.Message);

        var sup = As("sup", () => Assert.Throws<RecordLockedException>(() => iso.Analyzer.AnalyzeSpecimen("NS-2", updateDb: true)));
        Assert.Contains("Reinstate it with a reason before editing", sup.Message);
        Assert.True(sup.Cancelled);
    }
}
