using System.IO.Compression;
using System.Text;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

/// <summary>
/// Direct tests of the JSON/XML panel parser and the XLSX grid reader. Import errors put
/// wrong antigen typing into analysis, so these check intermediate parsing, not just success.
/// </summary>
public class StructuredImportParserTests
{
    private static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Json_KeepsAntigenCase_MapsValues_AndNeverInventsTyping()
    {
        var json = """
        {
          "vendor": "Acme", "lotNumber": " L-77 ", "expirationDate": "2027-01-31", "enzymeTreated": "true",
          "antigenOrder": ["D", "K", "k", "Fya", "Zzq"],
          "cells": [
            { "cellNumber": "1", "donorId": "A1", "antigens": { "D": "+", "K": "0", "k": "++", "Fya": "nt" } },
            { "cellNumber": "2", "antigens": { "D": 1, "K": "+", "Fy(a)": "-" } }
          ]
        }
        """;
        var r = StructuredPanelParser.ParseJson(Utf8(json), "Fallback", "lot.json", null);
        Assert.True(r.Success, string.Join("; ", r.Errors));
        Assert.Equal("Acme", r.Vendor);
        Assert.Equal("L-77", r.LotNumber);
        Assert.True(r.EnzymeTreated);
        Assert.Equal("json", r.SourceFormat);
        Assert.Equal(new[] { "D", "K", "k", "Fya" }, r.AntigenOrder.Take(4));
        Assert.Contains("Zzq", r.UnknownAntigens);

        var c1 = r.Cells[0];
        Assert.Equal("A1", c1.DonorId);
        Assert.Equal("-", c1.Antigens["K"]);
        Assert.Equal("++", c1.Antigens["k"]);
        Assert.Equal(AntigenConstants.AntigenNotTested, c1.Antigens["Fya"]);

        var c2 = r.Cells[1];
        Assert.Equal("+", c2.Antigens["D"]);
        Assert.Equal("+", c2.Antigens["K"]);
        Assert.Equal("-", c2.Antigens["Fya"]);
        Assert.False(c2.Antigens.ContainsKey("k")); // not listed for cell 2, so not typed
    }

    [Fact]
    public void Json_RejectsArrayRoot_InvalidJson_InvalidValues_MissingAndDuplicateCells()
    {
        Assert.Contains("not an array", StructuredPanelParser.ParseJson(Utf8("[]"), "V", "x.json", null).Errors.Single());
        Assert.StartsWith("Invalid JSON", StructuredPanelParser.ParseJson(Utf8("{ nope"), "V", "x.json", null).Errors.Single());

        var json = """
        { "cells": [
            { "cellNumber": "1", "antigens": { "D": "maybe" } },
            { "antigens": { "D": "+" } },
            { "cellNumber": " 1 ", "antigens": { "D": "+" } }
        ] }
        """;
        var r = StructuredPanelParser.ParseJson(Utf8(json), "V", "x.json", null);
        Assert.Contains("Cell 1: invalid value 'maybe' for D.", r.Errors);
        Assert.Contains("A cell is missing cellNumber.", r.Errors);
        Assert.Contains("Duplicate cell '1'.", r.Errors);
        Assert.False(r.Success);
    }

    [Theory]
    [InlineData("\"just text\"", "not a string")]
    [InlineData("42", "not a number")]
    [InlineData("null", "not a null")]
    public void Json_NonObjectRoot_IsRefusedWithAReason_NotThrown(string json, string expected)
    {
        var r = StructuredPanelParser.ParseJson(Utf8(json), "V", "x.json", null);
        Assert.Contains(expected, r.Errors.Single());
        Assert.False(r.Success);
    }

    [Fact]
    public void Json_NonObjectCellsAndSections_AreReportedNotThrown()
    {
        var json = """
        { "cells": [
            { "cellNumber": "1", "antigens": { "D": "+" } },
            7,
            "cell two",
            { "cellNumber": "3", "antigens": [ "D" ] }
        ] }
        """;
        var r = StructuredPanelParser.ParseJson(Utf8(json), "V", "x.json", null);
        Assert.Contains("A cell is missing cellNumber.", r.Errors);
        Assert.False(r.Success);
        // Through the lab import path the same file is refused, not an unhandled error.
        var lab = PanelCsvService.ImportBytes(Utf8(json), "x.json");
        Assert.False(lab.Success);
    }

