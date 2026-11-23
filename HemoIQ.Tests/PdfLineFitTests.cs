using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The PDF export draws each line without wrapping, so a line wider than the page used to be cut off at the edge:
/// on a wide panel, the CC and extra-phase grades at the end of each worksheet row. The font now shrinks so the widest
/// line fits, and a line still too wide at the smallest size is wrapped, so no grade is lost.
/// </summary>
[Collection("PersistentDatabase")]
public class PdfLineFitTests
{
    private const double LandscapeUsable = 792 - 56; // Letter landscape, less the margins

    [Fact]
    public void ShortLines_KeepTheDefaultSize() =>
        Assert.Equal(7, ReportService.PdfFontSizeToFit(80, LandscapeUsable, 7));

    [Fact]
    public void WideLines_ShrinkTheFontToFit()
    {
        var size = ReportService.PdfFontSizeToFit(220, LandscapeUsable, 7);
        Assert.True(size < 7);
        Assert.True(ReportService.PdfCharsPerLine(LandscapeUsable, size) >= 220);
    }

    [Fact]
    public void VeryWideLines_AreWrapped_WithoutLosingCharacters()
    {
        var size = ReportService.PdfFontSizeToFit(1000, LandscapeUsable, 7);
        Assert.Equal(ReportService.PdfMinimumFontSize, size);
        var max = ReportService.PdfCharsPerLine(LandscapeUsable, size);
        var line = string.Concat(Enumerable.Range(0, 1000).Select(i => (char)('a' + i % 26)));
        var pieces = ReportService.WrapForPdf(line, max).ToList();
        Assert.True(pieces.Count > 1);
        Assert.All(pieces, p => Assert.True(p.Length <= max));
        Assert.Equal(line, pieces[0] + string.Concat(pieces.Skip(1).Select(p => p[4..])));
    }

    [Fact]
    public void AWideWorksheet_FitsOnThePdfPage()
    {
        var previous = AppSettings.Current.ExtraPhases;
        try
        {
            AppSettings.Current.ExtraPhases = "PEG, LISS-IAT, Enzyme";
            using var iso = new IsolatedDatabase();
            var panel = iso.Db.AddPanel("Wide", "L-W", "V", 2, null, false, startTypedNegative: true);
            iso.Db.AddSpecimen("PDF-1", "serum", null);
            iso.Db.LinkSpecimenPanel("PDF-1", panel);
            iso.Db.SaveReaction("PDF-1", panel, "1", "0", "0", "0", "2+");
            var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "PDF-1");
            var longest = text.Split('\n').Max(l => l.TrimEnd().Length);
            var size = ReportService.PdfFontSizeToFit(longest, LandscapeUsable, 7);
            var fits = ReportService.PdfCharsPerLine(LandscapeUsable, size);
            // Either the font shrank so every line fits, or the longest lines are wrapped, never cut off.
            Assert.All(text.Split('\n').SelectMany(l => ReportService.WrapForPdf(l.TrimEnd(), fits)),
                piece => Assert.True(piece.Length <= fits, piece));
        }
        finally { AppSettings.Current.ExtraPhases = previous; }
    }
}
