using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Saving an analysis replaces an investigation's stored suspected and ruled-out lists and writes a snapshot, so a
/// view-only session may compute and view an analysis but not store one.
/// </summary>
[Collection("PersistentDatabase")]
public class ViewOnlyAnalysisTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static IsolatedDatabase Graded()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("VO", "L-VO", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("VO-1", "serum", null);
        iso.Db.LinkSpecimenPanel("VO-1", panel);
        iso.Db.SaveReaction("VO-1", panel, "1", "0", "0", "0", "2+");
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "ro", Initials = "RO", Name = "Reader", Role = UserRole.ReadOnly });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tech", Role = UserRole.Technologist });
        });
        return iso;
    }

    [Fact]
    public void ViewOnlySession_CannotStoreAnAnalysis_AndNothingIsWritten()
    {
        using var iso = Graded();
        As("ro", () =>
        {
            var ex = Assert.Throws<UnauthorizedAccessException>(() => iso.Analyzer.AnalyzeSpecimen("VO-1", updateDb: true));
            Assert.Contains("cannot record or change laboratory data", ex.Message);
        });
        Assert.Null(iso.Db.GetSpecimen("VO-1")!.LastAnalyzedAt);
        Assert.Null(iso.Db.GetLatestAnalysisSnapshot("VO-1"));
    }

    [Fact]
    public void ViewOnlySession_CanStillViewAnAnalysis()
    {
        using var iso = Graded();
        As("ro", () =>
        {
            var result = iso.Analyzer.AnalyzeSpecimen("VO-1", updateDb: false);
            Assert.NotNull(result);
        });
        Assert.Null(iso.Db.GetSpecimen("VO-1")!.LastAnalyzedAt);
    }

    [Fact]
    public void Technologist_StoresTheAnalysis()
    {
        using var iso = Graded();
        As("tech", () => iso.Analyzer.AnalyzeSpecimen("VO-1", updateDb: true));
        Assert.NotNull(iso.Db.GetSpecimen("VO-1")!.LastAnalyzedAt);
        Assert.NotNull(iso.Db.GetLatestAnalysisSnapshot("VO-1"));
    }
}
