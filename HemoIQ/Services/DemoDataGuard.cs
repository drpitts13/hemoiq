using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>
    /// Load Demo Data is for training and evaluation databases. Its specimens use realistic accession numbers and
    /// its lots carry real vendor names and are created active, so loading it into a laboratory database would mix
    /// synthetic results into real records with nothing to tell them apart. It is therefore allowed only into an
    /// empty database or one that holds nothing but demo records, and the whole load is one transaction.
    /// </summary>
    public static class DemoDataGuard
    {
        /// <summary>Every accession the demo seeders create.</summary>
        public static IReadOnlySet<string> SeededAccessions { get; } = new HashSet<string>(
            ClinicalDataSeeder.SpecimenIds
                .Concat(DemoDataSeeder.ScenarioIds)
                .Concat(E2EWorkflowSeeder.SpecimenIds),
            StringComparer.OrdinalIgnoreCase);

        /// <summary>Null when demo data may be loaded; otherwise why not, for the user.</summary>
        public static string? RefusalReason(DatabaseService db)
        {
            var specimens = db.GetAllSpecimens();
            var panels = db.GetAllPanels();
            if (specimens.Count == 0 && panels.Count == 0) return null;

            var real = specimens.Where(s => !SeededAccessions.Contains(s.AccessionNumber)).ToList();
            if (specimens.Count > 0 && real.Count == 0) return null; // a demo database: fill in any missing scenarios

            var what = real.Count > 0
                ? $"{real.Count} specimen(s) that are not demo records (for example {real[0].AccessionNumber})"
                : $"{panels.Count} panel lot(s) and no demo specimens";
            return "Demo data can only be loaded into an empty database or one that holds only demo data, because demo " +
                   "specimens and lots look like real ones. This database holds " + what + ".\n\n" +
                   "To try the demo, start HemoIQ with a separate database (set the HEMOIQ_DB environment variable to a " +
                   "new file) and load it there. Nothing was added.";
        }

        /// <summary>Loads every demo set in one transaction, so a failure part-way leaves nothing behind.</summary>
        public static void Load(DatabaseService db, AntibodyAnalyzer analyzer)
        {
            if (RefusalReason(db) is string reason) throw new InvalidOperationException(reason);
            db.RunInTransaction(() =>
            {
                ClinicalDataSeeder.SeedIfNeeded(db, analyzer);
                DemoDataSeeder.SeedIfNeeded(db);
                DemoDataSeeder.AnalyzeIfNeverAnalyzed(db, analyzer);
                E2EWorkflowSeeder.SeedIfNeeded(db, analyzer);
            });
        }
    }
}
