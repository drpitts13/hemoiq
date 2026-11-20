using System;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace HemoIQ.Services
{
    /// <summary>
    /// Every CSV HemoIQ writes may be opened in a spreadsheet, which runs a cell that starts with =, +, -, @,
    /// tab or carriage return as a formula. Reasons, notes, names and final calls are typed by users, so text
    /// fields are neutralized with a leading apostrophe. Typing made only of + and - ("+", "++", "-") and plain
    /// numbers such as -5 or +0.5 are left as they are, so antigrams and counts read back unchanged.
    /// </summary>
    public static class CsvText
    {
        /// <summary>The value with a leading apostrophe when a spreadsheet would read it as a formula.</summary>
        public static string Safe(string? value)
        {
            var t = value ?? "";
            if (t.Length == 0) return t;
            switch (t[0])
            {
                case '=': case '@': case '\t': case '\r':
                    return "'" + t;
                case '+': case '-':
                    if (t.Trim('+', '-').Length == 0) return t;
                    return double.TryParse(t, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out _) ? t : "'" + t;
                default:
                    return t;
            }
        }

        /// <summary>A neutralized CSV field, quoted only when it holds a comma, quote or line break.</summary>
        public static string Minimal(string? value)
        {
            var t = Safe(value);
            return t.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + t.Replace("\"", "\"\"") + "\"" : t;
        }

        /// <summary>A quoted, neutralized CSV field.</summary>
        public static string Field(string? value) => "\"" + Safe(value).Replace("\"", "\"\"") + "\"";

        /// <summary>A CsvHelper writer that neutralizes every field with <see cref="Safe"/>.</summary>
        public static CsvWriter Writer(TextWriter writer) => new SafeCsvWriter(writer);

        /// <summary>A UTF-8 file writer (with BOM, so spreadsheets read °C and other symbols correctly).</summary>
        public static StreamWriter File(string path) => new(path, false, new UTF8Encoding(true));

        private sealed class SafeCsvWriter : CsvWriter
        {
            public SafeCsvWriter(TextWriter writer)
                : base(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { InjectionOptions = InjectionOptions.Escape })
            {
            }

            protected override string SanitizeForInjection(string? field) => Safe(field);

            // Records (WriteRecords) reach this overload without the injection hook, so neutralize here too.
            // Safe is idempotent: a value that already starts with an apostrophe is left alone.
            public override void WriteField(string? field, bool shouldQuote) => base.WriteField(Safe(field), shouldQuote);
        }
    }
}