    [Fact]
    public void Json_EmptyCells_IsAnError_AndUtf8BomIsAccepted()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Utf8("""{ "vendor": "V", "cells": [] }""")).ToArray();
        var r = StructuredPanelParser.ParseJson(bom, "V", "x.json", null);
        Assert.Equal("Structured file contains no panel cells.", r.Errors.Single());
    }

    [Fact]
    public void Xml_ReadsAttributesOrChildren_AndKeepsCase()
    {
        var xml = """
        <panel vendor="Acme" lot="X-1">
          <expiration>2027-02-28</expiration>
          <cells>
            <cell cellNumber="1"><antigen name="C">+</antigen><antigen name="c">0</antigen><antigen name="S">nt</antigen></cell>
            <cell><cellNumber>2</cellNumber><ag id="E">++</ag><ag name="">+</ag></cell>
          </cells>
        </panel>
        """;
        var r = StructuredPanelParser.ParseXml(Utf8(xml), "V", "p.xml", null);
        Assert.True(r.Success, string.Join("; ", r.Errors));
        Assert.Equal("X-1", r.LotNumber);
        Assert.Equal("2027-02-28", r.ExpirationDate);
        Assert.Equal("+", r.Cells[0].Antigens["C"]);
        Assert.Equal("-", r.Cells[0].Antigens["c"]);
        Assert.Equal(AntigenConstants.AntigenNotTested, r.Cells[0].Antigens["S"]);
        Assert.Equal("2", r.Cells[1].CellNumber);
        Assert.Equal("++", r.Cells[1].Antigens["E"]);
        Assert.StartsWith("Invalid XML", StructuredPanelParser.ParseXml(Utf8("<panel>"), "V", "p.xml", null).Errors.Single());
    }

    [Fact]
    public void ToLabResult_DropsNotTestedValues_AndCarriesErrorsAndUnknowns()
    {
        var json = """{ "antigenOrder": ["D", "Qqq"], "cells": [ { "cellNumber": "1", "antigens": { "D": "+", "K": "nt" } } ] }""";
        var lab = StructuredPanelParser.ToLabResult(StructuredPanelParser.ParseJson(Utf8(json), "V", "x.json", null));
        var cell = Assert.Single(lab.Cells);
        Assert.Equal("+", cell.Antigens["D"]);
        Assert.False(cell.Antigens.ContainsKey("K"));
        Assert.Contains("Qqq", lab.UnknownHeaders);
    }

    // ── XLSX ─────────────────────────────────────────────────────────────

    private static byte[] Xlsx(string sheetXml, string? sharedXml = null, bool withWorkbook = true)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string path, string text)
            {
                using var w = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                w.Write(text);
            }
            const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            if (withWorkbook)
            {
                Add("xl/workbook.xml", $"""<workbook xmlns="{ns}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Antigram" sheetId="1" r:id="rId9"/></sheets></workbook>""");
                Add("xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId9" Target="worksheets/antigram.xml"/></Relationships>""");
                Add("xl/worksheets/antigram.xml", $"""<worksheet xmlns="{ns}"><sheetData>{sheetXml}</sheetData></worksheet>""");
                Add("xl/worksheets/sheet1.xml", $"""<worksheet xmlns="{ns}"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>WRONG SHEET</t></is></c></row></sheetData></worksheet>""");
            }
            else
                Add("xl/worksheets/sheet1.xml", $"""<worksheet xmlns="{ns}"><sheetData>{sheetXml}</sheetData></worksheet>""");
            if (sharedXml != null)
                Add("xl/sharedStrings.xml", $"""<sst xmlns="{ns}">{sharedXml}</sst>""");
        }
        return ms.ToArray();
    }

    [Fact]
    public void Xlsx_FollowsWorkbookRelationship_ResolvesSharedAndInlineStrings_AndPlacesSparseColumns()
    {
        var bytes = Xlsx(
            """
            <row r="1"><c r="A1" t="s"><v>0</v></c><c r="C1" t="s"><v>1</v></c></row>
            <row r="2"><c r="A2"><v>1</v></c><c r="C2" t="inlineStr"><is><t>+</t></is></c></row>
            <row r="3"></row>
            <row r="4"><c r="AA4"><v>7</v></c></row>
            """,
            "<si><t>Cell</t></si><si><r><t>K</t></r><r><t>pa</t></r></si>");
        var rows = XlsxGridReader.ReadFirstSheet(bytes);
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "Cell", "", "Kpa" }, rows[0]);
        Assert.Equal(new[] { "1", "", "+" }, rows[1]);
        Assert.Equal(27, rows[2].Length);
        Assert.Equal("7", rows[2][26]);
    }

    [Fact]
    public void Xlsx_CellsWithoutReferences_AreReadInOrder()
    {
        // The r attribute is optional in OOXML; cells without it follow the previous cell.
        var bytes = Xlsx(
            """
            <row><c t="inlineStr"><is><t>Cell</t></is></c><c t="inlineStr"><is><t>D</t></is></c></row>
            <row><c><v>1</v></c><c r="C2" t="inlineStr"><is><t>+</t></is></c><c t="inlineStr"><is><t>0</t></is></c></row>
            """, withWorkbook: false);
        var rows = XlsxGridReader.ReadFirstSheet(bytes);
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "Cell", "D" }, rows[0]);
        Assert.Equal(new[] { "1", "", "+", "0" }, rows[1]);
    }

    [Fact]
    public void Xlsx_SharedStringPhoneticRuns_AreNotAppended()
    {
        var bytes = Xlsx("""<row r="1"><c r="A1" t="s"><v>0</v></c></row>""",
            "<si><t>Fya</t><rPh sb=\"0\" eb=\"1\"><t>ふ</t></rPh></si>");
        Assert.Equal("Fya", XlsxGridReader.ReadFirstSheet(bytes)[0][0]);
    }

    [Theory]
    [InlineData("A1", 0)]
    [InlineData("Z9", 25)]
    [InlineData("AA10", 26)]
    [InlineData("ab3", 27)]
    [InlineData("", -1)]
    public void ColumnIndex_FromReference(string cellRef, int expected) =>
        Assert.Equal(expected, XlsxGridReader.ColumnIndex(cellRef));
}
