using System.Collections.Generic;

namespace HemoIQ.Models
{
    /// <summary>
    /// Describes how the reagent red cells in a panel have been treated before testing.
    /// </summary>
    public enum CellTreatment
    {
        None = 0,
        Ficin,   // proteolytic enzyme; destroys MNSs, Duffy; enhances Rh/Kidd/P1
        Papain,  // proteolytic enzyme; essentially same effect profile as ficin
        DTT,     // dithiothreitol; destroys Kell, Lutheran, and other disulfide-dependent antigens
    }

    /// <summary>
    /// Describes how the patient serum/plasma has been treated before testing against the panel.
    /// </summary>
    public enum SerumTreatment
    {
        None = 0,
        Prewarmed,           // serum pre-incubated at 37°C; suppresses cold/IgM reactors, IS phase unreliable
        AlloAdsorptionR1R1,  // absorbed with R1R1 (DCe/DCe) cells; removes anti-D, anti-C, anti-e
        AlloAdsorptionR2R2,  // absorbed with R2R2 (DcE/DcE) cells; removes anti-D, anti-c, anti-E
        AlloAdsorptionRr,    // absorbed with rr (ce/ce) cells; removes anti-c, anti-e, anti-f
        AutoAdsorption,      // absorbed with patient's own phenotype-matched cells; removes autoantibody
    }

    /// <summary>
    /// Describes the net effect of a treatment on a specific antigen's expression on the reagent cell.
    /// </summary>
    public enum AntigenEffect
    {
        Unaffected = 0,
        Destroyed,   // antigen no longer present; negative reactions on these cells prove nothing
        Weakened,    // antigen expression reduced; use results with caution
        Enhanced,    // antigen expression stronger; useful for weak antibody detection
    }

    /// <summary>
    /// Evidence-based lookup tables for the effect of each treatment on each antigen.
    /// Sources: AABB Technical Manual; Blood Group Antigen FactsBook.
    /// </summary>
    public static class AntigenTreatmentEffects
    {
        // ── Ficin / Papain (identical profile for standard + warehouse antigens) ─

        private static readonly Dictionary<string, AntigenEffect> FicinEffects = new()
        {
            // Destroyed by ficin/papain
            { "M",   AntigenEffect.Destroyed },
            { "N",   AntigenEffect.Destroyed },
            { "S",   AntigenEffect.Destroyed },
            { "s",   AntigenEffect.Destroyed },
            { "Fya", AntigenEffect.Destroyed },
            { "Fyb", AntigenEffect.Destroyed },
            { "Xga", AntigenEffect.Destroyed },
            { "Mia", AntigenEffect.Destroyed },
            { "Vw",  AntigenEffect.Destroyed },
            { "Mur", AntigenEffect.Destroyed },
            { "Ge2", AntigenEffect.Destroyed },
            { "Ch",  AntigenEffect.Destroyed },
            { "Rg",  AntigenEffect.Destroyed },
            // Indian antigens sit on CD44, which proteolytic enzymes cleave (AABB Technical Manual; FactsBook).
            // Listed as enhanced before, a nonreactive ficin-treated cell could rule out anti-Inb.
            { "Inb", AntigenEffect.Destroyed },

            // Enhanced by ficin/papain
            { "D",   AntigenEffect.Enhanced },
            { "C",   AntigenEffect.Enhanced },
            { "c",   AntigenEffect.Enhanced },
            { "E",   AntigenEffect.Enhanced },
            { "e",   AntigenEffect.Enhanced },
            { "f",   AntigenEffect.Enhanced },
            { "V",   AntigenEffect.Enhanced },
            { "Cw",  AntigenEffect.Enhanced },
            { "Jka", AntigenEffect.Enhanced },
            { "Jkb", AntigenEffect.Enhanced },
            { "P1",  AntigenEffect.Enhanced },
            // Lewis antigens resist ficin/papain and anti-Lea/-Leb react more strongly with treated cells
            // (AABB Technical Manual; FactsBook). Listing them as destroyed made a ficin-sensitive pattern
            // look consistent with Lewis, the opposite of what enzyme testing shows.
            { "Lea", AntigenEffect.Enhanced },
            { "Leb", AntigenEffect.Enhanced },
            { "Vel", AntigenEffect.Enhanced },
            { "VS",  AntigenEffect.Enhanced },
            { "Goa", AntigenEffect.Enhanced },
            { "LWa", AntigenEffect.Enhanced },
            { "LWb", AntigenEffect.Enhanced },
        };

        // ── DTT / 2-ME ────────────────────────────────────────────────────────
        // DTT cleaves disulfide bonds — destroys antigens that depend on them.

        private static readonly Dictionary<string, AntigenEffect> DttEffects = new()
        {
            { "K",   AntigenEffect.Destroyed },
            { "k",   AntigenEffect.Destroyed },
            { "Kpa", AntigenEffect.Destroyed },
            { "Kpb", AntigenEffect.Destroyed },
            { "Jsa", AntigenEffect.Destroyed },
            { "Jsb", AntigenEffect.Destroyed },
            { "Lua", AntigenEffect.Destroyed },
            { "Lub", AntigenEffect.Destroyed },

            // Warehouse — disulfide-dependent or DTT-sensitive systems
            { "Doa",  AntigenEffect.Destroyed },
            { "Dob",  AntigenEffect.Destroyed },
            { "Yta",  AntigenEffect.Destroyed },
            { "Ytb",  AntigenEffect.Destroyed },
            { "LWa",  AntigenEffect.Destroyed },
            { "LWb",  AntigenEffect.Destroyed },
            { "Kna",  AntigenEffect.Destroyed },
            { "McCa", AntigenEffect.Destroyed },
            { "Yka",  AntigenEffect.Destroyed },
            { "Hy",   AntigenEffect.Destroyed },
            { "Joa",  AntigenEffect.Destroyed },
            { "Inb",  AntigenEffect.Destroyed },
            { "Cra",  AntigenEffect.Destroyed },
            { "Ch",   AntigenEffect.Destroyed },   // classically weakened; treat as destroyed for rule-out
            { "Rg",   AntigenEffect.Destroyed },
        };

