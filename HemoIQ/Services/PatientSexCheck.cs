using System.Collections.Generic;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Cross-checks the specimen's pregnancy context against the patient's recorded sex.
    /// A pregnancy on a patient recorded as male usually means the wrong patient record,
    /// the wrong specimen, or a data-entry error; the pregnancy context still applies
    /// to specimen dating until someone corrects it. Review only.
    /// </summary>
    public static class PatientSexCheck
    {
        public static string? PregnancyConflict(Patient? patient, Specimen? specimen) =>
            PregnancyConflict(patient?.Sex, specimen?.PregnancyStatus, patient?.Mrn);

        public static string? PregnancyConflict(string? sex, string? pregnancyStatus, string? mrn)
        {
            if (PatientSex.Normalize(sex) != PatientSex.Male) return null;
            var preg = PregnancyStatus.Normalize(pregnancyStatus);
            if (preg is not (PregnancyStatus.Pregnant or PregnancyStatus.Recent)) return null;
            return $"The specimen records {PregnancyStatus.Label(preg).ToLowerInvariant()}, but patient {mrn} is recorded as male. " +
                   "Check that the specimen belongs to this patient and correct the pregnancy status or the patient record.";
        }

        public static List<string> ReviewNotes(Patient? patient, Specimen? specimen)
        {
            var notes = new List<string>();
            if (PregnancyConflict(patient, specimen) is string n) notes.Add(n);
            return notes;
        }
    }
}
