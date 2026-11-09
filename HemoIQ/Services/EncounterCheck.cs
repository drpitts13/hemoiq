using System;
using System.Linq;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>
    /// An encounter or account number belongs to one patient. When the number entered on
    /// a specimen is already recorded on a specimen with a different MRN, one of the two
    /// may be mislabeled or the number mistyped. This is a warning for review; it never
    /// blocks the save.
    /// </summary>
    public static class EncounterCheck
    {
        public static string? Describe(DatabaseService db, string accession, string? encounter, string? mrn)
        {
            var value = DatabaseService.NormalizeEncounter(encounter);
            var ownMrn = PatientHistory.NormalizeMrn(mrn);
            if (value == null || ownMrn == null) return null;
            var others = db.GetSpecimensByEncounter(value)
                .Where(s => !string.Equals(s.AccessionNumber, accession, StringComparison.OrdinalIgnoreCase))
                .Where(s => s.Mrn != null && !string.Equals(s.Mrn, ownMrn, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (others.Count == 0) return null;
            var list = string.Join(", ", others.Select(s => $"{s.AccessionNumber} (MRN {s.Mrn})"));
            return $"Encounter {value} is already recorded on {list}, but this specimen has MRN {ownMrn}. " +
                   "Check the specimen label, the MRN and the encounter number before continuing.";
        }
    }
}
