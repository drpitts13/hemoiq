using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using HemoIQ.Models;
using HemoIQ.Services.Vendors;

namespace HemoIQ.Services
{
    public class ImportedPanelCell
    {
        public string CellNumber { get; set; } = string.Empty;
        public Dictionary<string, string> Antigens { get; set; } = new();
        /// <summary>Donor ID, Rh phenotype and special types: kept through import so a lot round-trips intact.</summary>
        public string? DonorId { get; set; }
        public string? RhPhenotype { get; set; }
        public string? SpecialTypes { get; set; }
    }

    public class PanelCsvImportResult
    {
        public List<ImportedPanelCell> Cells { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> AntigenHeaderOrder { get; } = new();
        public List<string> UnknownHeaders { get; } = new();
        public byte[]? SourceBytes { get; set; }
        public string? SourceFileName { get; set; }
        public string? SourcePath { get; set; }
        public bool Success => Errors.Count == 0 && Cells.Count > 0;
    }

    public static class PanelCsvService
    {
        public static void Export(IReadOnlyList<PanelCell> cells, string filePath,
            IReadOnlyList<string>? columnOrder = null)
        {
            using var writer = CsvText.File(filePath);
            using var csv = CsvText.Writer(writer);

            var extras = ExtraAntigensOnCells(cells);
            var antigens = AntigenConstants.ResolveDisplayOrder(columnOrder, extras);
            // The cell details analysis or the bench copy use, written when any cell has them; Import reads them back.
            var donor = cells.Any(c => !string.IsNullOrWhiteSpace(c.DonorId));
            var rh = cells.Any(c => !string.IsNullOrWhiteSpace(c.RhPhenotype));
            var special = cells.Any(c => !string.IsNullOrWhiteSpace(c.SpecialTypes));
            csv.WriteField("Cell");
            if (donor) csv.WriteField(DonorHeader);
            if (rh) csv.WriteField(RhHeader);
            foreach (var ag in antigens)
                csv.WriteField(ag);
            if (special) csv.WriteField(SpecialTypesHeader);
            csv.NextRecord();

            foreach (var cell in cells)
            {
                csv.WriteField(cell.CellNumber);
                if (donor) csv.WriteField(cell.DonorId ?? "");
                if (rh) csv.WriteField(cell.RhPhenotype ?? "");
                foreach (var ag in antigens)
                    csv.WriteField(cell.GetTypedValue(ag) ?? cell.GetAntigen(ag));
                if (special) csv.WriteField(cell.SpecialTypes ?? "");
                csv.NextRecord();
            }
        }

        public const string DonorHeader = "Donor";
        public const string RhHeader = "Rh";
        public const string SpecialTypesHeader = "Special types";

        private static readonly string[] DonorKeys = { "DONOR", "DONORID", "DONORNO" };
        private static readonly string[] RhKeys = { "RH", "RHHR", "RHPHENOTYPE" };
        private static readonly string[] SpecialKeys = { "SPECIALTYPES", "SPECIAL" };

        /// <summary>The index of the first header whose normalised name is one of <paramref name="keys"/>, or -1.</summary>
        private static int ColumnOf(string[] headers, string[] keys)
        {
            for (int i = 0; i < headers.Length; i++)
                if (keys.Contains(VendorAntigenAliases.NormalizeKey(headers[i].Trim()), StringComparer.Ordinal)) // NormalizeKey upper-cases
                    return i;
            return -1;
        }

        public static PanelCsvImportResult Import(string filePath)
        {
            var result = new PanelCsvImportResult();
            if (!File.Exists(filePath))
            {
                result.Errors.Add("File not found.");
                return result;
            }

            var bytes = File.ReadAllBytes(filePath);
            result = ImportBytes(bytes, Path.GetFileName(filePath));
            result.SourceBytes = bytes;
            result.SourceFileName = Path.GetFileName(filePath);
            result.SourcePath = filePath;
            return result;
        }

        public static PanelCsvImportResult ImportBytes(byte[] bytes, string fileName)
        {
            if (LooksLikeStructured(bytes, fileName))
            {
                using var stream = new MemoryStream(bytes, writable: false);
                var parsed = Vendors.VendorAntigramParser.Parse(stream, "Lab", fileName, null);
                return StructuredPanelParser.ToLabResult(parsed);
            }

            using var csvStream = new MemoryStream(bytes, writable: false);
            return Import(csvStream);
        }

        private static bool LooksLikeStructured(byte[] bytes, string fileName)
        {
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return true;
            if (bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K'
                && bytes[2] == 3 && bytes[3] == 4)
                return true;
            var text = Encoding.UTF8.GetString(bytes).TrimStart();
            return text.StartsWith("{") || text.StartsWith("<");
        }

