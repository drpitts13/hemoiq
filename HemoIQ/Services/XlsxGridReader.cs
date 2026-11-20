using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace HemoIQ.Services
{
    /// <summary>
    /// Reads the first worksheet of an .xlsx (Office Open XML) workbook as a
    /// rectangular string grid. No extra package is required.
    /// </summary>
    public static class XlsxGridReader
    {
        public static List<string[]> ReadFirstSheet(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
            var shared = ReadSharedStrings(zip);
            var sheetPath = FindFirstSheetPath(zip);
            var entry = zip.GetEntry(sheetPath)
                ?? throw new InvalidOperationException("XLSX is missing the first worksheet.");
            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            return ParseSheet(doc, shared);
        }

        private static string FindFirstSheetPath(ZipArchive zip)
        {
            var workbook = zip.GetEntry("xl/workbook.xml");
            var rels = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbook != null && rels != null)
            {
                using var relStream = rels.Open();
                var relDoc = XDocument.Load(relStream);
                var relMap = relDoc.Descendants()
                    .Where(e => e.Name.LocalName == "Relationship")
                    .ToDictionary(
                        e => (string?)e.Attribute("Id") ?? "",
                        e => ((string?)e.Attribute("Target") ?? "").Replace('\\', '/'),
                        StringComparer.Ordinal);
                using var wbStream = workbook.Open();
                var wbDoc = XDocument.Load(wbStream);
                var firstSheet = wbDoc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "sheet");
                var rid = (string?)firstSheet?.Attribute(XName.Get("id",
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
                if (string.IsNullOrEmpty(rid))
                    rid = (string?)firstSheet?.Attribute("id") ?? (string?)firstSheet?.Attribute("r:id");
                if (!string.IsNullOrEmpty(rid) && relMap.TryGetValue(rid, out var target)
                    && !string.IsNullOrWhiteSpace(target))
                {
                    if (target.StartsWith("/")) target = target.TrimStart('/');
                    else if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                        target = "xl/" + target;
                    return target;
                }
            }

            var fallback = zip.Entries.FirstOrDefault(e =>
                e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            if (fallback != null) return fallback.FullName;
            throw new InvalidOperationException("XLSX has no worksheet part.");
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            var list = new List<string>();
            if (entry == null) return list;
            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            foreach (var si in doc.Descendants().Where(e => e.Name.LocalName == "si"))
                list.Add(VisibleText(si));
            return list;
        }

        private static List<string[]> ParseSheet(XDocument doc, IReadOnlyList<string> shared)
        {
            var rows = new List<string[]>();
            foreach (var rowEl in doc.Descendants().Where(e => e.Name.LocalName == "row"))
            {
                var cells = rowEl.Elements().Where(e => e.Name.LocalName == "c").ToList();
                if (cells.Count == 0) continue;
                // The r reference is optional in OOXML: a cell without one is in the column
                // after the previous cell.
                var placed = new List<(int Col, string Text)>();
                var next = 0;
                foreach (var cell in cells)
                {
                    var col = ColumnIndex((string?)cell.Attribute("r") ?? "");
                    if (col < 0) col = next;
                    placed.Add((col, CellText(cell, shared)));
                    next = col + 1;
                }
                var values = new string[placed.Max(p => p.Col) + 1];
                Array.Fill(values, "");
                foreach (var (col, text) in placed) values[col] = text;
                if (values.All(string.IsNullOrWhiteSpace)) continue;
                rows.Add(values);
            }
            return rows;
        }

        private static string CellText(XElement cell, IReadOnlyList<string> shared)
        {
            var type = (string?)cell.Attribute("t");
            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                var idxText = cell.Descendants().FirstOrDefault(e => e.Name.LocalName == "v")?.Value;
                if (int.TryParse(idxText, out var idx) && idx >= 0 && idx < shared.Count)
                    return shared[idx];
                return "";
            }
            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
                return cell.Elements().FirstOrDefault(e => e.Name.LocalName == "is") is XElement inline
                    ? VisibleText(inline) : "";
            return cell.Descendants().FirstOrDefault(e => e.Name.LocalName == "v")?.Value ?? "";
        }

        /// <summary>
        /// Text of a string item (plain or rich-text runs), leaving out phonetic (rPh) runs,
        /// which are reading hints and not part of the cell's value.
        /// </summary>
        private static string VisibleText(XElement item) =>
            string.Concat(item.Descendants()
                .Where(e => e.Name.LocalName == "t" && !e.Ancestors().Any(a => a.Name.LocalName == "rPh"))
                .Select(e => e.Value));

        /// <summary>Zero-based column of a reference such as "AA10"; -1 when there is no column part.</summary>
        public static int ColumnIndex(string cellRef)
        {
            var col = 0;
            foreach (var c in cellRef)
            {
                if (!char.IsLetter(c)) break;
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            }
            return col - 1;
        }
    }
}
