using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The source specimen's analysis lists its eluates' findings, so linking, unlinking, analysing or confirming an
/// eluate marks an analysed, open source out of date. A confirmed source is not reopened.
/// </summary>
public class EluateSourceStalenessTests
{
    private static int Analyzed(IsolatedDatabase iso, string acc, string type)
    {
        var panel = iso.Db.AddPanel("EL " + acc, "EL-" + acc, "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen(acc, type, null);
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen(acc, updateDb: true);
        return panel;
    }

    [Fact]
    public void LinkingAnEluate_MarksTheSourceStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "SRC-1", "serum");
        Analyzed(iso, "ELU-1", "eluate");
        Assert.False(iso.Db.IsSpecimenAnalysisStale("SRC-1"));

        iso.Db.SetEluateSource("ELU-1", "SRC-1");

        Assert.True(iso.Db.IsSpecimenAnalysisStale("SRC-1"));
    }

    [Fact]
    public void AnalysingALinkedEluate_MarksTheSourceStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "SRC-2", "serum");
        Analyzed(iso, "ELU-2", "eluate");
        iso.Db.SetEluateSource("ELU-2", "SRC-2");
        iso.Analyzer.AnalyzeSpecimen("SRC-2", updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale("SRC-2"));

        iso.Analyzer.AnalyzeSpecimen("ELU-2", updateDb: true);

        Assert.True(iso.Db.IsSpecimenAnalysisStale("SRC-2"));
    }

    [Fact]
    public void UnlinkingAnEluate_MarksTheFormerSourceStale()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "SRC-3", "serum");
        Analyzed(iso, "ELU-3", "eluate");
        iso.Db.SetEluateSource("ELU-3", "SRC-3");
        iso.Analyzer.AnalyzeSpecimen("SRC-3", updateDb: true);

        iso.Db.SetEluateSource("ELU-3", null);

        Assert.True(iso.Db.IsSpecimenAnalysisStale("SRC-3"));
    }

    [Fact]
    public void ConfirmedSource_IsNotReopened()
    {
        using var iso = new IsolatedDatabase();
        Analyzed(iso, "SRC-4", "serum");
        Analyzed(iso, "ELU-4", "eluate");
        iso.Db.SetEluateSource("ELU-4", "SRC-4");
        iso.Analyzer.AnalyzeSpecimen("SRC-4", updateDb: true);
        iso.Db.SetSpecimenFinalCall("SRC-4", "NAD", null, "TE");
        var before = iso.Db.GetSpecimen("SRC-4")!.ReactionsUpdatedAt;

        iso.Analyzer.AnalyzeSpecimen("ELU-4", updateDb: true);
        iso.Db.SetSpecimenFinalCall("ELU-4", "anti-K", null, "TE");

        Assert.Equal(before, iso.Db.GetSpecimen("SRC-4")!.ReactionsUpdatedAt);
    }
}
