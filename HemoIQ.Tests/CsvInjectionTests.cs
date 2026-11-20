using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// CSV exports are opened in spreadsheets, which run a cell starting with =, +, -, @, tab or CR as a formula.
/// User-typed text is neutralized; panel typing and numbers are written unchanged.
/// </summary>
public class CsvInjectionTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"http://x\",\"y\")", "'=HYPERLINK(\"http://x\",\"y\")")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("+cmd|' /C calc'!A0", "'+cmd|' /C calc'!A0")]
    [InlineData("-cmd|' /C calc'!A0", "'-cmd|' /C calc'!A0")]
    [InlineData("-1+1", "'-1+1")]
    [InlineData("\tx", "'\tx")]
    [InlineData("-", "-")]
    [InlineData("+", "+")]
    [InlineData("++", "++")]
    [InlineData("-5", "-5")]
    [InlineData("+0.5", "+0.5")]
    [InlineData("anti-E", "anti-E")]
    [InlineData("2+", "2+")]
    [InlineData("", "")]
    public void Safe_NeutralizesFormulas_AndKeepsTypingAndNumbers(string input, string expected) =>
        Assert.Equal(expected, CsvText.Safe(input));

    [Fact]
    public void Field_QuotesAndNeutralizes()
    {
        Assert.Equal("\"'=1+1\"", CsvText.Field("=1+1"));
        Assert.Equal("\"say \"\"hi\"\"\"", CsvText.Field("say \"hi\""));
        Assert.Equal("\"\"", CsvText.Field(null));
        Assert.Equal("'=1+1", CsvText.Minimal("=1+1"));
        Assert.Equal("\"Doe, T\"", CsvText.Minimal("Doe, T"));
        Assert.Equal("tech", CsvText.Minimal("tech"));
    }

    [Fact]
    public void Writer_NeutralizesEveryFormulaField()
    {
        using var sw = new StringWriter();
        using (var csv = CsvText.Writer(sw))
        {
            csv.WriteField("=2*3");
            csv.WriteField("-");
            csv.WriteField("++");
            csv.WriteField("@x");
            csv.NextRecord();
        }
        var line = sw.ToString().TrimEnd();
        Assert.Contains("'=2*3", line);
        Assert.Contains("'@x", line);
        Assert.Contains(",-,", line);
        Assert.Contains(",++,", line);
    }

    [Fact]
    public void PanelCsv_RoundTripsTyping_Unchanged()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("CSV", "CSV-1", "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "++");
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_csv_{Guid.NewGuid():N}.csv");
        try
        {
            PanelCsvService.Export(iso.Db.GetPanelCells(panel), path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("'+", text);
            Assert.DoesNotContain("'-", text);

            var parsed = PanelCsvService.Import(path);
            Assert.True(parsed.Success, string.Join("; ", parsed.Errors));
            Assert.Equal("+", parsed.Cells.Single(c => c.CellNumber == cells[0].CellNumber).Antigens["E"]);
            Assert.Equal("++", parsed.Cells.Single(c => c.CellNumber == cells[1].CellNumber).Antigens["E"]);
            Assert.Equal("-", parsed.Cells.Single(c => c.CellNumber == cells[0].CellNumber).Antigens["K"]);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void EveryCsvExport_UsesTheNeutralizingWriter()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var root = Path.Combine(dir!.FullName, "HemoIQ");
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                        !p.EndsWith("CsvText.cs", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("new CsvWriter(", StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(root, p))
            .ToList();
        Assert.True(offenders.Count == 0, "Write CSV through CsvText.Writer: " + string.Join(", ", offenders));
    }

    [Fact]
    public void AllPanelsReportCsv_NeutralizesAFormulaInAPanelName()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddPanel("=HYPERLINK(\"http://evil\",\"open\")", "INJ-1", "V", 2, null, false);
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_rpt_{Guid.NewGuid():N}.csv");
        try
        {
            iso.Reports.ExportToCsv(ReportType.AllPanels, path);
            var text = File.ReadAllText(path);
            Assert.Contains("'=HYPERLINK", text);
            Assert.DoesNotContain(",=HYPERLINK", text);
            Assert.DoesNotContain("\"=HYPERLINK", text);
        }
        finally { File.Delete(path); }
    }
}