        public static PanelCsvImportResult Import(Stream stream)
        {
            var result = new PanelCsvImportResult();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null,
            };

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            using var csv = new CsvReader(reader, config);
            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord == null)
            {
                result.Errors.Add("CSV has no header row.");
                return result;
            }

            var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < csv.HeaderRecord.Length; i++)
            {
                var name = csv.HeaderRecord[i].Trim();
                if (!headerMap.ContainsKey(name))
                    headerMap[name] = i;
            }

            if (!headerMap.ContainsKey("Cell"))
            {
                result.Errors.Add("CSV must include a Cell column.");
                return result;
            }

            foreach (var raw in csv.HeaderRecord)
            {
                var name = raw.Trim();
                if (AntigenConstants.IsKnown(name) && !result.AntigenHeaderOrder.Contains(name))
                    result.AntigenHeaderOrder.Add(name);
                else
                    PanelTypingInspector.TryRecordUnknownHeader(name, result.UnknownHeaders);
            }

            var donorCol = ColumnOf(csv.HeaderRecord, DonorKeys);
            var rhCol = ColumnOf(csv.HeaderRecord, RhKeys);
            var specialCol = ColumnOf(csv.HeaderRecord, SpecialKeys);

            int rowNum = 1;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (csv.Read())
            {
                rowNum++;
                var cellNumber = csv.GetField(headerMap["Cell"])?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(cellNumber))
                {
                    result.Errors.Add($"Row {rowNum}: blank cell number.");
                    continue;
                }
                if (!seen.Add(cellNumber))
                {
                    result.Errors.Add($"Row {rowNum}: duplicate cell '{cellNumber}'.");
                    continue;
                }
                // A row with fewer fields than the header is cut off (truncated file or lost line end). Its
                // missing fields would read as blank, which types antigen-negative, so refuse instead.
                if (csv.Parser.Count < csv.HeaderRecord.Length)
                {
                    result.Errors.Add($"Row {rowNum} cell {cellNumber}: has {csv.Parser.Count} of {csv.HeaderRecord.Length} columns; " +
                        "the file may be cut off. Missing values are not read as negative. Re-export the file.");
                    continue;
                }

                string? Optional(int col) => col < 0 || string.IsNullOrWhiteSpace(csv.GetField(col)) ? null : csv.GetField(col)!.Trim();
                var imported = new ImportedPanelCell
                {
                    CellNumber = cellNumber,
                    DonorId = Optional(donorCol),
                    RhPhenotype = Optional(rhCol),
                    SpecialTypes = Optional(specialCol),
                };
                foreach (var ag in AntigenConstants.Antigens)
                {
                    if (!headerMap.TryGetValue(ag, out var idx))
                        continue;
                    var raw = csv.GetField(idx) ?? "";
                    var normalized = NormalizeAntigen(raw);
                    if (normalized == null)
                    {
                        result.Errors.Add($"Row {rowNum} cell {cellNumber}: invalid value '{raw}' for {ag} (use + or −).");
                    }
                    else
                    {
                        imported.Antigens[ag] = normalized;
                    }
                }
                foreach (var ag in AntigenConstants.WarehouseAntigens)
                {
                    if (!headerMap.TryGetValue(ag, out var idx)) continue;
                    var raw = csv.GetField(idx) ?? "";
                    var normalized = NormalizeAntigen(raw);
                    if (normalized == null)
                    {
                        result.Errors.Add($"Row {rowNum} cell {cellNumber}: invalid value '{raw}' for {ag} (use + or −).");
                        imported.Antigens[ag] = "-";
                    }
                    else
                    {
                        imported.Antigens[ag] = normalized;
                    }
                }
                result.Cells.Add(imported);
            }

            if (result.Cells.Count == 0 && result.Errors.Count == 0)
                result.Errors.Add("CSV contains no data rows.");

            return result;
        }

        public static IReadOnlyList<string> ExtraAntigensOnCells(IEnumerable<PanelCell> cells) =>
            AntigenConstants.WarehouseAntigens
                .Where(ag => cells.Any(c => c.HasTypedAntigen(ag)))
                .ToList();

        public static string? NormalizeAntigen(string raw)
        {
            var v = raw.Trim();
            if (v.Length == 0) return "-";
            if (v is "++" or "+/+" or "homo" or "HOMO" or "homozygous" or "Homozygous")
                return AntigenConstants.AntigenHomozygous;
            if (v is "+" or "pos" or "POS" or "1" or "true" or "True") return "+";
            if (v is "-" or "−" or "neg" or "NEG" or "0" or "false" or "False") return "-";
            return null;
        }
    }
}
