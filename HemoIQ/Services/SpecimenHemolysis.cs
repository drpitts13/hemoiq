using System.Collections.Generic;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>Visual hemolysis of the specimen as received.</summary>
    public static class SpecimenHemolysis
    {
        public const string NotRecorded = "";
        public const string None = "none";
        public const string Slight = "slight";
        public const string Moderate = "moderate";
        public const string Gross = "gross";

        public static readonly IReadOnlyList<(string Code, string Label)> Options = new[]
        {
            (NotRecorded, "Not recorded"), (None, "None"), (Slight, "Slight"), (Moderate, "Moderate"), (Gross, "Gross"),
        };

        public static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            None or "no" => None,
            Slight or "mild" or "1+" => Slight,
            Moderate or "2+" => Moderate,
            Gross or "severe" or "3+" or "4+" => Gross,
            _ => NotRecorded,
        };

        public static string Label(string? value) =>
            Options.First(o => o.Code == Normalize(value)).Label;

        /// <summary>
        /// REVIEW notes for analysis. A grossly hemolyzed specimen always gets one: hemolysis read in
        /// the tests cannot be told apart from the specimen's own, and free hemoglobin can obscure weak
        /// agglutination. Slight or moderate hemolysis gets one only when an H grade was recorded, since
        /// that is where the two can be confused. Evidence only; no grade is changed or excluded.
        /// </summary>
        public static List<string> ReviewNotes(string? hemolysis, IEnumerable<Reaction> reactions)
        {
            var level = Normalize(hemolysis);
            var notes = new List<string>();
            if (level is NotRecorded or None) return notes;
            var hCells = reactions
                .Where(r => new[] { r.IS, r.C37, r.AHG }.Concat(r.ExtraPhases.Values).Any(v => ReactionGrade.Normalize(v) == ReactionGrade.Hemolysis))
                .Select(r => r.CellNumber)
                .Distinct()
                .ToList();
            var hText = hCells.Count == 0 ? "" :
                $" Hemolysis (H) was recorded on cell(s) {string.Join(", ", hCells.Take(8))}{(hCells.Count > 8 ? ", …" : "")}; " +
                "it may reflect the specimen rather than antibody-mediated hemolysis.";
            if (level == Gross)
                notes.Add("The specimen is recorded as grossly hemolyzed. Hemolysis in the test tubes cannot be told apart from the " +
                          "specimen's own, and free hemoglobin can hide weak agglutination." + hText +
                          " Consider a new, non-hemolyzed specimen before relying on these results.");
            else if (hCells.Count > 0)
                notes.Add($"The specimen is recorded as {Label(level).ToLowerInvariant()} hemolyzed." + hText +
                          " Compare with a non-hemolyzed specimen if the hemolysis matters to the interpretation.");
            return notes;
        }
    }
}
