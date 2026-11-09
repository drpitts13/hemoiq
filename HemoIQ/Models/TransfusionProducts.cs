using System;
using System.Collections.Generic;
using System.Linq;

namespace HemoIQ.Models
{
    /// <summary>Blood product types recorded with a transfusion, stored as comma-separated codes.</summary>
    public static class TransfusionProducts
    {
        public const string RedCells = "rbc";
        public const string WholeBlood = "whole_blood";
        public const string Granulocytes = "granulocytes";
        public const string Platelets = "platelets";
        public const string Plasma = "plasma";
        public const string Cryo = "cryo";

        public static readonly IReadOnlyList<(string Code, string Label)> All = new[]
        {
            (RedCells, "Red cells"), (WholeBlood, "Whole blood"), (Granulocytes, "Granulocytes"),
            (Platelets, "Platelets"), (Plasma, "Plasma"), (Cryo, "Cryoprecipitate"),
        };

        /// <summary>Products that carry enough donor red cells to change a serologic phenotype.</summary>
        public static readonly IReadOnlySet<string> ContainRedCells = new HashSet<string> { RedCells, WholeBlood, Granulocytes };

        /// <summary>Products with donor plasma, which can passively transfer antibodies.</summary>
        public static readonly IReadOnlySet<string> ContainPlasma = new HashSet<string> { WholeBlood, Platelets, Plasma };

        /// <summary>Known codes in canonical order; unknown or blank entries are dropped.</summary>
        public static List<string> Parse(string? text) =>
            All.Select(a => a.Code)
                .Where(code => (text ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Contains(code))
                .ToList();

        public static string? Format(IEnumerable<string>? codes)
        {
            var list = Parse(string.Join(",", codes ?? Enumerable.Empty<string>()));
            return list.Count == 0 ? null : string.Join(",", list);
        }

        public static string Describe(string? text) =>
            string.Join(", ", Parse(text).Select(c => All.First(a => a.Code == c).Label));
    }
}
