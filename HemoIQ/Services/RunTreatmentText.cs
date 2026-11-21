using System.Collections.Generic;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// What a new run's treatments mean, for the Add run dialog. Built from <see cref="AntigenTreatmentEffects"/>,
    /// the tables analysis itself uses, so the dialog can never describe a treatment differently from how the
    /// engine treats it.
    /// </summary>
    public static class RunTreatmentText
    {
        public static string Describe(CellTreatment cell, SerumTreatment serum)
        {
            var lines = new List<string>();
            var destroyed = AntigenTreatmentEffects.AntigensWithEffect(cell, AntigenEffect.Destroyed);
            var enhanced = AntigenTreatmentEffects.AntigensWithEffect(cell, AntigenEffect.Enhanced);
            if (destroyed.Count > 0 || enhanced.Count > 0)
            {
                var parts = new List<string>();
                if (destroyed.Count > 0) parts.Add("destroys " + string.Join(", ", destroyed));
                if (enhanced.Count > 0) parts.Add("enhances " + string.Join(", ", enhanced));
                lines.Add("Cells: " + string.Join(". ", parts) + ".");
            }

            var removed = AntigenTreatmentEffects.GetAbsorbedAntibodies(serum);
            var phases = AntigenTreatmentEffects.GetNonInterpretablePhases(serum);
            if (serum == SerumTreatment.AutoAdsorption)
                lines.Add("Serum: autoadsorbed. Autoantibody removed, alloantibodies retained (unless recent transfusion or transplant).");
            else if (removed.Count > 0)
                lines.Add($"Serum: {AntigenTreatmentEffects.GetDisplayName(serum)}. Removes {string.Join(", ", removed)}.");
            if (phases.Count > 0)
                lines.Add($"Serum: {string.Join(", ", phases)} phase not interpretable (cold/IgM reactors suppressed).");
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Grades may be copied only into an untreated repeat. A treated run exists to show how the treatment changes
        /// the reactions, so its grades must be read from the treated test; copied untreated grades would claim the
        /// treatment changed nothing.
        /// </summary>
        public static bool CanCopyGrades(CellTreatment cell, SerumTreatment serum) =>
            cell == CellTreatment.None && serum == SerumTreatment.None;
    }
}