        // ── Allogeneic absorption — antigens removed from serum per absorbing cell phenotype ──

        /// <summary>
        /// Antibody specificities removed when serum is absorbed with R1R1 (DCe/DCe) cells.
        /// R1R1 cells express: D, C, e (and their compound antigens).
        /// </summary>
        public static readonly IReadOnlyList<string> R1R1AbsorbedAntibodies =
            new[] { "anti-D", "anti-C", "anti-e" };

        /// <summary>
        /// Antibody specificities removed when serum is absorbed with R2R2 (DcE/DcE) cells.
        /// R2R2 cells express: D, c, E.
        /// </summary>
        public static readonly IReadOnlyList<string> R2R2AbsorbedAntibodies =
            new[] { "anti-D", "anti-c", "anti-E" };

        /// <summary>
        /// Antibody specificities removed when serum is absorbed with rr (ce/ce) cells.
        /// rr cells express: c, e, and the compound antigen f (ce, cis), so anti-f is removed too.
        /// R1R1 and R2R2 cells lack f. Leaving anti-f out let a nonreactive f+ cell on the rr-absorbed
        /// aliquot count as ruling anti-f out, although the absorption had removed it.
        /// </summary>
        public static readonly IReadOnlyList<string> RrAbsorbedAntibodies =
            new[] { "anti-c", "anti-e", "anti-f" };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Antigens the treatment affects in the given way, in table order (standard antigens first).</summary>
        public static IReadOnlyList<string> AntigensWithEffect(CellTreatment treatment, AntigenEffect effect)
        {
            var table = treatment switch
            {
                CellTreatment.Ficin or CellTreatment.Papain => FicinEffects,
                CellTreatment.DTT => DttEffects,
                _ => null,
            };
            return table == null
                ? System.Array.Empty<string>()
                : table.Where(kv => kv.Value == effect).Select(kv => kv.Key).ToList();
        }

        public static AntigenEffect GetCellEffect(CellTreatment treatment, string antigen)
        {
            if (treatment == CellTreatment.None) return AntigenEffect.Unaffected;

            var table = treatment switch
            {
                CellTreatment.Ficin  => FicinEffects,
                CellTreatment.Papain => FicinEffects,   // same profile
                CellTreatment.DTT    => DttEffects,
                _                    => null,
            };

            if (table == null) return AntigenEffect.Unaffected;
            return table.TryGetValue(antigen, out var effect) ? effect : AntigenEffect.Unaffected;
        }

        /// <summary>
        /// Returns true when the antigen is destroyed by the given cell treatment —
        /// meaning a negative reaction on a cell of that treatment cannot be used
        /// to rule out the corresponding antibody.
        /// </summary>
        public static bool IsAntigenDestroyedOnCell(CellTreatment treatment, string antigen) =>
            GetCellEffect(treatment, antigen) == AntigenEffect.Destroyed;

        /// <summary>
        /// Returns the list of antibodies expected to be removed from the serum
        /// by the given serum treatment (allogeneic or autologous adsorption).
        /// </summary>
        public static IReadOnlyList<string> GetAbsorbedAntibodies(SerumTreatment serumTreatment) =>
            serumTreatment switch
            {
                SerumTreatment.AlloAdsorptionR1R1 => R1R1AbsorbedAntibodies,
                SerumTreatment.AlloAdsorptionR2R2 => R2R2AbsorbedAntibodies,
                SerumTreatment.AlloAdsorptionRr   => RrAbsorbedAntibodies,
                // Auto-adsorption removes autoantibody (pan-reactive); no specific list
                _ => System.Array.Empty<string>(),
            };

        /// <summary>
        /// Phases that are NOT interpretable for antibody identification given the serum treatment.
        /// </summary>
        public static IReadOnlyList<string> GetNonInterpretablePhases(SerumTreatment serumTreatment) =>
            serumTreatment switch
            {
                SerumTreatment.Prewarmed => new[] { "IS" },
                _ => System.Array.Empty<string>(),
            };

        public static string GetDisplayName(CellTreatment t) => t switch
        {
            CellTreatment.None   => "Untreated",
            CellTreatment.Ficin  => "Ficin-treated",
            CellTreatment.Papain => "Papain-treated",
            CellTreatment.DTT    => "DTT-treated",
            _                    => t.ToString(),
        };

        public static string GetDisplayName(SerumTreatment t) => t switch
        {
            SerumTreatment.None                => string.Empty,
            SerumTreatment.Prewarmed           => "Prewarmed serum",
            SerumTreatment.AlloAdsorptionR1R1  => "Absorbed: R1R1",
            SerumTreatment.AlloAdsorptionR2R2  => "Absorbed: R2R2",
            SerumTreatment.AlloAdsorptionRr    => "Absorbed: rr",
            SerumTreatment.AutoAdsorption      => "Autoadsorbed",
            _                                  => t.ToString(),
        };
    }
}
