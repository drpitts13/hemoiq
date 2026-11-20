using System.Collections.Generic;

namespace HemoIQ.Models
{
    /// <summary>
    /// Patient sex as recorded on the permanent patient record. Stored as the lower-case
    /// code; the label is display only. Used where it matters operationally, such as
    /// checking a pregnancy recorded on a specimen.
    /// </summary>
    public static class PatientSex
    {
        public const string Unknown = "unknown";
        public const string Female = "female";
        public const string Male = "male";
        public const string Other = "other";

        public static readonly IReadOnlyList<(string Code, string Label)> Options = new[]
        {
            (Unknown, "Not recorded"),
            (Female, "Female"),
            (Male, "Male"),
            (Other, "Other / X"),
        };

        public static string Normalize(string? code)
        {
            var c = (code ?? string.Empty).Trim().ToLowerInvariant();
            return c switch
            {
                Female or "f" => Female,
                Male or "m" => Male,
                Other or "x" => Other,
                _ => Unknown,
            };
        }

        public static string Label(string? code)
        {
            var c = Normalize(code);
            foreach (var (k, label) in Options)
                if (k == c) return label;
            return "Not recorded";
        }
    }
}
