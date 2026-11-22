using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Data
{
    /// <summary>
    /// Full CRUD layer over hemoiq.db (mirrors database.py).
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly SqliteConnection _conn;

        public string DbPath { get; }

        public string ArtifactDirectory =>
            Path.Combine(Path.GetDirectoryName(DbPath) ?? ".",
                Path.GetFileNameWithoutExtension(DbPath) + "-artifacts");

        public DatabaseService(string? dbPath = null)
        {
            DbPath = dbPath ?? DefaultDbPath();
            _conn = new SqliteConnection($"Data Source={DbPath};Pooling=False");
            // If opening or migrating fails (a read-only file or share, a file another program holds), release the
            // file before the error reaches the caller, so nothing is left holding it.
            try
            {
                _conn.Open();
                // SQLite opens a read-only file (or a file on a read-only share) quietly in read-only mode, and an
                // up-to-date database needs no write at start-up, so the problem would only appear on the first save.
                // Refuse at open instead, with SQLite's own read-only error, so start-up can explain it.
                if (SQLitePCL.raw.sqlite3_db_readonly(_conn.Handle, "main") == 1)
                    throw new SqliteException("attempt to write a readonly database", 8);
                EnableForeignKeys();
                CreateTables();
                MigrateSpecimenTimestamps();
                MigratePanelStartCell();
                MigrateActiveFlag();
                MigrateReactionsToRuns();
                MigrateSpecimenClinical();
                MigrateSpecimenFinalCall();
                MigrateWarehouseAntigens();
                MigratePanelAntigenOrder();
                MigrateVendorPanelMetadata();
                MigrateAuditAndSnapshots();
                MigrateReactionExtraPhases();
                MigrateAnalysisSnapshotRules();
                MigratePatients();
                ExecNonQuery(@"
                    CREATE TABLE IF NOT EXISTS users (
                        login TEXT PRIMARY KEY COLLATE NOCASE CHECK (length(login) BETWEEN 1 AND 128),
                        initials TEXT NOT NULL CHECK (length(initials) BETWEEN 1 AND 12),
                        name TEXT,
                        role TEXT NOT NULL CHECK (role IN ('technologist','supervisor','administrator','readonly')),
                        active INTEGER NOT NULL DEFAULT 1
                    )");
                MigrateUserRoles();
                ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_audit_entity ON audit_events(entity_type, entity_id)");
                CreateForeignKeyIndexes();
                DeactivateExpiredSpecimens();
                DeactivateExpiredPanels();
            }
            catch
            {
                _conn.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Indexes on child-table foreign keys that no UNIQUE constraint already covers. The latest analysis
        /// snapshot, a panel's cells and a specimen's stored antibodies and rule-outs are read once per specimen
        /// or panel (staleness, reports, status strip); without an index each read scans the whole table, which
        /// grows with every analysis. Cascading deletes use them too.
        /// </summary>
        private void CreateForeignKeyIndexes()
        {
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_snapshots_specimen ON analysis_snapshots(specimen_id, id)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_panel_cells_panel ON panel_cells(panel_id)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_specimen_antibodies_specimen ON specimen_antibodies(specimen_id)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_specimen_ruleouts_specimen ON specimen_ruleouts(specimen_id)");
            // The audit log is the largest table (every reaction save). The latest rule/Preferences change and
            // per-specimen action counts are read once per specimen by reports and the status strip; they filter
            // by action, which ix_audit_entity (entity_type first) cannot serve.
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_audit_action ON audit_events(action, id)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_audit_action_entity ON audit_events(action, entity_id)");
        }

        public static string DefaultDbPath()
        {
            // ANTIBODY_PANELS_DB is the variable name used before the rename to HemoIQ.
            var envPath = Environment.GetEnvironmentVariable("HEMOIQ_DB");
            if (string.IsNullOrEmpty(envPath))
                envPath = Environment.GetEnvironmentVariable("ANTIBODY_PANELS_DB");
            if (!string.IsNullOrEmpty(envPath))
                return envPath;

            // Prefer an existing database beside the executable (dev / portable runs).
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var name in new[] { AppDataPaths.DatabaseFileName }.Concat(AppDataPaths.LegacyDatabaseFileNames))
            {
                var local = Path.Combine(exeDir, name);
                if (File.Exists(local))
                    return local;
            }

            // Installed / first-run: store user data in AppData (Program Files is not writable).
            var appData = AppDataPaths.Root;
            Directory.CreateDirectory(appData);
            return Path.Combine(appData, AppDataPaths.DatabaseFileName);
        }

        private void EnableForeignKeys()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        // ── Schema ──────────────────────────────────────────────────────────

        private void CreateTables()
        {
            var antigenCols = string.Join(", ",
                GetAllAntigenColumns(col => $"{col} TEXT DEFAULT '-'"));

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS specimens (
                    accession_number TEXT PRIMARY KEY,
                    type TEXT NOT NULL DEFAULT 'serum',
                    expiration_date TEXT,
                    created_date TEXT NOT NULL
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS specimen_antibodies (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    specimen_id TEXT NOT NULL,
                    antibody TEXT NOT NULL,
                    probability REAL NOT NULL,
                    FOREIGN KEY (specimen_id) REFERENCES specimens(accession_number) ON DELETE CASCADE
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS specimen_ruleouts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    specimen_id TEXT NOT NULL,
                    antibody TEXT NOT NULL,
                    ruleout_count INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (specimen_id) REFERENCES specimens(accession_number) ON DELETE CASCADE
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS panels (
                    panel_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    lot_number TEXT,
                    vendor TEXT,
                    num_cells INTEGER NOT NULL,
                    expiration_date TEXT,
                    include_ac INTEGER DEFAULT 0
                )");

            ExecNonQuery($@"
                CREATE TABLE IF NOT EXISTS panel_cells (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    panel_id INTEGER NOT NULL,
                    cell_number TEXT NOT NULL,
                    {antigenCols},
                    FOREIGN KEY (panel_id) REFERENCES panels(panel_id) ON DELETE CASCADE
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS specimen_panels (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    specimen_id TEXT NOT NULL,
                    panel_id INTEGER NOT NULL,
                    FOREIGN KEY (specimen_id) REFERENCES specimens(accession_number) ON DELETE CASCADE,
                    FOREIGN KEY (panel_id) REFERENCES panels(panel_id) ON DELETE CASCADE,
                    UNIQUE(specimen_id, panel_id)
                )");

            // panel_runs: one row per (specimen, panel, cell-treatment, serum-treatment) combination.
            // This replaces the old implicit single-run assumption in the reactions table.
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS panel_runs (
                    run_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    specimen_id TEXT NOT NULL,
                    panel_id INTEGER NOT NULL,
                    cell_treatment TEXT NOT NULL DEFAULT 'None',
                    serum_treatment TEXT NOT NULL DEFAULT 'None',
                    label TEXT,
                    created_date TEXT NOT NULL,
                    FOREIGN KEY (specimen_id) REFERENCES specimens(accession_number) ON DELETE CASCADE,
                    FOREIGN KEY (panel_id) REFERENCES panels(panel_id) ON DELETE CASCADE,
                    UNIQUE(specimen_id, panel_id, cell_treatment, serum_treatment)
                )");

            // New reactions schema keyed on run_id instead of (specimen_id, panel_id).
            // MigrateReactionsToRuns handles upgrading existing databases.
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS reactions (
                    reaction_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    run_id INTEGER NOT NULL,
                    cell_number TEXT NOT NULL,
                    ""IS"" TEXT DEFAULT 'NT',
                    C37 TEXT DEFAULT 'NT',
                    AHG TEXT DEFAULT 'NT',
                    CC TEXT DEFAULT 'NT',
                    FOREIGN KEY (run_id) REFERENCES panel_runs(run_id) ON DELETE CASCADE,
                    UNIQUE(run_id, cell_number)
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS rules (
                    rule_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT,
                    antibody TEXT NOT NULL,
                    exception_antigen TEXT,
                    heterozygous_ok INTEGER DEFAULT 0,
                    min_ruleout_count INTEGER DEFAULT 3
                )");
        }

        private void MigrateSpecimenTimestamps()
        {
            var cols = GetColumnNames("specimens");
            if (!cols.Contains("reactions_updated_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN reactions_updated_at TEXT");
            if (!cols.Contains("last_analyzed_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN last_analyzed_at TEXT");
            if (!cols.Contains("analysis_config_seq"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN analysis_config_seq INTEGER");
        }

        private void MigratePanelStartCell()
        {
            var cols = GetColumnNames("panels");
            if (!cols.Contains("start_cell"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN start_cell INTEGER NOT NULL DEFAULT 1");
        }

        private void MigrateActiveFlag()
        {
            var specCols = GetColumnNames("specimens");
            if (!specCols.Contains("is_active"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN is_active INTEGER NOT NULL DEFAULT 1");

            var panelCols = GetColumnNames("panels");
            if (!panelCols.Contains("is_active"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN is_active INTEGER NOT NULL DEFAULT 1");
        }

        private void MigrateSpecimenClinical()
        {
            var cols = GetColumnNames("specimens");
            if (!cols.Contains("notes"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN notes TEXT");
            if (!cols.Contains("phenotype"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN phenotype TEXT");
            if (!cols.Contains("previous_antibodies"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN previous_antibodies TEXT");
            if (!cols.Contains("dat_result"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN dat_result TEXT");
            if (!cols.Contains("genotype"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN genotype TEXT");
            if (!cols.Contains("transfusion_status"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN transfusion_status TEXT NOT NULL DEFAULT 'unknown' " +
                    "CHECK (transfusion_status IN ('unknown','none','transfused'))");
            if (!cols.Contains("last_transfusion_date"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN last_transfusion_date TEXT");
            if (!cols.Contains("transfusion_products"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN transfusion_products TEXT");
            if (!cols.Contains("transfusion_units"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN transfusion_units INTEGER");
            if (!cols.Contains("dat_igg"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN dat_igg TEXT");
            if (!cols.Contains("dat_c3"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN dat_c3 TEXT");
            if (!cols.Contains("hemolysis"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN hemolysis TEXT");
            if (!cols.Contains("mrn"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN mrn TEXT");
            if (!cols.Contains("pregnancy_status"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN pregnancy_status TEXT NOT NULL DEFAULT 'unknown' " +
                    "CHECK (pregnancy_status IN ('unknown','none','pregnant','recent'))");
            if (!cols.Contains("pregnancy_end_date"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN pregnancy_end_date TEXT");
            if (!cols.Contains("rhig_date"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN rhig_date TEXT");
            if (!cols.Contains("collected_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN collected_at TEXT");
            if (!cols.Contains("received_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN received_at TEXT");
            foreach (var col in new[] { "collector", "collection_location", "ordering_location", "ordering_provider" })
                if (!cols.Contains(col))
                    ExecNonQuery($"ALTER TABLE specimens ADD COLUMN {col} TEXT CHECK ({col} IS NULL OR length({col}) <= {OriginMaxLength})");
            if (!cols.Contains("source_accession"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN source_accession TEXT");
            if (!cols.Contains("encounter_number"))
                ExecNonQuery($"ALTER TABLE specimens ADD COLUMN encounter_number TEXT CHECK (encounter_number IS NULL OR length(encounter_number) <= {EncounterMaxLength})");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_specimens_source ON specimens(source_accession)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_specimens_encounter ON specimens(encounter_number COLLATE NOCASE)");
            ExecNonQuery("CREATE INDEX IF NOT EXISTS ix_specimens_mrn ON specimens(mrn)");
        }

        private void MigratePatients()
        {
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS patients (
                    mrn TEXT PRIMARY KEY CHECK (length(mrn) BETWEEN 1 AND 64),
                    name TEXT,
                    date_of_birth TEXT,
                    blood_bank_comments TEXT,
                    transfusion_requirements TEXT,
                    updated_at TEXT
                )");
            var patientCols = GetColumnNames("patients");
            if (!patientCols.Contains("antigen_negative"))
                ExecNonQuery("ALTER TABLE patients ADD COLUMN antigen_negative TEXT");
            if (!patientCols.Contains("hpc_transplant"))
                ExecNonQuery("ALTER TABLE patients ADD COLUMN hpc_transplant TEXT NOT NULL DEFAULT 'none'");
            if (!patientCols.Contains("hpc_transplant_date"))
                ExecNonQuery("ALTER TABLE patients ADD COLUMN hpc_transplant_date TEXT");
            if (!patientCols.Contains("sex"))
                ExecNonQuery("ALTER TABLE patients ADD COLUMN sex TEXT NOT NULL DEFAULT 'unknown' " +
                             "CHECK (sex IN ('unknown','female','male','other'))");
            var ruleCols = GetColumnNames("rules");
            if (!ruleCols.Contains("separation_exempt"))
                ExecNonQuery("ALTER TABLE rules ADD COLUMN separation_exempt INTEGER NOT NULL DEFAULT 0");
            if (!ruleCols.Contains("separation_exempt_from"))
                ExecNonQuery("ALTER TABLE rules ADD COLUMN separation_exempt_from TEXT");
        }

        private void MigrateSpecimenFinalCall()
        {
            var cols = GetColumnNames("specimens");
            if (!cols.Contains("final_antibodies"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN final_antibodies TEXT");
            if (!cols.Contains("final_comment"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN final_comment TEXT");
            if (!cols.Contains("identified_by"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN identified_by TEXT");
            if (!cols.Contains("identified_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN identified_at TEXT");
            if (!cols.Contains("reviewed_by"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN reviewed_by TEXT");
            if (!cols.Contains("reviewed_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN reviewed_at TEXT");
            if (!cols.Contains("cancelled_at"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN cancelled_at TEXT");
            if (!cols.Contains("cancel_reason"))
                ExecNonQuery("ALTER TABLE specimens ADD COLUMN cancel_reason TEXT");
        }

        /// <summary>
        /// Warehouse antigens are stored per-panel rather than as wide columns so
        /// existing panels are not silently typed as antigen-negative.
        /// </summary>
        private void MigrateWarehouseAntigens()
        {
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS panel_extra_antigens (
                    panel_id INTEGER NOT NULL,
                    antigen_name TEXT NOT NULL,
                    PRIMARY KEY (panel_id, antigen_name),
                    FOREIGN KEY (panel_id) REFERENCES panels(panel_id) ON DELETE CASCADE
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS panel_cell_extra_antigens (
                    cell_id INTEGER NOT NULL,
                    antigen_name TEXT NOT NULL,
                    value TEXT NOT NULL DEFAULT '-',
                    PRIMARY KEY (cell_id, antigen_name),
                    FOREIGN KEY (cell_id) REFERENCES panel_cells(id) ON DELETE CASCADE
                )");
        }

        private void MigratePanelAntigenOrder()
        {
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS panel_antigen_order (
                    panel_id INTEGER NOT NULL,
                    antigen_name TEXT NOT NULL,
                    sort_index INTEGER NOT NULL,
                    PRIMARY KEY (panel_id, antigen_name),
                    FOREIGN KEY (panel_id) REFERENCES panels(panel_id) ON DELETE CASCADE
                )");
        }

        /// <summary>Databases created before the Read-only role have a role CHECK without it; rebuild the table to allow it.</summary>
        private void MigrateUserRoles()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'users'";
            var sql = cmd.ExecuteScalar() as string;
            if (sql == null || sql.Contains("'readonly'")) return;
            RunInTransaction(() =>
            {
                ExecNonQuery(@"
                    CREATE TABLE users_v2 (
                        login TEXT PRIMARY KEY COLLATE NOCASE CHECK (length(login) BETWEEN 1 AND 128),
                        initials TEXT NOT NULL CHECK (length(initials) BETWEEN 1 AND 12),
                        name TEXT,
                        role TEXT NOT NULL CHECK (role IN ('technologist','supervisor','administrator','readonly')),
                        active INTEGER NOT NULL DEFAULT 1
                    )");
                ExecNonQuery("INSERT INTO users_v2 (login, initials, name, role, active) SELECT login, initials, name, role, active FROM users");
                ExecNonQuery("DROP TABLE users");
                ExecNonQuery("ALTER TABLE users_v2 RENAME TO users");
            });
        }

        private void MigrateVendorPanelMetadata()
        {
            var panelCols = GetColumnNames("panels");
            if (!panelCols.Contains("catalog_number"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN catalog_number TEXT");
            if (!panelCols.Contains("product_line"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN product_line TEXT");
            if (!panelCols.Contains("enzyme_treated"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN enzyme_treated INTEGER NOT NULL DEFAULT 0");
            if (!panelCols.Contains("source_url"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN source_url TEXT");
            if (!panelCols.Contains("source_format"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN source_format TEXT");
            if (!panelCols.Contains("imported_at"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN imported_at TEXT");
            if (!panelCols.Contains("special_notes"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN special_notes TEXT");
            if (!panelCols.Contains("source_sha256"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN source_sha256 TEXT");
            if (!panelCols.Contains("source_artifact_path"))
                ExecNonQuery("ALTER TABLE panels ADD COLUMN source_artifact_path TEXT");

            var cellCols = GetColumnNames("panel_cells");
            if (!cellCols.Contains("donor_id"))
                ExecNonQuery("ALTER TABLE panel_cells ADD COLUMN donor_id TEXT");
            if (!cellCols.Contains("rh_phenotype"))
                ExecNonQuery("ALTER TABLE panel_cells ADD COLUMN rh_phenotype TEXT");
            if (!cellCols.Contains("special_types"))
                ExecNonQuery("ALTER TABLE panel_cells ADD COLUMN special_types TEXT");
        }

        private void MigrateAuditAndSnapshots()
        {
            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS audit_events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    occurred_at_utc TEXT NOT NULL,
                    operator TEXT NOT NULL,
                    action TEXT NOT NULL,
                    entity_type TEXT,
                    entity_id TEXT,
                    reason TEXT,
                    before_json TEXT,
                    after_json TEXT
                )");

            ExecNonQuery(@"
                CREATE TABLE IF NOT EXISTS analysis_snapshots (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    specimen_id TEXT NOT NULL,
                    analyzed_at_utc TEXT NOT NULL,
                    software_version TEXT NOT NULL,
                    settings_json TEXT NOT NULL,
                    input_fingerprint TEXT NOT NULL,
                    ruled_out_json TEXT,
                    suspected_json TEXT,
                    acs_json TEXT,
                    FOREIGN KEY (specimen_id) REFERENCES specimens(accession_number) ON DELETE CASCADE
                )");
        }

        private void MigrateReactionExtraPhases()
        {
            var cols = GetColumnNames("reactions");
            if (!cols.Contains("extra_phases"))
                ExecNonQuery("ALTER TABLE reactions ADD COLUMN extra_phases TEXT");
        }

        private void MigrateAnalysisSnapshotRules()
        {
            var cols = GetColumnNames("analysis_snapshots");
            if (!cols.Contains("rule_engine_version"))
                ExecNonQuery("ALTER TABLE analysis_snapshots ADD COLUMN rule_engine_version TEXT");
            if (!cols.Contains("rules_json"))
                ExecNonQuery("ALTER TABLE analysis_snapshots ADD COLUMN rules_json TEXT");
            if (!cols.Contains("analyzed_by"))
                ExecNonQuery("ALTER TABLE analysis_snapshots ADD COLUMN analyzed_by TEXT");
            if (!cols.Contains("absorption_json"))
                ExecNonQuery("ALTER TABLE analysis_snapshots ADD COLUMN absorption_json TEXT");
            if (!cols.Contains("additional_testing_json"))
                ExecNonQuery("ALTER TABLE analysis_snapshots ADD COLUMN additional_testing_json TEXT");
        }

        private void DeactivateExpiredSpecimens() =>
            DeactivateExpired("specimens", "accession_number", "specimen");

        private void DeactivateExpiredPanels() =>
            DeactivateExpired("panels", "panel_id", "panel");

        /// <summary>
        /// Marks records whose expiration date has passed as inactive, at startup. Each one is
        /// audited (auto_deactivate_expired, with the expiration date) so the status change is
        /// never silent and appears on the record's history.
        /// </summary>
        private void DeactivateExpired(string table, string keyColumn, string entityType)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var expired = new List<(string Id, string Expiration)>();
            using (var select = _conn.CreateCommand())
            {
                select.CommandText = $@"
                    SELECT {keyColumn}, expiration_date FROM {table}
                    WHERE expiration_date IS NOT NULL AND expiration_date < $today AND is_active = 1";
                select.Parameters.AddWithValue("$today", today);
                using var r = select.ExecuteReader();
                while (r.Read()) expired.Add((Convert.ToString(r.GetValue(0))!, r.GetString(1)));
            }
            if (expired.Count == 0) return;
            RunInTransaction(() =>
            {
                foreach (var (id, expiration) in expired)
                {
                    using var cmd = _conn.CreateCommand();
                    cmd.CommandText = $"UPDATE {table} SET is_active = 0 WHERE {keyColumn} = $id AND is_active = 1";
                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.ExecuteNonQuery();
                    AppendAudit("auto_deactivate_expired", entityType, id, $"Expired {expiration}; deactivated automatically at startup",
                        JsonSerializer.Serialize(new { isActive = true, expiration }),
                        JsonSerializer.Serialize(new { isActive = false, expiration }));
                }
            });
        }

        /// <summary>
        /// Upgrades existing databases that used the old reactions schema
        /// (specimen_id, panel_id, cell_number unique) to the new panel_runs-based schema.
        /// Safe to call on already-migrated databases (no-op when run_id column exists).
        /// </summary>
        private void MigrateReactionsToRuns()
        {
            var reactionCols = GetColumnNames("reactions");
            if (reactionCols.Contains("run_id")) return; // already migrated

            // The old reactions table has (specimen_id, panel_id, cell_number) columns.
            // We need to:
            //   1. Create panel_runs rows for every unique (specimen_id, panel_id) pair.
            //   2. Rebuild the reactions table with run_id as the FK.

            using var tx = _conn.BeginTransaction();
            try
            {
                // 1. Seed default (untreated) runs for every existing specimen-panel combo.
                ExecNonQuery(@"
                    INSERT OR IGNORE INTO panel_runs
                        (specimen_id, panel_id, cell_treatment, serum_treatment, label, created_date)
                    SELECT DISTINCT specimen_id, panel_id, 'None', 'None', 'Untreated', date('now')
                    FROM reactions");

                // 2. Create new reactions table.
                ExecNonQuery(@"
                    CREATE TABLE reactions_v2 (
                        reaction_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        run_id INTEGER NOT NULL,
                        cell_number TEXT NOT NULL,
                        ""IS"" TEXT DEFAULT 'NT',
                        C37 TEXT DEFAULT 'NT',
                        AHG TEXT DEFAULT 'NT',
                        CC TEXT DEFAULT 'NT',
                        FOREIGN KEY (run_id) REFERENCES panel_runs(run_id) ON DELETE CASCADE,
                        UNIQUE(run_id, cell_number)
                    )");

                // 3. Copy rows; join on the untreated run we just created.
                ExecNonQuery(@"
                    INSERT INTO reactions_v2 (run_id, cell_number, ""IS"", C37, AHG, CC)
                    SELECT pr.run_id, r.cell_number, r.""IS"", r.C37, r.AHG, r.CC
                    FROM reactions r
                    JOIN panel_runs pr
                        ON pr.specimen_id = r.specimen_id
                       AND pr.panel_id    = r.panel_id
                       AND pr.cell_treatment  = 'None'
                       AND pr.serum_treatment = 'None'");

                // 4. Swap tables.
                ExecNonQuery("DROP TABLE reactions");
                ExecNonQuery("ALTER TABLE reactions_v2 RENAME TO reactions");

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private HashSet<string> GetColumnNames(string table)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var r = cmd.ExecuteReader();
            while (r.Read()) set.Add(r.GetString(1));
            return set;
        }

        // ── Specimens ────────────────────────────────────────────────────────

        public void AddSpecimen(string accessionNumber, string type = "serum", string? expirationDate = null, bool? isActive = null,
            string? notes = null, string? phenotype = null, string? previousAntibodies = null, string? datResult = null,
            string? createdDate = null, string? genotype = null)
        {
            EnsureCanRecord();
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var created = string.IsNullOrWhiteSpace(createdDate) ? today : createdDate.Trim();
            bool active = isActive ?? (expirationDate == null || string.Compare(expirationDate, today) >= 0);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO specimens (accession_number, type, expiration_date, created_date, is_active,
                    notes, phenotype, previous_antibodies, dat_result, genotype)
                VALUES ($acc, $type, $exp, $created, $active, $notes, $pheno, $prev, $dat, $geno)";
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$exp", (object?)expirationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$created", created);
            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pheno", (object?)phenotype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$prev", (object?)previousAntibodies ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dat", (object?)datResult ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$geno", (object?)genotype ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            AppendAudit("add_specimen", "specimen", accessionNumber, null, null,
                JsonSerializer.Serialize(new { type, createdDate = created, expirationDate, isActive = active }));
        }

        public Specimen? GetSpecimen(string accessionNumber)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadSpecimen(r) : null;
        }

        public List<Specimen> GetAllSpecimens()
        {
            var list = new List<Specimen>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens ORDER BY created_date DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadSpecimen(r));
            return list;
        }

        public List<Specimen> GetActiveSpecimens()
        {
            var list = new List<Specimen>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens WHERE is_active = 1 ORDER BY created_date DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadSpecimen(r));
            return list;
        }

        public void UpdateSpecimen(string accessionNumber, string type, string? expirationDate, bool isActive = true,
            string? notes = null, string? phenotype = null, string? previousAntibodies = null, string? datResult = null,
            string? genotype = null)
        {
            EnsureCanRecord();
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            var changed = new Dictionary<string, (object? before, object? after)>();
            void Diff(string field, object? b, object? a)
            {
                if (!Equals(b, a)) changed[field] = (b, a);
            }
            Diff("type", before.Type, type);
            Diff("expirationDate", before.ExpirationDate, expirationDate);
            Diff("isActive", before.IsActive, isActive);
            Diff("notes", before.Notes, notes);
            Diff("phenotype", before.Phenotype, phenotype);
            Diff("previousAntibodies", before.PreviousAntibodies, previousAntibodies);
            Diff("datResult", before.DatResult, datResult);
            Diff("genotype", before.Genotype, genotype);
            if (changed.Count == 0) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE specimens SET type = $type, expiration_date = $exp, is_active = $active,
                    notes = $notes, phenotype = $pheno, previous_antibodies = $prev, dat_result = $dat,
                    genotype = $geno
                WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$exp", (object?)expirationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pheno", (object?)phenotype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$prev", (object?)previousAntibodies ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dat", (object?)datResult ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$geno", (object?)genotype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_specimen", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(changed.ToDictionary(c => c.Key, c => c.Value.before)),
                JsonSerializer.Serialize(changed.ToDictionary(c => c.Key, c => c.Value.after)));
            if (changed.Keys.Any(k => k is "type" or "notes" or "phenotype" or "previousAntibodies" or "datResult" or "genotype"))
                MarkAnalysisInputChanged(accessionNumber);
            // An earlier specimen's phenotype can stand in for unreliable current typing on the patient's other specimens.
            if (changed.ContainsKey("phenotype"))
                MarkSamePatientSpecimensChanged(accessionNumber);
        }

        /// <summary>
        /// Records structured transfusion history. Audited with before and
        /// after values because it changes how patient typing is interpreted.
        /// </summary>
        /// <summary>
        /// Records which products were transfused and how many units. Kept only while the status is
        /// Transfused. Audited (<c>update_transfusion_products</c>); locked after a confirmed identification.
        /// </summary>
        public void UpdateSpecimenTransfusionProducts(string accessionNumber, IEnumerable<string>? products, int? units)
        {
            EnsureCanRecord();
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            var transfused = TransfusionStatus.Normalize(before.TransfusionStatus) == TransfusionStatus.Transfused;
            if (units is < 1 or > 999)
                throw new ArgumentException("Units transfused must be between 1 and 999.");
            var codes = transfused ? Models.TransfusionProducts.Format(products) : null;
            var count = transfused ? units : null;
            if (before.TransfusionProducts == codes && before.TransfusionUnits == count) return;
            EnsureSpecimenUnlocked(accessionNumber);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET transfusion_products = $p, transfusion_units = $u WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$p", (object?)codes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$u", (object?)count ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_transfusion_products", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { products = before.TransfusionProducts, units = before.TransfusionUnits }),
                JsonSerializer.Serialize(new { products = codes, units = count }));
            MarkAnalysisInputChanged(accessionNumber);
            MarkSamePatientSpecimensChanged(accessionNumber); // the patient's other specimens read transfusion history
        }

        public void UpdateSpecimenTransfusion(string accessionNumber, string? status, string? lastTransfusionDate)
        {
            EnsureCanRecord();
            var code = TransfusionStatus.Normalize(status);
            string? date = null;
            if (code == TransfusionStatus.Transfused && !string.IsNullOrWhiteSpace(lastTransfusionDate))
            {
                if (!DateTime.TryParse(lastTransfusionDate.Trim(), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var d))
                    throw new ArgumentException($"Last transfusion date '{lastTransfusionDate}' is not a valid date.");
                if (d.Date > DateTime.Today)
                    throw new ArgumentException("Last transfusion date cannot be in the future.");
                date = d.ToString("yyyy-MM-dd");
            }

            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.TransfusionStatus == code && before.LastTransfusionDate == date) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE specimens SET transfusion_status = $status, last_transfusion_date = $date
                WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$status", code);
            cmd.Parameters.AddWithValue("$date", (object?)date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_transfusion", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { status = before.TransfusionStatus, lastTransfusionDate = before.LastTransfusionDate }),
                JsonSerializer.Serialize(new { status = code, lastTransfusionDate = date }));
            MarkAnalysisInputChanged(accessionNumber);
            MarkSamePatientSpecimensChanged(accessionNumber); // the patient's other specimens read transfusion history
        }

        /// <summary>
        /// Records monospecific IgG and C3 DAT results. NT or blank clears a
        /// value. Audited with before and after values.
        /// </summary>
        public void UpdateSpecimenDatSplit(string accessionNumber, string? igg, string? c3)
        {
            EnsureCanRecord();
            static string? Clean(string? v, string name)
            {
                if (string.IsNullOrWhiteSpace(v) || v.Trim().Equals("NT", StringComparison.OrdinalIgnoreCase))
                    return null;
                var match = AntigenConstants.DatResults.FirstOrDefault(
                    d => string.Equals(d, v.Trim(), StringComparison.OrdinalIgnoreCase));
                return match ?? throw new ArgumentException($"DAT {name} value '{v}' is not a recognized grade.");
            }
            var newIgg = Clean(igg, "IgG");
            var newC3 = Clean(c3, "C3");

            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.DatIgg == newIgg && before.DatC3 == newC3) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET dat_igg = $igg, dat_c3 = $c3 WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$igg", (object?)newIgg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$c3", (object?)newC3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_dat", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { igg = before.DatIgg, c3 = before.DatC3 }),
                JsonSerializer.Serialize(new { igg = newIgg, c3 = newC3 }));
            MarkAnalysisInputChanged(accessionNumber);
        }

        /// <summary>Records the specimen's visual hemolysis. Audited; locked after a confirmed identification.</summary>
        public void UpdateSpecimenHemolysis(string accessionNumber, string? hemolysis)
        {
            EnsureCanRecord();
            var level = Services.SpecimenHemolysis.Normalize(hemolysis);
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.Hemolysis == level) return;
            EnsureSpecimenUnlocked(accessionNumber);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET hemolysis = $h WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$h", level.Length == 0 ? DBNull.Value : level);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_hemolysis", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { hemolysis = before.Hemolysis }),
                JsonSerializer.Serialize(new { hemolysis = level }));
            // Analysis prints hemolysis REVIEW notes from this value.
            MarkAnalysisInputChanged(accessionNumber);
        }

        public List<AppUser> GetUsers()
        {
            var list = new List<AppUser>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT login, initials, name, role, active FROM users ORDER BY active DESC, login";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AppUser
                {
                    Login = r.GetString(0),
                    Initials = r.GetString(1),
                    Name = r.IsDBNull(2) ? string.Empty : r.GetString(2),
                    Role = r.GetString(3),
                    Active = r.GetInt32(4) != 0,
                });
            return list;
        }

        /// <summary>
        /// Adds or updates a registered user. Requires Manage users (always allowed while
        /// no user exists). The first active user must be an Administrator, and the last
        /// active Administrator cannot be demoted or deactivated. Audited.
        /// </summary>
        public void SaveUser(AppUser user)
        {
            Services.AccessControl.Demand(this, Services.Permission.ManageUsers);
            var login = (user.Login ?? string.Empty).Trim();
            var initials = LabSettings.NormalizeInitials(user.Initials);
            if (login.Length == 0) throw new ArgumentException("Windows login is required.");
            if (initials.Length == 0) throw new ArgumentException("Initials are required.");
            if (!UserRole.IsValid(user.Role)) throw new ArgumentException($"Unknown role '{user.Role}'.");

            var users = GetUsers();
            var before = users.FirstOrDefault(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
            var otherActiveAdmins = users.Count(u => u.Active && u.Role == UserRole.Administrator &&
                !string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
            var willBeActiveAdmin = user.Active && user.Role == UserRole.Administrator;
            if (otherActiveAdmins == 0 && !willBeActiveAdmin)
                throw new InvalidOperationException(before == null && !users.Any(u => u.Active)
                    ? "The first user must be an active Administrator."
                    : "At least one active Administrator is required; this change would leave none.");
            if (users.Any(u => u.Active && string.Equals(u.Initials, initials, StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Initials {initials} already belong to another active user.");

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO users (login, initials, name, role, active) VALUES ($l, $i, $n, $r, $a)
                ON CONFLICT(login) DO UPDATE SET initials = $i, name = $n, role = $r, active = $a";
            cmd.Parameters.AddWithValue("$l", login);
            cmd.Parameters.AddWithValue("$i", initials);
            cmd.Parameters.AddWithValue("$n", (object?)(string.IsNullOrWhiteSpace(user.Name) ? null : user.Name.Trim()) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$r", user.Role);
            cmd.Parameters.AddWithValue("$a", user.Active ? 1 : 0);
            cmd.ExecuteNonQuery();
            AppendAudit(before == null ? "add_user" : "update_user", "user", login, null,
                before == null ? null : JsonSerializer.Serialize(new { before.Initials, before.Name, before.Role, before.Active }),
                JsonSerializer.Serialize(new { Initials = initials, user.Name, user.Role, user.Active }));
        }

        public List<Patient> GetAllPatients()
        {
            var list = new List<Patient>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT mrn FROM patients ORDER BY mrn";
            var mrns = new List<string>();
            using (var r = cmd.ExecuteReader())
                while (r.Read()) mrns.Add(r.GetString(0));
            foreach (var m in mrns)
                if (GetPatient(m) is Patient p) list.Add(p);
            return list;
        }

        public Patient? GetPatient(string? mrn)
        {
            var key = Services.PatientHistory.NormalizeMrn(mrn);
            if (key == null) return null;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM patients WHERE mrn = $mrn";
            cmd.Parameters.AddWithValue("$mrn", key);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Patient
            {
                Mrn = r.GetString(r.GetOrdinal("mrn")),
                Name = SafeGetString(r, "name"),
                DateOfBirth = SafeGetString(r, "date_of_birth"),
                BloodBankComments = SafeGetString(r, "blood_bank_comments"),
                TransfusionRequirements = SafeGetString(r, "transfusion_requirements"),
                AntigenNegative = SafeGetString(r, "antigen_negative"),
                HpcTransplant = Services.HpcTransplant.Normalize(SafeGetString(r, "hpc_transplant")),
                HpcTransplantDate = SafeGetString(r, "hpc_transplant_date"),
                Sex = Models.PatientSex.Normalize(SafeGetString(r, "sex")),
                UpdatedAt = SafeGetString(r, "updated_at"),
            };
        }

        /// <summary>
        /// Creates or updates the permanent patient record for an MRN.
        /// Audited with before and after values; not locked by a specimen's
        /// final call because it spans the patient's specimens.
        /// </summary>
        public void SavePatient(Patient patient)
        {
            EnsureCanRecord();
            var key = Services.PatientHistory.NormalizeMrn(patient.Mrn)
                      ?? throw new ArgumentException("MRN is required for a patient record.");
            if (key.Length > 64) throw new ArgumentException("MRN cannot be longer than 64 characters.");
            string? dob = null;
            if (!string.IsNullOrWhiteSpace(patient.DateOfBirth))
            {
                if (!DateTime.TryParse(patient.DateOfBirth.Trim(), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var d))
                    throw new ArgumentException($"Date of birth '{patient.DateOfBirth}' is not a valid date.");
                if (d.Date > DateTime.Today) throw new ArgumentException("Date of birth cannot be in the future.");
                dob = d.ToString("yyyy-MM-dd");
            }
            static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
            var antigenNegative = Services.AntigenNegativeRequirement.Parse(patient.AntigenNegative);
            if (antigenNegative.Unrecognized.Count > 0)
                throw new ArgumentException(
                    $"Antigen-negative requirement: {string.Join(", ", antigenNegative.Unrecognized)} is not a recognized antigen. " +
                    "Enter antigen codes such as E, K, Fya; write other requirements under Transfusion requirements.");
            var transplant = Services.HpcTransplant.Normalize(patient.HpcTransplant);
            string? transplantDate = null;
            if (transplant != Services.HpcTransplant.None && !string.IsNullOrWhiteSpace(patient.HpcTransplantDate))
            {
                if (!DateTime.TryParse(patient.HpcTransplantDate.Trim(), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var t))
                    throw new ArgumentException($"Transplant date '{patient.HpcTransplantDate}' is not a valid date.");
                if (t.Date > DateTime.Today) throw new ArgumentException("Transplant date cannot be in the future.");
                transplantDate = t.ToString("yyyy-MM-dd");
            }
            var after = new Patient
            {
                Mrn = key,
                HpcTransplant = transplant,
                HpcTransplantDate = transplantDate,
                Name = Clean(patient.Name),
                DateOfBirth = dob,
                Sex = Models.PatientSex.Normalize(patient.Sex),
                BloodBankComments = Clean(patient.BloodBankComments),
                TransfusionRequirements = Clean(patient.TransfusionRequirements),
                AntigenNegative = antigenNegative.Canonical,
            };
            var before = GetPatient(key);
            if (before != null && before.Name == after.Name && before.DateOfBirth == after.DateOfBirth &&
                before.BloodBankComments == after.BloodBankComments &&
                before.TransfusionRequirements == after.TransfusionRequirements &&
                before.AntigenNegative == after.AntigenNegative &&
                before.HpcTransplant == after.HpcTransplant && before.HpcTransplantDate == after.HpcTransplantDate &&
                before.Sex == after.Sex)
                return;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO patients (mrn, name, date_of_birth, blood_bank_comments, transfusion_requirements, antigen_negative,
                                      hpc_transplant, hpc_transplant_date, sex, updated_at)
                VALUES ($mrn, $name, $dob, $comments, $req, $neg, $hpc, $hpcdate, $sex, $at)
                ON CONFLICT(mrn) DO UPDATE SET name = $name, date_of_birth = $dob,
                    blood_bank_comments = $comments, transfusion_requirements = $req, antigen_negative = $neg,
                    hpc_transplant = $hpc, hpc_transplant_date = $hpcdate, sex = $sex, updated_at = $at";
            cmd.Parameters.AddWithValue("$mrn", key);
            cmd.Parameters.AddWithValue("$name", (object?)after.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dob", (object?)after.DateOfBirth ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$comments", (object?)after.BloodBankComments ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$req", (object?)after.TransfusionRequirements ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$neg", (object?)after.AntigenNegative ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hpc", after.HpcTransplant);
            cmd.Parameters.AddWithValue("$hpcdate", (object?)after.HpcTransplantDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$sex", after.Sex);
            cmd.Parameters.AddWithValue("$at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
            object? Snap(Patient? p) => p == null ? null : new
            {
                name = p.Name, dateOfBirth = p.DateOfBirth, sex = p.Sex,
                bloodBankComments = p.BloodBankComments, transfusionRequirements = p.TransfusionRequirements,
                antigenNegative = p.AntigenNegative,
                hpcTransplant = p.HpcTransplant, hpcTransplantDate = p.HpcTransplantDate,
            };
            AppendAudit(before == null ? "add_patient" : "update_patient", "patient", key, null,
                before == null ? null : JsonSerializer.Serialize(Snap(before)),
                JsonSerializer.Serialize(Snap(after)));
            // The transplant and antigen-negative requirement feed analysis (typing reliability, autoadsorption,
            // transfusion-requirement notes) for every open investigation on this MRN.
            // The free-text requirements feed the antigen-negative review note too.
            if (before == null || before.HpcTransplant != after.HpcTransplant ||
                before.HpcTransplantDate != after.HpcTransplantDate || before.AntigenNegative != after.AntigenNegative ||
                before.TransfusionRequirements != after.TransfusionRequirements)
                foreach (var s in GetSpecimensByMrn(key))
                    MarkAnalysisInputChanged(s.AccessionNumber);
        }

        /// <summary>Eluate specimens linked to <paramref name="sourceAccession"/>, oldest first.</summary>
        /// <summary>Null when the MRNs agree or either is blank; otherwise "belong to different patients (MRN a and b)".</summary>
        private static string? EluateMrnConflict(string? eluateMrn, string? sourceMrn)
        {
            var a = Services.PatientHistory.NormalizeMrn(eluateMrn);
            var b = Services.PatientHistory.NormalizeMrn(sourceMrn);
            return a == null || b == null || a == b ? null : $"belong to different patients (MRN {a} and {b})";
        }

        public List<Specimen> GetEluatesFor(string sourceAccession)
        {
            var list = new List<Specimen>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens WHERE source_accession = $src ORDER BY created_date, accession_number";
            cmd.Parameters.AddWithValue("$src", sourceAccession);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadSpecimen(r));
            return list;
        }

        /// <summary>
        /// Links an eluate specimen to the specimen whose red cells were eluted, or clears
        /// the link with null. Only eluate specimens may be linked; the source must exist,
        /// must not be the eluate itself, and must not be another eluate. Audited.
        /// </summary>
        public void SetEluateSource(string eluateAccession, string? sourceAccession)
        {
            EnsureCanRecord();
            var eluate = GetSpecimen(eluateAccession)
                         ?? throw new ArgumentException($"Specimen {eluateAccession} not found.");
            var src = string.IsNullOrWhiteSpace(sourceAccession) ? null : sourceAccession.Trim();
            if (src != null)
            {
                if (!eluate.IsEluate)
                    throw new ArgumentException("Only an eluate specimen can be linked to a source specimen.");
                if (string.Equals(src, eluateAccession, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("An eluate cannot be its own source.");
                var source = GetSpecimen(src)
                             ?? throw new ArgumentException($"Source specimen {src} not found.");
                if (source.IsEluate)
                    throw new ArgumentException("The source must be the patient's serum or plasma specimen, not another eluate.");
                // The source's analysis reads this eluate's findings, so the two must be the same patient.
                // Checked even when the link is unchanged: a save sets the MRN first, then calls this.
                if (EluateMrnConflict(eluate.Mrn, source.Mrn) is string conflict)
                    throw new ArgumentException(
                        $"Eluate {eluate.AccessionNumber} and source {source.AccessionNumber} {conflict}. " +
                        "Correct the MRN, or link the eluate to this patient's own serum or plasma specimen.");
                src = source.AccessionNumber;
            }
            if (eluate.SourceAccession == src) return;
            EnsureSpecimenUnlocked(eluateAccession);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET source_accession = $src WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$src", (object?)src ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", eluateAccession);
            cmd.ExecuteNonQuery();
            AppendAudit("link_eluate", "specimen", eluateAccession, null,
                JsonSerializer.Serialize(new { source = eluate.SourceAccession }),
                JsonSerializer.Serialize(new { source = src }));
            // The source's analysis lists its eluates' findings, so both the old and the new source change.
            if (!string.IsNullOrWhiteSpace(eluate.SourceAccession)) MarkAnalysisInputChanged(eluate.SourceAccession);
            if (src != null) MarkAnalysisInputChanged(src);
        }

        /// <summary>
        /// Records collection and receipt date-times. Neither may be in the future and
        /// receipt may not precede collection. Blank clears a value. Audited; locked
        /// after a final call.
        /// </summary>
        public const int OriginMaxLength = 128;

        /// <summary>
        /// Records who collected the specimen, where, and who ordered it from where.
        /// Values are trimmed with inner whitespace collapsed; blank clears a value; each
        /// is at most 128 characters. Audited; locked after a final call.
        /// </summary>
        public void UpdateSpecimenOrigin(string accessionNumber, string? collector, string? collectionLocation,
            string? orderingLocation, string? orderingProvider)
        {
            EnsureCanRecord();
            static string? Clean(string? v, string label)
            {
                var t = string.IsNullOrWhiteSpace(v) ? null : System.Text.RegularExpressions.Regex.Replace(v.Trim(), @"\s+", " ");
                if (t != null && t.Length > OriginMaxLength)
                    throw new ArgumentException($"{label} is longer than {OriginMaxLength} characters.");
                return t;
            }
            var c = Clean(collector, "Collector");
            var cl = Clean(collectionLocation, "Collection location");
            var ol = Clean(orderingLocation, "Ordering location");
            var op = Clean(orderingProvider, "Ordering provider");
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.Collector == c && before.CollectionLocation == cl &&
                before.OrderingLocation == ol && before.OrderingProvider == op) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"UPDATE specimens SET collector = $c, collection_location = $cl,
                ordering_location = $ol, ordering_provider = $op WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$c", (object?)c ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cl", (object?)cl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ol", (object?)ol ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$op", (object?)op ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_origin", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { collector = before.Collector, collectionLocation = before.CollectionLocation,
                    orderingLocation = before.OrderingLocation, orderingProvider = before.OrderingProvider }),
                JsonSerializer.Serialize(new { collector = c, collectionLocation = cl, orderingLocation = ol, orderingProvider = op }));
        }

        public const int EncounterMaxLength = 64;

        /// <summary>
        /// Records the encounter or account number the specimen was drawn under. Trimmed,
        /// inner whitespace removed; blank clears it; at most 64 characters. Audited as
        /// update_encounter; locked after a final call.
        /// </summary>
        public void UpdateSpecimenEncounter(string accessionNumber, string? encounter)
        {
            EnsureCanRecord();
            var value = NormalizeEncounter(encounter);
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.EncounterNumber == value) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET encounter_number = $e WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$e", (object?)value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_encounter", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { encounter = before.EncounterNumber }),
                JsonSerializer.Serialize(new { encounter = value }));
        }

        public static string? NormalizeEncounter(string? encounter)
        {
            if (string.IsNullOrWhiteSpace(encounter)) return null;
            var t = System.Text.RegularExpressions.Regex.Replace(encounter, @"\s+", "");
            if (t.Length > EncounterMaxLength)
                throw new ArgumentException($"Encounter / account number is longer than {EncounterMaxLength} characters.");
            return t;
        }

        /// <summary>
        /// Other specimens recorded under the same encounter number (ignoring case),
        /// oldest first. Used to catch an encounter shared across different MRNs.
        /// </summary>
        public List<Specimen> GetSpecimensByEncounter(string encounter)
        {
            var list = new List<Specimen>();
            var value = NormalizeEncounter(encounter);
            if (value == null) return list;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens WHERE encounter_number = $e COLLATE NOCASE ORDER BY created_date, accession_number";
            cmd.Parameters.AddWithValue("$e", value);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadSpecimen(r));
            return list;
        }

        public void UpdateSpecimenCollection(string accessionNumber, string? collectedAt, string? receivedAt)
        {
            EnsureCanRecord();
            var now = DateTime.Now;
            var collected = Services.SpecimenDating.Normalize(collectedAt, "Collection time", now);
            var received = Services.SpecimenDating.Normalize(receivedAt, "Received time", now);
            if (collected != null && received != null && string.CompareOrdinal(received, collected) < 0)
                throw new ArgumentException("Received time cannot be before collection time.");
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.CollectedAt == collected && before.ReceivedAt == received) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET collected_at = $c, received_at = $r WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$c", (object?)collected ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$r", (object?)received ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_collection", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { collectedAt = before.CollectedAt, receivedAt = before.ReceivedAt }),
                JsonSerializer.Serialize(new { collectedAt = collected, receivedAt = received }));
            // Collection is the reference date for specimen dating and the transfusion-history window, here and on
            // the patient's other specimens (their historical-phenotype and transfusion-history checks read it).
            if (before.CollectedAt != collected)
            {
                MarkAnalysisInputChanged(accessionNumber);
                MarkSamePatientSpecimensChanged(accessionNumber);
            }
        }

        /// <summary>
        /// Records pregnancy context and Rh immune globulin. A recent pregnancy requires an
        /// end date; other statuses drop it. Dates may not be in the future. Audited; locked
        /// after a final call.
        /// </summary>
        public void UpdateSpecimenPregnancy(string accessionNumber, string? status, string? endDate, string? rhigDate)
        {
            EnsureCanRecord();
            static string? Day(string? text, string label)
            {
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (!DateTime.TryParse(text.Trim(), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var d))
                    throw new ArgumentException($"{label} '{text}' is not a valid date.");
                if (d.Date > DateTime.Today) throw new ArgumentException($"{label} cannot be in the future.");
                return d.ToString("yyyy-MM-dd");
            }
            var code = PregnancyStatus.Normalize(status);
            var end = code == PregnancyStatus.Recent ? Day(endDate, "Pregnancy end date") : null;
            if (code == PregnancyStatus.Recent && end == null)
                throw new ArgumentException("Enter the date the recent pregnancy ended.");
            var rhig = Day(rhigDate, "RhIG date");

            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.PregnancyStatus == code && before.PregnancyEndDate == end && before.RhigDate == rhig) return;
            EnsureSpecimenUnlocked(accessionNumber);

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"UPDATE specimens SET pregnancy_status = $s, pregnancy_end_date = $e, rhig_date = $r
                WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$s", code);
            cmd.Parameters.AddWithValue("$e", (object?)end ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$r", (object?)rhig ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_pregnancy", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { status = before.PregnancyStatus, endDate = before.PregnancyEndDate, rhigDate = before.RhigDate }),
                JsonSerializer.Serialize(new { status = code, endDate = end, rhigDate = rhig }));
            MarkAnalysisInputChanged(accessionNumber);
        }

        public List<Specimen> GetSpecimensByMrn(string mrn)
        {
            var list = new List<Specimen>();
            var key = Services.PatientHistory.NormalizeMrn(mrn);
            if (key == null) return list;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM specimens WHERE mrn = $mrn ORDER BY created_date DESC";
            cmd.Parameters.AddWithValue("$mrn", key);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadSpecimen(r));
            return list;
        }

        /// <summary>
        /// Sets the MRN that links this specimen to the patient's other
        /// specimens. Audited because it changes which history analysis uses.
        /// </summary>
        public void UpdateSpecimenMrn(string accessionNumber, string? mrn)
        {
            EnsureCanRecord();
            var key = Services.PatientHistory.NormalizeMrn(mrn);
            if (key != null && key.Length > 64)
                throw new ArgumentException("MRN cannot be longer than 64 characters.");
            var before = GetSpecimen(accessionNumber)
                         ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (before.Mrn == key) return;
            EnsureSpecimenUnlocked(accessionNumber);
            // A source must stay the same patient as the eluates linked to it (their findings feed its analysis).
            // An eluate's own MRN is checked against its source by SetEluateSource, which the save calls next.
            foreach (var linked in GetEluatesFor(before.AccessionNumber))
                if (EluateMrnConflict(linked.Mrn, key) is string conflict)
                    throw new ArgumentException(
                        $"Eluate {linked.AccessionNumber} is linked to {before.AccessionNumber}, and with this change they {conflict.Replace("belong", "would belong")}. " +
                        "Unlink or correct the eluate first.");

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET mrn = $mrn WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$mrn", (object?)key ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("update_mrn", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { mrn = before.Mrn }),
                JsonSerializer.Serialize(new { mrn = key }));
            // A different MRN brings a different patient record (transplant, antigen-negative requirement) and
            // history (prior confirmed antibodies), all of which the analysis reads.
            MarkAnalysisInputChanged(accessionNumber);
        }

        public void SetSpecimenActive(string accessionNumber, bool active)
        {
            EnsureCanRecord();
            var before = GetSpecimen(accessionNumber);
            if (before == null || before.IsActive == active) return;
            AppendAudit("set_specimen_active", "specimen", accessionNumber, null,
                JsonSerializer.Serialize(new { isActive = before.IsActive }),
                JsonSerializer.Serialize(new { isActive = active }));
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET is_active = $active WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Confirms the identification. A confirmed identification is never replaced in place: it is cleared
        /// first with <see cref="ClearSpecimenFinalCall"/>, which demands the permission, asks for a reason and
        /// records the identification as it stood, so every change appears in the amendment history.
        /// </summary>
        public void SetSpecimenFinalCall(string accessionNumber,
            string? antibodies, string? comment, string? identifiedBy,
            DateTime? identifiedAt = null, IReadOnlyList<string>? overrides = null)
        {
            if (antibodies == null)
                throw new ArgumentException(
                    "Clearing a confirmed identification needs a reason; use ClearSpecimenFinalCall.", nameof(antibodies));
            EnsureCanRecord();
            var current = GetSpecimen(accessionNumber);
            if (current?.IsCancelled == true)
                throw LockedError(accessionNumber, cancelled: true);
            if (current?.HasFinalCall == true)
                throw new RecordLockedException(accessionNumber,
                    $"Specimen {accessionNumber} already has a confirmed identification ({current.FinalAntibodies}). " +
                    AccessControl.NextStep(this, Permission.ClearIdentification,
                        "To change it, click Clear on the Analysis tab and give a reason, then confirm the new identification.",
                        "clear it with a reason; the new identification can then be confirmed.") +
                    " The earlier identification stays in the amendment history.");
            WriteFinalCall(accessionNumber, antibodies, comment, identifiedBy, identifiedAt, overrides);
        }

        private void WriteFinalCall(string accessionNumber,
            string? antibodies, string? comment, string? identifiedBy,
            DateTime? identifiedAt, IReadOnlyList<string>? overrides)
        {
            WriteFinalCallCore(accessionNumber, antibodies, comment, identifiedBy, identifiedAt, overrides);
            MarkEluateSourceChanged(accessionNumber);
            MarkSamePatientSpecimensChanged(accessionNumber);
        }

        /// <summary>
        /// A confirmed identification is patient history for the patient's other specimens (prior antibodies,
        /// separation exemptions), so confirming or clearing one marks their analyses out of date.
        /// </summary>
        private void MarkSamePatientSpecimensChanged(string accession)
        {
            var mrn = GetSpecimen(accession)?.Mrn;
            if (string.IsNullOrWhiteSpace(mrn)) return;
            foreach (var other in GetSpecimensByMrn(mrn))
                if (!string.Equals(other.AccessionNumber, accession, StringComparison.Ordinal))
                    MarkAnalysisInputChanged(other.AccessionNumber);
        }

        /// <summary>
        /// When <paramref name="accession"/> is an eluate linked to a source, the source's analysis (which lists
        /// the eluate's suspected or confirmed specificities) is now out of date.
        /// </summary>
        private void MarkEluateSourceChanged(string accession)
        {
            if (GetSpecimen(accession) is { IsEluate: true } e && !string.IsNullOrWhiteSpace(e.SourceAccession))
                MarkAnalysisInputChanged(e.SourceAccession);
        }

        private void WriteFinalCallCore(string accessionNumber,
            string? antibodies, string? comment, string? identifiedBy,
            DateTime? identifiedAt, IReadOnlyList<string>? overrides)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE specimens SET
                    final_antibodies = $ab,
                    final_comment = $comment,
                    identified_by = $by,
                    identified_at = $at,
                    reviewed_by = NULL,
                    reviewed_at = NULL
                WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$ab", (object?)antibodies ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$comment", (object?)comment ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$by", (object?)identifiedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$at", antibodies == null
                ? DBNull.Value
                : (identifiedAt ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            if (antibodies != null)
            {
                // Warnings confirmed past are part of the confirmation record (one entry, written together).
                var overridden = overrides?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
                AppendAudit("confirm_final_call", "specimen", accessionNumber,
                    overridden is { Count: > 0 } ? $"Confirmed past {overridden.Count} warning(s)" : null,
                    afterJson: overridden is { Count: > 0 }
                        ? JsonSerializer.Serialize(new { antibodies, comment, identifiedBy, overrides = overridden })
                        : JsonSerializer.Serialize(new { antibodies, comment, identifiedBy }));
            }
        }

        /// <summary>
        /// Records a supervisor review of the confirmed identification. The
        /// reviewer must differ from the person who confirmed it. Allowed on a
        /// locked specimen because it signs off the locked record without
        /// changing it. Audited.
        /// </summary>
        public void SetSpecimenSupervisorReview(string accessionNumber, string reviewer, string? comment = null)
        {
            Services.AccessControl.Demand(this, Services.Permission.SupervisorReview);
            var initials = LabSettings.NormalizeInitials(reviewer);
            if (string.IsNullOrEmpty(initials))
                throw new ArgumentException("Reviewer initials are required.", nameof(reviewer));
            if (Services.AccessControl.CurrentUser(this) is AppUser me &&
                !string.Equals(me.Initials, initials, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Enter your own initials ({me.Initials}); the review is recorded under the signed-in user.");
            var s = GetSpecimen(accessionNumber)
                    ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (!s.HasFinalCall)
                throw new InvalidOperationException("There is no confirmed identification to review.");
            if (s.IsReviewed)
                throw new InvalidOperationException($"Already reviewed by {s.ReviewedBy} on {s.ReviewedAt}.");
            if (string.Equals(initials, LabSettings.NormalizeInitials(s.IdentifiedBy), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The supervisor review must be recorded by someone other than the person who confirmed the identification.");

            var at = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET reviewed_by = $by, reviewed_at = $at WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$by", initials);
            cmd.Parameters.AddWithValue("$at", at);
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("supervisor_review", "specimen", accessionNumber,
                string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                afterJson: JsonSerializer.Serialize(new
                {
                    reviewedBy = initials,
                    reviewedAt = at,
                    finalAntibodies = s.FinalAntibodies,
                    identifiedBy = s.IdentifiedBy,
                }));
        }

        /// <summary>
        /// Cancels an investigation (e.g. mislabeled or unacceptable specimen, cancelled order).
        /// Requires a reason and no confirmed identification. The record is kept, locked, and
        /// removed from work queues. Audited.
        /// </summary>
        public void CancelInvestigation(string accessionNumber, string reason)
        {
            EnsureCanRecord();
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required to cancel an investigation.", nameof(reason));
            var s = GetSpecimen(accessionNumber)
                    ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (s.IsCancelled)
                throw new InvalidOperationException($"{accessionNumber} is already cancelled.");
            if (s.HasFinalCall)
                throw new InvalidOperationException(
                    "Clear the confirmed identification with a reason before cancelling the investigation.");
            var at = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET cancelled_at = $at, cancel_reason = $r WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$at", at);
            cmd.Parameters.AddWithValue("$r", reason.Trim());
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("cancel_investigation", "specimen", accessionNumber, reason.Trim(),
                afterJson: JsonSerializer.Serialize(new { cancelledAt = at }));
        }

        /// <summary>Reinstates a cancelled investigation. Requires a reason. Audited.</summary>
        public void ReinstateInvestigation(string accessionNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required to reinstate an investigation.", nameof(reason));
            Services.AccessControl.Demand(this, Services.Permission.ReinstateInvestigation);
            var s = GetSpecimen(accessionNumber)
                    ?? throw new ArgumentException($"Specimen {accessionNumber} not found.");
            if (!s.IsCancelled)
                throw new InvalidOperationException($"{accessionNumber} is not cancelled.");
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE specimens SET cancelled_at = NULL, cancel_reason = NULL WHERE accession_number = $acc";
            cmd.Parameters.AddWithValue("$acc", accessionNumber);
            cmd.ExecuteNonQuery();
            AppendAudit("reinstate_investigation", "specimen", accessionNumber, reason.Trim(),
                beforeJson: JsonSerializer.Serialize(new { s.CancelledAt, s.CancelReason }));
        }

        public void ClearSpecimenFinalCall(string accessionNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required to clear a confirmed identification.", nameof(reason));
            Services.AccessControl.Demand(this, Services.Permission.ClearIdentification);

            EnsureCanRecord();
            var before = GetSpecimen(accessionNumber);
            WriteFinalCall(accessionNumber, null, null, null, null, null);
            AppendAudit("clear_final_call", "specimen", accessionNumber, reason.Trim(),
                beforeJson: before == null ? null : JsonSerializer.Serialize(new
                {
                    before.FinalAntibodies,
                    before.FinalComment,
                    before.IdentifiedBy,
                    before.IdentifiedAt
                }));
        }

        /// <summary>
        /// Deletes a specimen and, by cascade, its panel links, runs, reactions and
        /// analysis. Refused when the specimen has a confirmed identification (cancel the
        /// investigation or clear the identification first) or when eluates name it as
        /// their source. Audited with what was removed. Missing accessions are ignored.
        /// </summary>
        public void DeleteSpecimen(string accessionNumber)
        {
            EnsureCanRecord();
            var s = GetSpecimen(accessionNumber);
            if (s == null) return;
            if (s.HasFinalCall)
                throw new InvalidOperationException(
                    $"Specimen {accessionNumber} has a confirmed identification ({s.FinalAntibodies}) and cannot be deleted. " +
                    "Cancel the investigation instead, or have a supervisor clear the identification first.");
            var eluates = GetEluatesFor(accessionNumber);
            if (eluates.Count > 0)
                throw new InvalidOperationException(
                    $"Specimen {accessionNumber} is the source of eluate(s) {string.Join(", ", eluates.Select(e => e.AccessionNumber))}. " +
                    "Remove those links before deleting it.");

            int reactionRows;
            using (var count = _conn.CreateCommand())
            {
                count.CommandText = @"
                    SELECT COUNT(*) FROM reactions r JOIN panel_runs pr ON pr.run_id = r.run_id
                    WHERE pr.specimen_id = $acc";
                count.Parameters.AddWithValue("$acc", accessionNumber);
                reactionRows = Convert.ToInt32(count.ExecuteScalar());
            }
            var panels = GetSpecimenPanels(accessionNumber).Select(p => p.PanelId).ToList();

            RunInTransaction(() =>
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "DELETE FROM specimens WHERE accession_number = $acc";
                cmd.Parameters.AddWithValue("$acc", accessionNumber);
                cmd.ExecuteNonQuery();
                AppendAudit("delete_specimen", "specimen", accessionNumber, null,
                    JsonSerializer.Serialize(new
                    {
                        mrn = s.Mrn, type = s.Type, createdDate = s.CreatedDate,
                        linkedPanels = panels, reactionRows, cancelled = s.IsCancelled,
                    }),
                    null);
            });
        }

        /// <summary>
        /// Staleness compares reactions_updated_at with last_analyzed_at (local time, to the second). The two
        /// writers below keep the order of events even when the clock does not: a reaction change is always
        /// stamped after the last analysis (an edit in the same second, or after the clock went back at the end
        /// of daylight saving time or by a correction, would otherwise look older and the analysis would look
        /// current), and an analysis is never stamped before the reactions it read.
        /// </summary>
        /// <summary>
        /// An input to the analysis other than a grade changed (transfusion, DAT, pregnancy, patient typing or
        /// history, notes, the patient's transplant or antigen-negative requirement, or panel typing). An analysed,
        /// open investigation is marked out of date, as a grade change would. Never-analysed, confirmed and
        /// cancelled investigations are left alone.
        /// </summary>
        private void MarkAnalysisInputChanged(string accession)
        {
            var s = GetSpecimen(accession);
            if (s == null || s.LastAnalyzedAt == null || s.HasFinalCall || s.IsCancelled) return;
            TouchSpecimenReactionsUpdated(accession);
        }

        public void TouchSpecimenReactionsUpdated(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE specimens SET reactions_updated_at =
                    CASE WHEN last_analyzed_at IS NOT NULL AND last_analyzed_at >= $now
                              AND strftime('%Y-%m-%dT%H:%M:%S', last_analyzed_at, '+1 second') IS NOT NULL
                         THEN strftime('%Y-%m-%dT%H:%M:%S', last_analyzed_at, '+1 second')
                         ELSE $now END
                WHERE accession_number = $id";
            cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString("o")[..19]);
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.ExecuteNonQuery();
        }

        public void SetSpecimenLastAnalyzed(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE specimens SET last_analyzed_at =
                    CASE WHEN reactions_updated_at IS NOT NULL AND reactions_updated_at > $now
                         THEN reactions_updated_at ELSE $now END,
                    analysis_config_seq = (SELECT COALESCE(MAX(id), 0) FROM audit_events WHERE action IN " + ConfigActionsSql + @")
                WHERE accession_number = $id";
            cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString("o")[..19]);
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.ExecuteNonQuery();
            MarkEluateSourceChanged(specimenId);
        }

        /// <summary>
        /// Why the specimen's last analysis is out of date, or null. See <see cref="Services.AnalysisCurrency"/>:
        /// grades saved after it, rules or Preferences changed after it (audit log), or this workstation's
        /// clinical Preferences differing from those saved with the analysis.
        /// </summary>
        public string? AnalysisStaleText(string specimenId) =>
            GetSpecimen(specimenId) is Specimen s
                ? Services.AnalysisCurrency.StaleReason(s, LastAnalysisConfigurationChange(), SettingsDrifted(s))
                : null;

        /// <summary>
        /// True when an unconfirmed analysis was saved with clinical Preferences that differ from this
        /// workstation's current ones (for example another workstation's settings, or an unaudited change).
        /// </summary>
        public bool SettingsDrifted(Specimen s) =>
            !s.HasFinalCall && s.LastAnalyzedAt != null &&
            LabSettings.ClinicalSettingsDiffer(GetLatestAnalysisSnapshot(s.AccessionNumber)?.SettingsJson,
                AppSettings.Current.ClinicalSettingsJson());

        public bool IsSpecimenAnalysisStale(string specimenId) =>
            GetAnalysisStaleReason(specimenId) != AnalysisStaleReason.None;

        public AnalysisStaleReason GetAnalysisStaleReason(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT reactions_updated_at, last_analyzed_at, final_antibodies FROM specimens
                WHERE accession_number = $id";
            cmd.Parameters.AddWithValue("$id", specimenId);
            string? ru;
            string? la;
            string? finalId;
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return AnalysisStaleReason.None;
                ru = r.IsDBNull(0) ? null : r.GetString(0);
                la = r.IsDBNull(1) ? null : r.GetString(1);
                finalId = r.IsDBNull(2) ? null : r.GetString(2);
            }
            var reactionsChanged = ReactionsChangedSinceAnalysis(ru, la);
            var preferencesChanged = false;
            if (string.IsNullOrWhiteSpace(finalId) && la != null && GetSpecimen(specimenId) is Specimen spec)
            {
                var saved = GetLatestAnalysisSnapshot(specimenId)?.SettingsJson;
                preferencesChanged = LabSettings.ClinicalSettingsDiffer(saved, AppSettings.Current.ClinicalSettingsJson()) ||
                    Services.AnalysisCurrency.ConfigChangedAfter(spec, LastAnalysisConfigurationChange());
            }
            return AnalysisFreshness.Combine(reactionsChanged, preferencesChanged);
        }

        private static bool ReactionsChangedSinceAnalysis(string? reactionsUpdatedAt, string? lastAnalyzedAt)
        {
            if (reactionsUpdatedAt == null && lastAnalyzedAt == null) return false;
            if (reactionsUpdatedAt != null && lastAnalyzedAt == null) return true;
            if (reactionsUpdatedAt == null) return false;
            return string.Compare(reactionsUpdatedAt, lastAnalyzedAt, StringComparison.Ordinal) > 0;
        }

        public Dictionary<string, string> GetLatestSnapshotSettingsBySpecimen()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT specimen_id, settings_json
                FROM analysis_snapshots
                WHERE id IN (SELECT MAX(id) FROM analysis_snapshots GROUP BY specimen_id)";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r.IsDBNull(1)) continue;
                map[r.GetString(0)] = r.GetString(1);
            }
            return map;
        }

        // ── Specimen Antibodies ───────────────────────────────────────────────

        public void AddSpecimenAntibody(string specimenId, string antibody, double probability)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM specimen_antibodies WHERE specimen_id = $id AND antibody = $ab";
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.Parameters.AddWithValue("$ab", antibody);
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                INSERT INTO specimen_antibodies (specimen_id, antibody, probability)
                VALUES ($id, $ab, $prob)";
            cmd.Parameters.AddWithValue("$prob", probability);
            cmd.ExecuteNonQuery();
        }

        public List<SpecimenAntibody> GetSpecimenAntibodies(string specimenId)
        {
            var list = new List<SpecimenAntibody>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, specimen_id, antibody, probability FROM specimen_antibodies
                WHERE specimen_id = $id ORDER BY probability DESC";
            cmd.Parameters.AddWithValue("$id", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SpecimenAntibody
                {
                    Id = r.GetInt32(0),
                    SpecimenId = r.GetString(1),
                    Antibody = r.GetString(2),
                    Probability = r.GetDouble(3)
                });
            return list;
        }

        public void ClearSpecimenAntibodies(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM specimen_antibodies WHERE specimen_id = $id";
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.ExecuteNonQuery();
        }

        // ── Specimen Rule-outs ────────────────────────────────────────────────

        public void AddSpecimenRuleout(string specimenId, string antibody, int count)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id FROM specimen_ruleouts WHERE specimen_id = $id AND antibody = $ab";
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.Parameters.AddWithValue("$ab", antibody);
            var existingId = cmd.ExecuteScalar();
            if (existingId != null)
            {
                cmd.CommandText = "UPDATE specimen_ruleouts SET ruleout_count = $cnt WHERE id = $eid";
                cmd.Parameters.AddWithValue("$cnt", count);
                cmd.Parameters.AddWithValue("$eid", existingId);
            }
            else
            {
                cmd.CommandText = @"
                    INSERT INTO specimen_ruleouts (specimen_id, antibody, ruleout_count)
                    VALUES ($id, $ab, $cnt)";
                cmd.Parameters.AddWithValue("$cnt", count);
            }
            cmd.ExecuteNonQuery();
        }

        public List<SpecimenRuleout> GetSpecimenRuleouts(string specimenId)
        {
            var list = new List<SpecimenRuleout>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, specimen_id, antibody, ruleout_count FROM specimen_ruleouts
                WHERE specimen_id = $id ORDER BY antibody";
            cmd.Parameters.AddWithValue("$id", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SpecimenRuleout
                {
                    Id = r.GetInt32(0),
                    SpecimenId = r.GetString(1),
                    Antibody = r.GetString(2),
                    RuleoutCount = r.GetInt32(3)
                });
            return list;
        }

        public void ClearSpecimenRuleouts(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM specimen_ruleouts WHERE specimen_id = $id";
            cmd.Parameters.AddWithValue("$id", specimenId);
            cmd.ExecuteNonQuery();
        }

        // ── Panels ────────────────────────────────────────────────────────────

        public int AddPanel(string name, string? lotNumber, string? vendor,
            int numCells, string? expirationDate, bool includeAc, int startCell = 1, bool? isActive = null,
            string? catalogNumber = null, string? productLine = null, bool enzymeTreated = false,
            string? sourceUrl = null, string? sourceFormat = null, string? importedAt = null,
            string? specialNotes = null, string? sourceSha256 = null, string? sourceArtifactPath = null,
            bool startTypedNegative = false)
        {
            EnsureCanRecord();
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            bool active = isActive ?? (expirationDate == null || string.Compare(expirationDate, today) >= 0);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO panels (name, lot_number, vendor, num_cells, expiration_date, include_ac, start_cell, is_active,
                    catalog_number, product_line, enzyme_treated, source_url, source_format, imported_at, special_notes,
                    source_sha256, source_artifact_path)
                VALUES ($name, $lot, $vendor, $num, $exp, $ac, $sc, $active,
                    $cat, $pline, $enz, $url, $fmt, $imp, $notes, $sha, $art);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$lot", (object?)lotNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$vendor", (object?)vendor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", numCells);
            cmd.Parameters.AddWithValue("$exp", (object?)expirationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ac", includeAc ? 1 : 0);
            cmd.Parameters.AddWithValue("$sc", startCell);
            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$cat", (object?)catalogNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pline", (object?)productLine ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$enz", enzymeTreated ? 1 : 0);
            cmd.Parameters.AddWithValue("$url", (object?)sourceUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$fmt", (object?)sourceFormat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$imp", (object?)importedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$notes", (object?)specialNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$sha", (object?)sourceSha256 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$art", (object?)sourceArtifactPath ?? DBNull.Value);
            var panelId = Convert.ToInt32(cmd.ExecuteScalar());

            // Cells start NT. Only synthetic demo data asks for negative, then types its positives.
            for (int i = startCell; i < startCell + numCells; i++)
                AddPanelCell(panelId, i.ToString(), startTypedNegative);
            if (includeAc)
                AddPanelCell(panelId, "AC", startTypedNegative);

            AppendAudit("add_panel", "panel", panelId.ToString(), null,
                afterJson: JsonSerializer.Serialize(new { name, lotNumber, vendor }));
            return panelId;
        }

        public Panel? GetPanel(int panelId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM panels WHERE panel_id = $id";
            cmd.Parameters.AddWithValue("$id", panelId);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadPanel(r) : null;
        }

        public List<Panel> GetAllPanels()
        {
            var list = new List<Panel>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM panels ORDER BY name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanel(r));
            return list;
        }

        public List<Panel> GetActivePanels()
        {
            var list = new List<Panel>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM panels WHERE is_active = 1 ORDER BY name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanel(r));
            return list;
        }

        public void UpdatePanel(int panelId, string name, string? lotNumber, string? vendor,
            int numCells, string? expirationDate, bool includeAc, int startCell = 1, bool isActive = true,
            string? catalogNumber = null, string? productLine = null, bool enzymeTreated = false,
            string? specialNotes = null)
        {
            EnsureCanRecord();
            var before = GetPanel(panelId)
                ?? throw new InvalidOperationException($"Panel #{panelId} was not found.");
            // Keep the cells that stay in range, with their typing; add only new cells and
            // remove only cells that fall out of range. Removing a cell with recorded
            // reactions is refused so test history keeps its antigram.
            var wanted = Enumerable.Range(startCell, numCells).Select(i => i.ToString()).ToList();
            if (includeAc) wanted.Add("AC");
            var existing = GetPanelCells(panelId);
            var removed = existing
                .Where(c => !wanted.Contains(c.CellNumber, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.CellNumber)
                .ToList();
            var added = wanted
                .Where(w => !existing.Any(c => string.Equals(c.CellNumber, w, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var tested = CellsWithReactions(panelId);
            var blocked = removed.Where(tested.Contains).ToList();
            if (blocked.Count > 0)
                throw new InvalidOperationException(
                    $"Cell(s) {string.Join(", ", blocked)} have recorded reactions and cannot be removed from this panel. " +
                    "Keep the cell range, or copy the panel to a new lot.");

            RunInTransaction(() => UpdatePanelCore(before, panelId, name, lotNumber, vendor, numCells, expirationDate,
                includeAc, startCell, isActive, catalogNumber, productLine, enzymeTreated, specialNotes, removed, added));
        }

        private HashSet<string> CellsWithReactions(int panelId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT r.cell_number FROM reactions r JOIN panel_runs pr ON pr.run_id = r.run_id
                WHERE pr.panel_id = $id
                  AND (COALESCE(r.""IS"", 'NT') NOT IN ('NT', '') OR COALESCE(r.C37, 'NT') NOT IN ('NT', '')
                       OR COALESCE(r.AHG, 'NT') NOT IN ('NT', '') OR COALESCE(r.CC, 'NT') NOT IN ('NT', ''))";
            cmd.Parameters.AddWithValue("$id", panelId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) set.Add(r.GetString(0));
            return set;
        }

        private void UpdatePanelCore(Panel before, int panelId, string name, string? lotNumber, string? vendor,
            int numCells, string? expirationDate, bool includeAc, int startCell, bool isActive,
            string? catalogNumber, string? productLine, bool enzymeTreated, string? specialNotes,
            List<string> removed, List<string> added)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE panels SET name=$name, lot_number=$lot, vendor=$vendor,
                num_cells=$num, expiration_date=$exp, include_ac=$ac, start_cell=$sc, is_active=$active,
                catalog_number=$cat, product_line=$pline, enzyme_treated=$enz, special_notes=$notes
                WHERE panel_id=$id";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$lot", (object?)lotNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$vendor", (object?)vendor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$num", numCells);
            cmd.Parameters.AddWithValue("$exp", (object?)expirationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ac", includeAc ? 1 : 0);
            cmd.Parameters.AddWithValue("$sc", startCell);
            cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$cat", (object?)catalogNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pline", (object?)productLine ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$enz", enzymeTreated ? 1 : 0);
            cmd.Parameters.AddWithValue("$notes", (object?)specialNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.ExecuteNonQuery();

            foreach (var cellNumber in removed)
            {
                using var del = _conn.CreateCommand();
                del.CommandText = "DELETE FROM panel_cells WHERE panel_id = $id AND cell_number = $cn";
                del.Parameters.AddWithValue("$id", panelId);
                del.Parameters.AddWithValue("$cn", cellNumber);
                del.ExecuteNonQuery();
            }
            foreach (var cellNumber in added)
                AddPanelCell(panelId, cellNumber);
            // Record each changed field with its old and new value, so a lot number, expiry or treatment edit
            // on a lot that holds test history can always be traced back.
            var (beforeFields, afterFields) = PanelEditDiff(before, name, lotNumber, vendor, numCells, expirationDate,
                includeAc, startCell, isActive, catalogNumber, productLine, enzymeTreated, specialNotes);
            afterFields["cellsAdded"] = added;
            afterFields["cellsRemoved"] = removed;
            AppendAudit("update_panel", "panel", panelId.ToString(), null,
                JsonSerializer.Serialize(beforeFields), JsonSerializer.Serialize(afterFields));
        }

        private static (Dictionary<string, object?> Before, Dictionary<string, object?> After) PanelEditDiff(Panel before,
            string name, string? lotNumber, string? vendor, int numCells, string? expirationDate, bool includeAc,
            int startCell, bool isActive, string? catalogNumber, string? productLine, bool enzymeTreated, string? specialNotes)
        {
            var b = new Dictionary<string, object?>();
            var a = new Dictionary<string, object?>();
            void Field(string key, object? old, object? now)
            {
                if (Equals(old, now) || (old is null or "" && now is null or "")) return;
                b[key] = old;
                a[key] = now;
            }
            Field("name", before.Name, name);
            Field("lotNumber", before.LotNumber, lotNumber);
            Field("vendor", before.Vendor, vendor);
            Field("numCells", before.NumCells, numCells);
            Field("startCell", before.StartCell, startCell);
            Field("includeAc", before.IncludeAc, includeAc);
            Field("expirationDate", before.ExpirationDate, expirationDate);
            Field("active", before.IsActive, isActive);
            Field("catalogNumber", before.CatalogNumber, catalogNumber);
            Field("productLine", before.ProductLine, productLine);
            Field("enzymeTreated", before.EnzymeTreated, enzymeTreated);
            Field("specialNotes", before.SpecialNotes, specialNotes);
            return (b, a);
        }

        public Panel? FindPanelByVendorLot(string? vendor, string? lotNumber)
        {
            if (string.IsNullOrWhiteSpace(lotNumber)) return null;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM panels
                WHERE lot_number = $lot
                  AND ($vendor IS NULL OR vendor = $vendor)
                ORDER BY panel_id
                LIMIT 1";
            cmd.Parameters.AddWithValue("$lot", lotNumber.Trim());
            cmd.Parameters.AddWithValue("$vendor",
                string.IsNullOrWhiteSpace(vendor) ? DBNull.Value : vendor.Trim());
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadPanel(r) : null;
        }

        public void SetPanelVendorMetadata(int panelId, string? catalogNumber, string? productLine,
            bool enzymeTreated, string? sourceUrl, string? sourceFormat, string? importedAt,
            string? specialNotes, string? sourceSha256 = null, string? sourceArtifactPath = null)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE panels SET catalog_number=$cat, product_line=$pline, enzyme_treated=$enz,
                    source_url=$url, source_format=$fmt, imported_at=$imp, special_notes=$notes,
                    source_sha256=$sha, source_artifact_path=$art
                WHERE panel_id=$id";
            cmd.Parameters.AddWithValue("$cat", (object?)catalogNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$pline", (object?)productLine ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$enz", enzymeTreated ? 1 : 0);
            cmd.Parameters.AddWithValue("$url", (object?)sourceUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$fmt", (object?)sourceFormat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$imp", (object?)importedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$notes", (object?)specialNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$sha", (object?)sourceSha256 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$art", (object?)sourceArtifactPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.ExecuteNonQuery();
        }

        public Panel? FindPriorVendorPanel(string? vendor, string? productLine, string? excludeLot)
        {
            if (string.IsNullOrWhiteSpace(vendor)) return null;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM panels
                WHERE vendor = $vendor
                  AND ($pline IS NULL OR product_line IS NULL OR product_line = $pline)
                  AND ($lot IS NULL OR lot_number IS NULL OR lot_number != $lot)
                ORDER BY
                    CASE WHEN $pline IS NOT NULL AND product_line = $pline THEN 0 ELSE 1 END,
                    imported_at DESC,
                    panel_id DESC
                LIMIT 1";
            cmd.Parameters.AddWithValue("$vendor", vendor.Trim());
            cmd.Parameters.AddWithValue("$pline",
                string.IsNullOrWhiteSpace(productLine) ? DBNull.Value : productLine.Trim());
            cmd.Parameters.AddWithValue("$lot",
                string.IsNullOrWhiteSpace(excludeLot) ? DBNull.Value : excludeLot.Trim());
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadPanel(r) : null;
        }

        /// <summary>
        /// Activates or deactivates a panel. <paramref name="acknowledgedCoverage"/> records, on activation,
        /// the antigen-coverage gaps against the previous lot that the user reviewed and accepted.
        /// </summary>
        public void SetPanelActive(int panelId, bool active, Services.ActivationCoverage? acknowledgedCoverage = null)
        {
            EnsureCanRecord();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE panels SET is_active = $active WHERE panel_id = $id";
            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.ExecuteNonQuery();
            var gaps = active ? acknowledgedCoverage : null;
            var expiredOn = active && GetPanel(panelId) is { IsExpired: true } p ? p.ExpirationDate : null;
            var reasons = new List<string>();
            if (expiredOn != null) reasons.Add($"Activated after expiry ({expiredOn})");
            if (gaps != null) reasons.Add($"Activated with coverage gaps against {gaps.PriorLabel}");
            AppendAudit(active ? "activate_panel" : "deactivate_panel", "panel", panelId.ToString(),
                reasons.Count == 0 ? null : string.Join("; ", reasons),
                afterJson: gaps == null
                    ? JsonSerializer.Serialize(new { active })
                    : JsonSerializer.Serialize(new { active, priorPanelId = gaps.Prior.PanelId, coverageGaps = gaps.Gaps }));
        }

        /// <summary>How a panel has been used: linked specimens, recorded reaction rows, and
        /// linked specimens with a confirmed identification.</summary>
        public PanelUsage GetPanelUsage(int panelId)
        {
            var usage = new PanelUsage();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT sp.specimen_id, COALESCE(s.final_antibodies, '')
                    FROM specimen_panels sp LEFT JOIN specimens s ON s.accession_number = sp.specimen_id
                    WHERE sp.panel_id = $id ORDER BY sp.specimen_id";
                cmd.Parameters.AddWithValue("$id", panelId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    usage.LinkedSpecimens.Add(r.GetString(0));
                    if (!string.IsNullOrWhiteSpace(r.GetString(1))) usage.ConfirmedSpecimens.Add(r.GetString(0));
                }
            }
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM reactions r JOIN panel_runs pr ON pr.run_id = r.run_id
                    WHERE pr.panel_id = $id";
                cmd.Parameters.AddWithValue("$id", panelId);
                usage.ReactionCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            return usage;
        }

        /// <summary>
        /// Deletes a panel and, by cascade, its cells, runs and specimen links. Refused
        /// when the panel has recorded reactions or is linked to a confirmed
        /// identification, because that would erase test history; deactivate it
        /// instead. Linked specimens are recorded in the audit entry.
        /// </summary>
        public void DeletePanel(int panelId)
        {
            EnsureCanRecord();
            var usage = GetPanelUsage(panelId);
            if (usage.ProtectsHistory)
                throw new InvalidOperationException(
                    $"Panel #{panelId} cannot be deleted because it holds test history: {usage.Describe()}. " +
                    "Deactivate it instead so it is no longer offered for new testing.");
            RunInTransaction(() =>
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "DELETE FROM panels WHERE panel_id = $id";
                cmd.Parameters.AddWithValue("$id", panelId);
                cmd.ExecuteNonQuery();
                AppendAudit("delete_panel", "panel", panelId.ToString(), null,
                    usage.LinkedSpecimens.Count == 0 ? null : JsonSerializer.Serialize(new { linkedSpecimens = usage.LinkedSpecimens }),
                    null);
            });
        }

        // ── Panel Cells ───────────────────────────────────────────────────────

        public void AddPanelCell(int panelId, string cellNumber, bool typedNegative = false)
        {
            var cols = string.Join(", ", AntigenMapper.AllColumns);
            // New cells start untested: a missing antigram must never read as antigen-negative.
            var initial = typedNegative ? "'-'" : $"'{AntigenConstants.AntigenNotTested}'";
            var vals = string.Join(", ", GetAllAntigenColumns(_ => initial));
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    INSERT INTO panel_cells (panel_id, cell_number, {cols})
                    VALUES ($pid, $cn, {vals})";
                cmd.Parameters.AddWithValue("$pid", panelId);
                cmd.Parameters.AddWithValue("$cn", cellNumber);
                cmd.ExecuteNonQuery();
            }

            long cellId;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                cellId = (long)(cmd.ExecuteScalar() ?? 0L);
            }
            SeedExtraAntigensForCell(panelId, (int)cellId);
        }

        public List<PanelCell> GetPanelCells(int panelId)
        {
            var list = new List<PanelCell>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM panel_cells WHERE panel_id = $id
                ORDER BY CASE WHEN cell_number = 'AC' THEN 999
                              ELSE CAST(cell_number AS INTEGER) END";
            cmd.Parameters.AddWithValue("$id", panelId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanelCell(r));
            AttachExtraAntigens(list);
            return list;
        }

        public void UpdatePanelCellAntigen(int cellId, string antigen, string value)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(PanelIdOfCell(cellId));
            UpdatePanelCellAntigenCore(cellId, antigen, value);
        }

        private void UpdatePanelCellAntigenCore(int cellId, string antigen, string value)
        {
            value = RequireAntigenValue(value, antigen, cellId);
            if (AntigenConstants.IsWarehouse(antigen))
            {
                UpsertExtraCellAntigen(cellId, antigen, value);
                return;
            }

            var col = AntigenMapper.GetColumn(antigen);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"UPDATE panel_cells SET {col} = $val WHERE id = $id";
            cmd.Parameters.AddWithValue("$val", value);
            cmd.Parameters.AddWithValue("$id", cellId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Panel typing is stored only as +, -, ++ (homozygous) or NT. Blank means NT. Anything else,
        /// for example "0", "w" or "+w", is refused rather than stored, because analysis would read it
        /// as untyped without saying so.
        /// </summary>
        private string RequireAntigenValue(string? value, string antigen, int cellId)
        {
            var v = (value ?? string.Empty).Trim();
            if (v.Length == 0) return AntigenConstants.AntigenNotTested;
            if (AntigenConstants.IsTypedAntigenValue(v) || v == AntigenConstants.AntigenNotTested) return v;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT cell_number FROM panel_cells WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", cellId);
            var cell = cmd.ExecuteScalar() as string ?? $"#{cellId}";
            throw new ArgumentException(
                $"Cell {cell} {antigen}: \"{v}\" is not a panel typing value (+, -, ++ for homozygous, or NT).");
        }

        /// <summary>The value <see cref="UpdatePanelCell"/> would store for an antigen.</summary>
        private static string StoredTyping(PanelCell cell, string antigen) =>
            cell.Antigens.TryGetValue(antigen, out var v) &&
            (AntigenConstants.IsTypedAntigenValue(v) || v == AntigenConstants.AntigenNotTested)
                ? v
                : AntigenConstants.AntigenNotTested;

        /// <summary>
        /// Differences between the stored cells of a panel and edited copies, one line per
        /// change, e.g. "Cell 3 Jka: + → 0" or "Cell 3 donor: A1 → A2".
        /// </summary>
        public List<string> DescribePanelCellChanges(int panelId, IEnumerable<PanelCell> edited)
        {
            var stored = GetPanelCells(panelId).ToDictionary(c => c.Id);
            var extras = GetPanelExtraAntigens(panelId);
            var changes = new List<string>();
            foreach (var cell in edited)
            {
                if (!stored.TryGetValue(cell.Id, out var old)) continue;
                foreach (var ag in AntigenConstants.Antigens.Concat(extras))
                {
                    var before = StoredTyping(old, ag);
                    var after = StoredTyping(cell, ag);
                    if (AntigenConstants.IsWarehouse(ag) && !cell.Antigens.ContainsKey(ag)) continue;
                    if (before != after) changes.Add($"Cell {cell.CellNumber} {ag}: {before} → {after}");
                }
                void Meta(string label, string? before, string? after)
                {
                    var b = string.IsNullOrWhiteSpace(before) ? null : before.Trim();
                    var a = string.IsNullOrWhiteSpace(after) ? null : after.Trim();
                    if (b != a) changes.Add($"Cell {cell.CellNumber} {label}: {b ?? "—"} → {a ?? "—"}");
                }
                Meta("donor", old.DonorId, cell.DonorId);
                Meta("Rh phenotype", old.RhPhenotype, cell.RhPhenotype);
                Meta("special types", old.SpecialTypes, cell.SpecialTypes);
            }
            return changes;
        }

        /// <summary>
        /// Saves edited cell typing for a panel in one transaction and audits each change
        /// (<c>edit_panel_typing</c>). When the panel holds test history (recorded reactions
        /// or a confirmed identification), a reason is required, because the edit changes
        /// the evidence behind past results; the audit entry names the linked specimens.
        /// Returns the changes saved (empty when nothing changed).
        /// </summary>
        public List<string> SavePanelCellEdits(int panelId, IReadOnlyList<PanelCell> edited, string? reason = null)
        {
            EnsureCanRecord();
            var changes = DescribePanelCellChanges(panelId, edited);
            if (changes.Count == 0) return changes;
            var usage = GetPanelUsage(panelId);
            if (usage.ProtectsHistory && string.IsNullOrWhiteSpace(reason))
                throw new ReasonRequiredException(
                    $"Panel #{panelId} holds test history ({usage.Describe()}). A reason is required to change its typing: " +
                    string.Join("; ", changes.Take(5)) + (changes.Count > 5 ? "; …" : "") + ".", changes);

            var storedIds = GetPanelCells(panelId).Select(c => c.Id).ToHashSet();
            RunInTransaction(() =>
            {
                foreach (var cell in edited.Where(c => storedIds.Contains(c.Id)))
                    UpdatePanelCellCore(cell);
                AppendAudit("edit_panel_typing", "panel", panelId.ToString(),
                    string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                    usage.LinkedSpecimens.Count == 0 ? null : JsonSerializer.Serialize(new { linkedSpecimens = usage.LinkedSpecimens }),
                    JsonSerializer.Serialize(new { changes }));
                // Analyses of open investigations on this panel were computed from the old typing: mark them out of
                // date, as a grade change would. Confirmed and cancelled investigations are locked and not reopened;
                // the audit entry above names every linked specimen.
                // A donor ID or Rh phenotype label is not read by analysis, so editing only those does not mark
                // anything stale (a needless stale warning teaches users to ignore real ones).
                if (changes.Any(ChangesAnalysisInput))
                    foreach (var accession in OpenInvestigationsOnPanel(panelId))
                        TouchSpecimenReactionsUpdated(accession);
            });
            return changes;
        }

        /// <summary>
        /// True for a described cell change analysis reads: antigen typing or special types. False for the donor ID
        /// and the Rh phenotype label ("Cell 3 donor: …", "Cell 3 Rh phenotype: …").
        /// </summary>
        public static bool ChangesAnalysisInput(string change) =>
            !System.Text.RegularExpressions.Regex.IsMatch(change, @"^Cell \S+ (donor|Rh phenotype): ");

        /// <summary>Linked specimens on the panel with no confirmed identification that are not cancelled.</summary>
        public List<string> OpenInvestigationsOnPanel(int panelId) =>
            GetPanelUsage(panelId).LinkedSpecimens
                .Select(GetSpecimen)
                .Where(s => s != null && !s.HasFinalCall && !s.IsCancelled)
                .Select(s => s!.AccessionNumber)
                .ToList();

        /// <summary>
        /// Writes one cell's typing directly, without an audit entry. Refused when the panel was used
        /// for a confirmed identification: such edits must go through <see cref="SavePanelCellEdits"/>,
        /// which requires a reason and audits the change.
        /// </summary>
        public void UpdatePanelCell(PanelCell cell)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(PanelIdOfCell(cell.Id));
            UpdatePanelCellCore(cell);
        }

        private void UpdatePanelCellCore(PanelCell cell)
        {
            foreach (var ag in AntigenConstants.Antigens)
            {
                if (cell.Antigens.TryGetValue(ag, out var value) &&
                    (AntigenConstants.IsTypedAntigenValue(value) ||
                     value == AntigenConstants.AntigenNotTested))
                    UpdatePanelCellAntigenCore(cell.Id, ag, value);
                else
                    UpdatePanelCellAntigenCore(cell.Id, ag, AntigenConstants.AntigenNotTested);
            }
            foreach (var ag in AntigenConstants.WarehouseAntigens)
            {
                if (cell.Antigens.TryGetValue(ag, out var extra) &&
                    (AntigenConstants.IsTypedAntigenValue(extra) ||
                     extra == AntigenConstants.AntigenNotTested))
                    UpdatePanelCellAntigenCore(cell.Id, ag, extra);
            }
            UpdatePanelCellMetadataCore(cell.Id, cell.DonorId, cell.RhPhenotype, cell.SpecialTypes);
        }

        /// <summary>
        /// A panel behind a confirmed identification keeps its typing unless a reasoned, audited edit
        /// (<see cref="SavePanelCellEdits"/>) changes it. The direct, unaudited cell writers call this first.
        /// </summary>
        private void EnsurePanelEditableWithoutAudit(int? panelId)
        {
            if (panelId is not int id) return;
            var usage = GetPanelUsage(id);
            if (usage.ConfirmedSpecimens.Count > 0)
                throw new InvalidOperationException(
                    $"Panel #{id} was used for confirmed identification(s) ({usage.Describe()}). Change its typing with Edit " +
                    "on the Panels tab, which asks for a reason and records the change.");
        }

        /// <summary>Data changes need a registered user who may record results, once users are registered.</summary>
        private void EnsureCanRecord() => Services.AccessControl.Demand(this, Services.Permission.RecordResults);

        private int? PanelIdOfCell(int cellId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT panel_id FROM panel_cells WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", cellId);
            var v = cmd.ExecuteScalar();
            return v == null || v == DBNull.Value ? null : Convert.ToInt32(v);
        }

        public void UpdatePanelCellMetadata(int cellId, string? donorId, string? rhPhenotype, string? specialTypes)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(PanelIdOfCell(cellId));
            UpdatePanelCellMetadataCore(cellId, donorId, rhPhenotype, specialTypes);
        }

        private void UpdatePanelCellMetadataCore(int cellId, string? donorId, string? rhPhenotype, string? specialTypes)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE panel_cells
                SET donor_id = $donor, rh_phenotype = $rh, special_types = $notes
                WHERE id = $id";
            cmd.Parameters.AddWithValue("$donor", (object?)donorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rh", (object?)rhPhenotype ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$notes", (object?)specialTypes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", cellId);
            cmd.ExecuteNonQuery();
        }

        public void DeletePanelCells(int panelId)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(panelId);
            DeletePanelCellsCore(panelId);
        }

        private void DeletePanelCellsCore(int panelId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM panel_cells WHERE panel_id = $id";
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Replaces every cell on a panel in one transaction; a failure keeps the old cells.</summary>
        public void ReplacePanelCells(int panelId, IReadOnlyList<PanelCell> cells)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(panelId);
            RunInTransaction(() => ReplacePanelCellsCore(panelId, cells));
        }

        private void ReplacePanelCellsCore(int panelId, IReadOnlyList<PanelCell> cells)
        {
            DeletePanelCellsCore(panelId);
            int numeric = 0;
            bool includeAc = false;
            int startCell = 1;
            bool startSet = false;
            foreach (var imported in cells)
            {
                AddPanelCell(panelId, imported.CellNumber);
                if (string.Equals(imported.CellNumber, "AC", StringComparison.OrdinalIgnoreCase))
                {
                    includeAc = true;
                    continue;
                }
                numeric++;
                if (int.TryParse(imported.CellNumber, out var n) && !startSet)
                {
                    startCell = n;
                    startSet = true;
                }
            }

            var created = GetPanelCells(panelId).ToDictionary(c => c.CellNumber, StringComparer.OrdinalIgnoreCase);
            var extrasOnImport = cells
                .SelectMany(c => c.Antigens.Keys)
                .Where(AntigenConstants.IsWarehouse)
                .Distinct()
                .ToList();
            foreach (var ag in extrasOnImport)
                AddPanelExtraAntigen(panelId, ag);

            foreach (var imported in cells)
            {
                if (!created.TryGetValue(imported.CellNumber, out var cell)) continue;
                foreach (var ag in AntigenConstants.Antigens)
                {
                    if (imported.Antigens.TryGetValue(ag, out var importedVal) &&
                        (AntigenConstants.IsTypedAntigenValue(importedVal) ||
                         importedVal == AntigenConstants.AntigenNotTested))
                        cell.SetAntigen(ag, importedVal);
                    else
                        cell.SetAntigen(ag, AntigenConstants.AntigenNotTested);
                }
                foreach (var ag in extrasOnImport)
                {
                    if (imported.HasTypedAntigen(ag))
                        cell.SetAntigen(ag, imported.GetTypedValue(ag)!);
                    else if (imported.Antigens.TryGetValue(ag, out var extraVal))
                        cell.SetAntigen(ag, extraVal);
                }
                cell.DonorId = imported.DonorId;
                cell.RhPhenotype = imported.RhPhenotype;
                cell.SpecialTypes = imported.SpecialTypes;
                UpdatePanelCellCore(cell);
            }

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE panels SET num_cells = $num, include_ac = $ac, start_cell = $sc
                WHERE panel_id = $id";
            cmd.Parameters.AddWithValue("$num", numeric);
            cmd.Parameters.AddWithValue("$ac", includeAc ? 1 : 0);
            cmd.Parameters.AddWithValue("$sc", startCell);
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.ExecuteNonQuery();
        }

        public void CopyPanelCells(int sourcePanelId, int targetPanelId)
        {
            EnsureCanRecord();
            EnsurePanelEditableWithoutAudit(targetPanelId);
            // Copying replaces the whole antigram, so it is for a new lot only; a lot with recorded reactions keeps
            // the typing its results were read against (correct it cell by cell with an audited edit instead).
            if (GetPanelUsage(targetPanelId) is { ProtectsHistory: true } used)
                throw new InvalidOperationException(
                    $"Panel #{targetPanelId} holds test history ({used.Describe()}), so another lot's antigram cannot be copied over it.");
            DeletePanelCellsCore(targetPanelId);
            var agCols = string.Join(", ", AntigenMapper.AllColumns);
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    INSERT INTO panel_cells (panel_id, cell_number, donor_id, rh_phenotype, special_types, {agCols})
                    SELECT {targetPanelId}, cell_number, donor_id, rh_phenotype, special_types, {agCols}
                    FROM panel_cells WHERE panel_id = $src";
                cmd.Parameters.AddWithValue("$src", sourcePanelId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    DELETE FROM panel_extra_antigens WHERE panel_id = $tgt;
                    INSERT INTO panel_extra_antigens (panel_id, antigen_name)
                    SELECT $tgt, antigen_name FROM panel_extra_antigens WHERE panel_id = $src";
                cmd.Parameters.AddWithValue("$tgt", targetPanelId);
                cmd.Parameters.AddWithValue("$src", sourcePanelId);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO panel_cell_extra_antigens (cell_id, antigen_name, value)
                    SELECT t.id, x.antigen_name, x.value
                    FROM panel_cell_extra_antigens x
                    JOIN panel_cells s ON s.id = x.cell_id AND s.panel_id = $src
                    JOIN panel_cells t ON t.panel_id = $tgt AND t.cell_number = s.cell_number";
                cmd.Parameters.AddWithValue("$src", sourcePanelId);
                cmd.Parameters.AddWithValue("$tgt", targetPanelId);
                cmd.ExecuteNonQuery();
            }

            CopyPanelAntigenOrder(sourcePanelId, targetPanelId);

            // The typing now on the target came from another lot. Record which one, so the antigram's origin is
            // traceable and a reviewer knows to check it against the new lot's own sheet.
            var source = GetPanel(sourcePanelId);
            AppendAudit("copy_panel_typing", "panel", targetPanelId.ToString(), null,
                afterJson: JsonSerializer.Serialize(new
                {
                    sourcePanelId,
                    sourceName = source?.Name,
                    sourceLot = source?.LotNumber,
                    sourceVendor = source?.Vendor,
                    cells = GetPanelCells(targetPanelId).Count,
                }));
        }

        // ── Panel antigen display order ───────────────────────────────────────

        public List<string> GetPanelAntigenOrder(int panelId)
        {
            var list = new List<string>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT antigen_name FROM panel_antigen_order
                WHERE panel_id = $id
                ORDER BY sort_index";
            cmd.Parameters.AddWithValue("$id", panelId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
            return list;
        }

        public List<string> GetPanelDisplayAntigens(int panelId) =>
            AntigenConstants.ResolveDisplayOrder(
                GetPanelAntigenOrder(panelId),
                GetPanelExtraAntigens(panelId)).ToList();

        /// <summary>
        /// Antigens this panel actually types. A saved import order is the sheet
        /// set. Otherwise any antigen with a +/− on at least one cell is typed.
        /// Missing / NT values are not invented as negatives.
        /// </summary>
        public List<string> GetPanelTypedAntigens(int panelId)
        {
            var extras = GetPanelExtraAntigens(panelId);
            var order = GetPanelAntigenOrder(panelId);
            if (order.Count > 0)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var typed = new List<string>();
                foreach (var ag in order.Concat(extras))
                {
                    if (!AntigenConstants.IsKnown(ag) || !seen.Add(ag)) continue;
                    typed.Add(ag);
                }
                return typed;
            }

            var fromCells = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in GetPanelCells(panelId))
            {
                foreach (var ag in cell.Antigens.Keys)
                {
                    if (cell.HasTypedAntigen(ag))
                        fromCells.Add(ag);
                }
            }
            foreach (var extra in extras)
                fromCells.Add(extra);
            if (fromCells.Count == 0)
                return AntigenConstants.GetAnalyzedAntigens(extras).ToList();
            return AntigenConstants.GetAnalyzedAntigens(extras)
                .Where(fromCells.Contains)
                .ToList();
        }

        /// <summary>
        /// Saves a panel's antigen display order, audited (<c>set_antigen_order</c>) when a
        /// previously saved order changes. Order affects display only, not analysis.
        /// </summary>
        public void SetPanelAntigenOrder(int panelId, IReadOnlyList<string> antigens)
        {
            EnsureCanRecord();
            var previous = GetPanelAntigenOrder(panelId);
            SetPanelAntigenOrderCore(panelId, antigens);
            var current = GetPanelAntigenOrder(panelId);
            if (previous.Count > 0 && !previous.SequenceEqual(current))
                AppendAudit("set_antigen_order", "panel", panelId.ToString(), null,
                    JsonSerializer.Serialize(new { order = previous }), JsonSerializer.Serialize(new { order = current }));
        }

        private void SetPanelAntigenOrderCore(int panelId, IReadOnlyList<string> antigens)
        {
            using (var del = _conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM panel_antigen_order WHERE panel_id = $id";
                del.Parameters.AddWithValue("$id", panelId);
                del.ExecuteNonQuery();
            }

            var extras = GetPanelExtraAntigens(panelId);
            var resolved = AntigenConstants.ResolveDisplayOrder(antigens, extras);
            using var insert = _conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO panel_antigen_order (panel_id, antigen_name, sort_index)
                VALUES ($pid, $ag, $idx)";
            insert.Parameters.AddWithValue("$pid", panelId);
            var agParam = insert.Parameters.Add("$ag", SqliteType.Text);
            var idxParam = insert.Parameters.Add("$idx", SqliteType.Integer);
            int i = 0;
            foreach (var ag in resolved)
            {
                if (!AntigenConstants.IsKnown(ag)) continue;
                agParam.Value = ag;
                idxParam.Value = i++;
                insert.ExecuteNonQuery();
            }
        }

        public void CopyPanelAntigenOrder(int sourcePanelId, int targetPanelId)
        {
            EnsureCanRecord();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM panel_antigen_order WHERE panel_id = $tgt;
                INSERT INTO panel_antigen_order (panel_id, antigen_name, sort_index)
                SELECT $tgt, antigen_name, sort_index
                FROM panel_antigen_order WHERE panel_id = $src";
            cmd.Parameters.AddWithValue("$tgt", targetPanelId);
            cmd.Parameters.AddWithValue("$src", sourcePanelId);
            cmd.ExecuteNonQuery();
        }

        // ── Warehouse / extra antigens ────────────────────────────────────────

        public List<string> GetPanelExtraAntigens(int panelId)
        {
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT antigen_name FROM panel_extra_antigens WHERE panel_id = $id";
            cmd.Parameters.AddWithValue("$id", panelId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) assigned.Add(r.GetString(0));
            return AntigenConstants.WarehouseAntigens.Where(assigned.Contains).ToList();
        }

        public List<string> GetExtraAntigensForPanels(IEnumerable<int> panelIds)
        {
            var ids = panelIds.Distinct().ToList();
            if (ids.Count == 0) return new();
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT DISTINCT antigen_name FROM panel_extra_antigens
                WHERE panel_id IN ({string.Join(",", ids)})";
            using var r = cmd.ExecuteReader();
            while (r.Read()) assigned.Add(r.GetString(0));
            return AntigenConstants.WarehouseAntigens.Where(assigned.Contains).ToList();
        }

        public bool PanelHasExtraAntigen(int panelId, string antigen)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 1 FROM panel_extra_antigens
                WHERE panel_id = $id AND antigen_name = $ag LIMIT 1";
            cmd.Parameters.AddWithValue("$id", panelId);
            cmd.Parameters.AddWithValue("$ag", antigen);
            return cmd.ExecuteScalar() != null;
        }

        /// <summary>
        /// Adds an extra (warehouse) antigen column to a panel. Cells start as NT (not
        /// tested), so the antigen stays out of analysis until it is typed; it is never
        /// assumed negative. Audited when the column is new.
        /// </summary>
        public void AddPanelExtraAntigen(int panelId, string antigen)
        {
            EnsureCanRecord();
            if (!AntigenConstants.IsWarehouse(antigen)) return;
            int inserted;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO panel_extra_antigens (panel_id, antigen_name)
                    VALUES ($pid, $ag)";
                cmd.Parameters.AddWithValue("$pid", panelId);
                cmd.Parameters.AddWithValue("$ag", antigen);
                inserted = cmd.ExecuteNonQuery();
            }

            foreach (var cell in GetPanelCells(panelId))
            {
                if (cell.HasTypedAntigen(antigen)) continue;
                UpsertExtraCellAntigen(cell.Id, antigen, AntigenConstants.AntigenNotTested);
            }
            if (inserted > 0)
                AppendAudit("add_extra_antigen", "panel", panelId.ToString(), null, null,
                    JsonSerializer.Serialize(new { antigen }));

            var saved = GetPanelAntigenOrder(panelId);
            if (saved.Count > 0)
                SetPanelAntigenOrder(panelId, saved);
        }

        /// <summary>
        /// Removes an extra antigen column and its typing. Refused when the panel holds
        /// test history and any cell is typed for the antigen, because that typing is
        /// evidence behind past results. Audited with the removed values.
        /// </summary>
        public void RemovePanelExtraAntigen(int panelId, string antigen)
        {
            EnsureCanRecord();
            var typed = GetPanelCells(panelId)
                .Where(c => c.HasTypedAntigen(antigen))
                .ToDictionary(c => c.CellNumber, c => c.Antigens[antigen]);
            if (typed.Count > 0 && GetPanelUsage(panelId) is { ProtectsHistory: true } usage)
                throw new InvalidOperationException(
                    $"{antigen} is typed on {typed.Count} cell(s) of panel #{panelId}, which holds test history " +
                    $"({usage.Describe()}). Removing it would erase evidence behind those results. " +
                    "Mark individual values NT with Edit if they are wrong.");
            RunInTransaction(() => RemovePanelExtraAntigenCore(panelId, antigen, typed));
        }

        private void RemovePanelExtraAntigenCore(int panelId, string antigen, Dictionary<string, string> typed)
        {
            AppendAudit("remove_extra_antigen", "panel", panelId.ToString(), null,
                JsonSerializer.Serialize(new { antigen, typed }), null);
            var saved = GetPanelAntigenOrder(panelId);
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    DELETE FROM panel_cell_extra_antigens
                    WHERE antigen_name = $ag
                      AND cell_id IN (SELECT id FROM panel_cells WHERE panel_id = $pid)";
                cmd.Parameters.AddWithValue("$ag", antigen);
                cmd.Parameters.AddWithValue("$pid", panelId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"
                    DELETE FROM panel_extra_antigens
                    WHERE panel_id = $pid AND antigen_name = $ag";
                cmd.Parameters.AddWithValue("$pid", panelId);
                cmd.Parameters.AddWithValue("$ag", antigen);
                cmd.ExecuteNonQuery();
            }
            if (saved.Count > 0)
                SetPanelAntigenOrder(panelId, saved);
        }

        private void SeedExtraAntigensForCell(int panelId, int cellId)
        {
            foreach (var ag in GetPanelExtraAntigens(panelId))
                UpsertExtraCellAntigen(cellId, ag, "-");
        }

        private void UpsertExtraCellAntigen(int cellId, string antigen, string value)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO panel_cell_extra_antigens (cell_id, antigen_name, value)
                VALUES ($id, $ag, $val)
                ON CONFLICT(cell_id, antigen_name) DO UPDATE SET value = $val";
            cmd.Parameters.AddWithValue("$id", cellId);
            cmd.Parameters.AddWithValue("$ag", antigen);
            cmd.Parameters.AddWithValue("$val", value);
            cmd.ExecuteNonQuery();
        }

        private void AttachExtraAntigens(IReadOnlyList<PanelCell> cells)
        {
            if (cells.Count == 0) return;
            var byId = cells.ToDictionary(c => c.Id);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT cell_id, antigen_name, value
                FROM panel_cell_extra_antigens
                WHERE cell_id IN ({string.Join(",", byId.Keys)})";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var cellId = r.GetInt32(0);
                if (!byId.TryGetValue(cellId, out var cell)) continue;
                cell.Antigens[r.GetString(1)] = r.GetString(2);
            }
        }

        // ── Specimen-Panel Linking ─────────────────────────────────────────────

        public void LinkSpecimenPanel(string specimenId, int panelId)
        {
            EnsureCanRecord();
            EnsureSpecimenUnlocked(specimenId);
            try
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO specimen_panels (specimen_id, panel_id) VALUES ($sid, $pid)";
                cmd.Parameters.AddWithValue("$sid", specimenId);
                cmd.Parameters.AddWithValue("$pid", panelId);
                cmd.ExecuteNonQuery();
                AppendAudit("link_panel", "specimen", specimenId, null,
                    afterJson: JsonSerializer.Serialize(new { panelId, panel = PanelAuditName(panelId) }));
            }
            catch (SqliteException) { /* already linked */ }
        }

        public void UnlinkSpecimenPanel(string specimenId, int panelId)
        {
            EnsureCanRecord();
            EnsureSpecimenUnlocked(specimenId);
            // Analysis reads every run on the specimen, so detaching a panel that has grades would leave
            // evidence in the analysis that the specimen no longer lists.
            var graded = GetReactionRecords(specimenId)
                .Count(r => r.PanelId == panelId && r.Grades.Values.Any(Services.DataConsistencyCheck.IsGraded));
            if (graded > 0)
                throw new InvalidOperationException(
                    $"{PanelAuditName(panelId) ?? $"Panel #{panelId}"} has {graded} graded cell(s) for {specimenId}. " +
                    "Analysis uses them, so the panel stays attached. Clear its reactions on the Reactions tab (with a reason) first.");
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM specimen_panels WHERE specimen_id = $sid AND panel_id = $pid";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$pid", panelId);
            if (cmd.ExecuteNonQuery() > 0)
                AppendAudit("unlink_panel", "specimen", specimenId, null,
                    beforeJson: JsonSerializer.Serialize(new { panelId, panel = PanelAuditName(panelId) }));
        }

        private string? PanelAuditName(int panelId)
        {
            var p = GetPanel(panelId);
            if (p == null) return null;
            return string.IsNullOrWhiteSpace(p.LotNumber) ? p.Name : $"{p.Name} (lot {p.LotNumber})";
        }

        /// <summary>
        /// Every panel each specimen was tested on (linked panels and panel runs), keyed by
        /// accession, in one query for Search. Each entry is (name, vendor, lot), ordered by name.
        /// </summary>
        public Dictionary<string, List<(string Name, string? Vendor, string? Lot)>> GetPanelsBySpecimen()
        {
            var map = new Dictionary<string, List<(string, string?, string?)>>(StringComparer.OrdinalIgnoreCase);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT u.specimen_id, p.name, p.vendor, p.lot_number FROM (
                    SELECT specimen_id, panel_id FROM specimen_panels
                    UNION
                    SELECT specimen_id, panel_id FROM panel_runs
                ) u JOIN panels p ON p.panel_id = u.panel_id
                ORDER BY u.specimen_id, p.name, p.panel_id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var acc = r.GetString(0);
                if (!map.TryGetValue(acc, out var list)) map[acc] = list = new List<(string, string?, string?)>();
                list.Add((r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)));
            }
            return map;
        }

        public List<Panel> GetSpecimenPanels(string specimenId)
        {
            var list = new List<Panel>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.* FROM panels p
                JOIN specimen_panels sp ON p.panel_id = sp.panel_id
                WHERE sp.specimen_id = $sid ORDER BY p.name";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanel(r));
            return list;
        }

        // ── Panel Runs ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new panel run and returns its run_id.
        /// Throws <see cref="SqliteException"/> if a run with the same
        /// (specimen, panel, cell_treatment, serum_treatment) already exists.
        /// </summary>
        public int AddPanelRun(string specimenId, int panelId,
            CellTreatment cellTreatment,
            SerumTreatment serumTreatment,
            string label = "")
        {
            EnsureCanRecord();
            EnsureSpecimenUnlocked(specimenId);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO panel_runs (specimen_id, panel_id, cell_treatment, serum_treatment, label, created_date)
                VALUES ($sid, $pid, $ct, $st, $lbl, $dt);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$pid", panelId);
            cmd.Parameters.AddWithValue("$ct", cellTreatment.ToString());
            cmd.Parameters.AddWithValue("$st", serumTreatment.ToString());
            cmd.Parameters.AddWithValue("$lbl", (object?)label ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$dt", DateTime.Now.ToString("yyyy-MM-dd"));
            var runId = Convert.ToInt32(cmd.ExecuteScalar());
            AppendAudit("add_run", "specimen", specimenId, null, null, JsonSerializer.Serialize(new
            {
                runId,
                panel = GetPanel(panelId)?.Name,
                cellTreatment = cellTreatment.ToString(),
                serumTreatment = serumTreatment.ToString(),
                label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
            }));
            return runId;
        }

        /// <summary>
        /// Returns the run_id of the untreated (default) run for a specimen/panel pair,
        /// creating one if it does not yet exist.
        /// </summary>
        public int GetOrCreateDefaultRun(string specimenId, int panelId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT run_id FROM panel_runs
                WHERE specimen_id = $sid AND panel_id = $pid
                  AND cell_treatment = 'None' AND serum_treatment = 'None'";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$pid", panelId);
            var existing = cmd.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            return AddPanelRun(specimenId, panelId,
                CellTreatment.None,
                SerumTreatment.None,
                "Untreated");
        }

        public PanelRun? GetPanelRun(int runId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT pr.*, p.name AS panel_name
                FROM panel_runs pr
                JOIN panels p ON pr.panel_id = p.panel_id
                WHERE pr.run_id = $id";
            cmd.Parameters.AddWithValue("$id", runId);
            using var r = cmd.ExecuteReader();
            return r.Read() ? ReadPanelRun(r) : null;
        }

        public List<PanelRun> GetPanelRuns(string specimenId, int panelId)
        {
            var list = new List<PanelRun>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT pr.*, p.name AS panel_name
                FROM panel_runs pr
                JOIN panels p ON pr.panel_id = p.panel_id
                WHERE pr.specimen_id = $sid AND pr.panel_id = $pid
                ORDER BY pr.run_id";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$pid", panelId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanelRun(r));
            return list;
        }

        public List<PanelRun> GetAllSpecimenRuns(string specimenId)
        {
            var list = new List<PanelRun>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT pr.*, p.name AS panel_name
                FROM panel_runs pr
                JOIN panels p ON pr.panel_id = p.panel_id
                WHERE pr.specimen_id = $sid
                ORDER BY p.name, pr.run_id";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadPanelRun(r));
            return list;
        }

        /// <summary>Cells on a run with at least one graded (not NT or blank) result.</summary>
        public int CountGradedReactions(int runId) =>
            GetReactions(runId).Count(r => IsGraded(r.IS) || IsGraded(r.C37) || IsGraded(r.AHG) || IsGraded(r.CC) ||
                                           r.ExtraPhases.Values.Any(IsGraded));

        /// <summary>
        /// Checks a deletion of recorded results and returns the snapshot for its audit
        /// entry. Locked specimens are refused; after analysis, deleting graded results
        /// needs a reason.
        /// </summary>
        private object? PrepareReactionDeletion(PanelRun run, string what, string? reason)
        {
            EnsureSpecimenUnlocked(run.SpecimenId);
            var rows = GetReactions(run.RunId);
            var graded = CountGradedReactions(run.RunId);
            if (graded > 0 && string.IsNullOrWhiteSpace(reason) && GetSpecimen(run.SpecimenId)?.LastAnalyzedAt != null)
                throw new ReasonRequiredException(
                    $"{what} would delete {graded} graded cell(s) that were already analyzed. A reason is required.",
                    new[] { $"{graded} graded cell(s) on {run.PanelName}, {run.DisplayLabel}" });
            return new
            {
                runId = run.RunId, panel = run.PanelName, run = run.DisplayLabel, gradedCells = graded,
                reactions = rows.Select(r => new { cell = r.CellNumber, is_ = r.IS, c37 = r.C37, ahg = r.AHG, cc = r.CC, extra = r.ExtraPhases }),
            };
        }

        /// <summary>
        /// Deletes a treatment run and its reactions. Refused after a confirmed
        /// identification; after analysis, graded results need a reason. Audited
        /// (<c>delete_run</c>) with every deleted grade.
        /// </summary>
        public void DeletePanelRun(int runId, string? reason = null)
        {
            EnsureCanRecord();
            var run = GetPanelRun(runId);
            if (run == null) return;
            var snapshot = PrepareReactionDeletion(run, $"Deleting run '{run.DisplayLabel}'", reason);
            RunInTransaction(() =>
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "DELETE FROM panel_runs WHERE run_id = $id";
                cmd.Parameters.AddWithValue("$id", runId);
                cmd.ExecuteNonQuery();
                AppendAudit("delete_run", "specimen", run.SpecimenId,
                    string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), JsonSerializer.Serialize(snapshot), null);
                TouchSpecimenReactionsUpdated(run.SpecimenId);
            });
        }

        // ── Reactions ─────────────────────────────────────────────────────────

        private static bool IsGraded(string? v) =>
            !string.IsNullOrWhiteSpace(v) && !string.Equals(v, "NT", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Recorded results the given grades would replace on a run, one line per phase,
        /// e.g. "Cell 3 AHG: 2+ → 0". A phase counts only when it already held a graded
        /// value (not NT or blank) and the new value differs.
        /// </summary>
        public List<string> DescribeReactionChanges(int runId, IEnumerable<ReactionInput> rows)
        {
            var stored = GetReactions(runId).ToDictionary(r => r.CellNumber, StringComparer.OrdinalIgnoreCase);
            var changes = new List<string>();
            foreach (var row in rows)
            {
                if (!stored.TryGetValue(row.CellNumber, out var old)) continue;
                void Compare(string phase, string? before, string? after)
                {
                    if (IsGraded(before) && !string.Equals(before, after ?? "NT", StringComparison.Ordinal))
                        changes.Add($"Cell {row.CellNumber} {phase}: {before} → {(string.IsNullOrWhiteSpace(after) ? "NT" : after)}");
                }
                Compare("IS", old.IS, row.IS);
                Compare("37°C", old.C37, row.C37);
                Compare("AHG", old.AHG, row.AHG);
                Compare("CC", old.CC, row.CC);
                var oldExtra = old.ExtraPhases;
                foreach (var (phase, value) in oldExtra)
                    Compare(phase, value, row.ExtraPhases != null && row.ExtraPhases.TryGetValue(phase, out var nv) ? nv : null);
            }
            return changes;
        }

        /// <summary>
        /// Saves a run's grades in one transaction. Once the specimen has been analyzed,
        /// changing a recorded result needs a reason, which is stored on each change's
        /// audit entry; without one, nothing is saved.
        /// </summary>
        public void SaveReactions(int runId, IReadOnlyList<ReactionInput> rows, string? reason = null)
        {
            EnsureCanRecord();
            var run = GetPanelRun(runId);
            if (run != null && string.IsNullOrWhiteSpace(reason) &&
                GetSpecimen(run.SpecimenId)?.LastAnalyzedAt != null)
            {
                var changes = DescribeReactionChanges(runId, rows);
                if (changes.Count > 0)
                    throw new ReasonRequiredException(
                        $"A reason is required to change {changes.Count} recorded result(s) after analysis: " +
                        string.Join("; ", changes.Take(5)) + (changes.Count > 5 ? "; …" : "") + ".", changes);
            }
            RunInTransaction(() =>
            {
                foreach (var r in rows)
                    SaveReaction(runId, r.CellNumber, r.IS, r.C37, r.AHG, r.CC, r.ExtraPhases, reason);
            });
        }

        /// <summary>Primary overload: saves a reaction for a specific run.</summary>
        public void SaveReaction(int runId, string cellNumber,
            string is_, string c37, string ahg, string cc,
            IReadOnlyDictionary<string, string>? extraPhases = null, string? reason = null)
        {
            EnsureCanRecord();
            // Need specimen_id to call TouchSpecimenReactionsUpdated
            var run = GetPanelRun(runId);
            if (run != null) EnsureSpecimenUnlocked(run.SpecimenId);
            // Grades are stored in their normalized form; anything unrecognized is refused, as is a cell the
            // panel does not have (it could not be matched to antigen typing). See DataConsistencyCheck.
            is_ = RequireGrade(is_, cellNumber, "IS");
            c37 = RequireGrade(c37, cellNumber, "37°C");
            ahg = RequireGrade(ahg, cellNumber, "AHG");
            cc = RequireGrade(cc, cellNumber, "CC");
            if (extraPhases != null)
                extraPhases = extraPhases.ToDictionary(kv => kv.Key, kv => RequireGrade(kv.Value, cellNumber, kv.Key),
                    StringComparer.OrdinalIgnoreCase);
            if (run != null) EnsureCellOnPanel(run.PanelId, cellNumber);
            var extra = ExtraPhaseParser.Serialize(extraPhases);

            // Unchanged saves write nothing, so the audit log holds only real entries and changes.
            object? before = null;
            var replacesResult = false;
            using (var read = _conn.CreateCommand())
            {
                read.CommandText = @"SELECT ""IS"", C37, AHG, CC, extra_phases FROM reactions
                                     WHERE run_id = $rid AND cell_number = $cn";
                read.Parameters.AddWithValue("$rid", runId);
                read.Parameters.AddWithValue("$cn", cellNumber);
                using var r = read.ExecuteReader();
                if (r.Read())
                {
                    string Col(int i) => r.IsDBNull(i) ? "" : r.GetString(i);
                    var oldExtra = r.IsDBNull(4) ? null : r.GetString(4);
                    if (Col(0) == is_ && Col(1) == c37 && Col(2) == ahg && Col(3) == cc &&
                        string.IsNullOrEmpty(oldExtra) == string.IsNullOrEmpty(extra) &&
                        (string.IsNullOrEmpty(extra) || oldExtra == extra))
                        return;
                    before = new { runId, cellNumber, is_ = Col(0), c37 = Col(1), ahg = Col(2), cc = Col(3), extra = oldExtra };
                    replacesResult = IsGraded(Col(0)) || IsGraded(Col(1)) || IsGraded(Col(2)) || IsGraded(Col(3)) ||
                                     ExtraPhaseParser.Deserialize(oldExtra).Values.Any(IsGraded);
                }
            }

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO reactions (run_id, cell_number, ""IS"", C37, AHG, CC, extra_phases)
                VALUES ($rid, $cn, $is, $c37, $ahg, $cc, $extra)";
            cmd.Parameters.AddWithValue("$rid", runId);
            cmd.Parameters.AddWithValue("$cn", cellNumber);
            cmd.Parameters.AddWithValue("$is", is_);
            cmd.Parameters.AddWithValue("$c37", c37);
            cmd.Parameters.AddWithValue("$ahg", ahg);
            cmd.Parameters.AddWithValue("$cc", cc);
            cmd.Parameters.AddWithValue("$extra", (object?)extra ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            if (run != null)
            {
                TouchSpecimenReactionsUpdated(run.SpecimenId);
                AppendAudit("save_reaction", "specimen", run.SpecimenId,
                    !replacesResult || string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                    before == null ? null : JsonSerializer.Serialize(before),
                    JsonSerializer.Serialize(new
                    {
                        runId,
                        cellNumber,
                        is_,
                        c37,
                        ahg,
                        cc,
                        extra
                    }));
            }
        }

        /// <summary>
        /// Compatibility overload: resolves (or creates) the untreated default run,
        /// then saves the reaction.
        /// </summary>
        public void SaveReaction(string specimenId, int panelId, string cellNumber,
            string is_, string c37, string ahg, string cc)
        {
            EnsureCanRecord();
            var runId = GetOrCreateDefaultRun(specimenId, panelId);
            SaveReaction(runId, cellNumber, is_, c37, ahg, cc);
        }

        private static string RequireGrade(string? raw, string cellNumber, string phase)
        {
            var normalized = ReactionGrade.Normalize(raw);
            if (ReactionGrade.All.Contains(normalized, StringComparer.Ordinal)) return normalized;
            throw new ArgumentException(
                $"Cell {cellNumber} {phase}: \"{raw?.Trim()}\" is not a recognized grade (0, w+, 1+ to 4+, MF, H or NT).");
        }

        private void EnsureCellOnPanel(int panelId, string cellNumber)
        {
            if (string.Equals(cellNumber, "AC", StringComparison.OrdinalIgnoreCase)) return;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM panel_cells WHERE panel_id = $pid AND cell_number = $cn";
            cmd.Parameters.AddWithValue("$pid", panelId);
            cmd.Parameters.AddWithValue("$cn", cellNumber);
            if (cmd.ExecuteScalar() != null) return;
            throw new ArgumentException(
                $"Cell {cellNumber} is not on {PanelAuditName(panelId) ?? $"panel #{panelId}"}; grades can only be recorded for the panel's own cells.");
        }

        /// <summary>
        /// Every stored reaction (or one specimen's), with whether its panel is still attached to the
        /// specimen and whether the panel still has its cell, for <see cref="Services.DataConsistencyCheck"/>.
        /// Grades are returned as stored, not normalized.
        /// </summary>
        public List<Services.ReactionRecord> GetReactionRecords(string? specimenId = null)
        {
            var list = new List<Services.ReactionRecord>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT pr.specimen_id, pr.panel_id, p.name, rxn.cell_number,
                       rxn.""IS"", rxn.C37, rxn.AHG, rxn.CC, rxn.extra_phases,
                       EXISTS (SELECT 1 FROM specimen_panels sp WHERE sp.specimen_id = pr.specimen_id AND sp.panel_id = pr.panel_id),
                       EXISTS (SELECT 1 FROM panel_cells pc WHERE pc.panel_id = pr.panel_id AND pc.cell_number = rxn.cell_number)
                FROM reactions rxn
                JOIN panel_runs pr ON rxn.run_id = pr.run_id
                JOIN panels p ON p.panel_id = pr.panel_id
                WHERE $sid IS NULL OR pr.specimen_id = $sid";
            cmd.Parameters.AddWithValue("$sid", (object?)specimenId ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string? Col(int i) => r.IsDBNull(i) ? null : r.GetString(i);
                var grades = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["IS"] = Col(4), ["37°C"] = Col(5), ["AHG"] = Col(6), ["CC"] = Col(7),
                };
                foreach (var (phase, value) in ExtraPhaseParser.Deserialize(Col(8)))
                    grades[phase] = value;
                var cell = r.GetString(3);
                list.Add(new Services.ReactionRecord(r.GetString(0), r.GetInt32(1), r.GetString(2), cell, grades,
                    PanelLinked: r.GetInt64(9) != 0,
                    CellOnPanel: r.GetInt64(10) != 0 || string.Equals(cell, "AC", StringComparison.OrdinalIgnoreCase)));
            }
            return list;
        }

        /// <summary>Returns reactions for a specific run.</summary>
        public List<Reaction> GetReactions(int runId)
        {
            var list = new List<Reaction>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT rxn.reaction_id, rxn.run_id, rxn.cell_number,
                       rxn.""IS"", rxn.C37, rxn.AHG, rxn.CC, rxn.extra_phases,
                       pr.specimen_id, pr.panel_id, pr.cell_treatment, pr.serum_treatment
                FROM reactions rxn
                JOIN panel_runs pr ON rxn.run_id = pr.run_id
                WHERE rxn.run_id = $rid
                ORDER BY CASE WHEN rxn.cell_number = 'AC' THEN 999
                              ELSE CAST(rxn.cell_number AS INTEGER) END";
            cmd.Parameters.AddWithValue("$rid", runId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadReactionFull(r));
            return list;
        }

        /// <summary>
        /// Compatibility overload: returns reactions for the untreated default run of
        /// a (specimen, panel) pair. Returns empty if no default run exists.
        /// </summary>
        public List<Reaction> GetReactions(string specimenId, int panelId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT run_id FROM panel_runs
                WHERE specimen_id = $sid AND panel_id = $pid
                  AND cell_treatment = 'None' AND serum_treatment = 'None'";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$pid", panelId);
            var runId = cmd.ExecuteScalar();
            if (runId == null || runId == DBNull.Value) return new();
            return GetReactions(Convert.ToInt32(runId));
        }

        /// <summary>Returns all reactions for a specimen across every run.</summary>
        public List<Reaction> GetAllSpecimenReactions(string specimenId)
        {
            var list = new List<Reaction>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT rxn.reaction_id, rxn.run_id, rxn.cell_number,
                       rxn.""IS"", rxn.C37, rxn.AHG, rxn.CC, rxn.extra_phases,
                       pr.specimen_id, pr.panel_id, pr.cell_treatment, pr.serum_treatment
                FROM reactions rxn
                JOIN panel_runs pr ON rxn.run_id = pr.run_id
                JOIN panels p ON pr.panel_id = p.panel_id
                WHERE pr.specimen_id = $sid
                ORDER BY p.name, pr.run_id,
                    CASE WHEN rxn.cell_number = 'AC' THEN 999
                         ELSE CAST(rxn.cell_number AS INTEGER) END";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadReactionFull(r));
            return list;
        }

        /// <summary>Deletes all reactions for a specific run.</summary>
        /// <summary>
        /// Clears every reaction on a run. Refused after a confirmed identification;
        /// after analysis, graded results need a reason. Audited (<c>clear_reactions</c>)
        /// with every deleted grade; clearing an empty run writes nothing.
        /// </summary>
        public void DeleteReactions(int runId, string? reason = null)
        {
            EnsureCanRecord();
            var run = GetPanelRun(runId);
            if (run == null) return;
            if (GetReactions(runId).Count == 0) return;
            var snapshot = PrepareReactionDeletion(run, "Clearing this run", reason);
            RunInTransaction(() =>
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "DELETE FROM reactions WHERE run_id = $rid";
                cmd.Parameters.AddWithValue("$rid", runId);
                cmd.ExecuteNonQuery();
                AppendAudit("clear_reactions", "specimen", run.SpecimenId,
                    string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), JsonSerializer.Serialize(snapshot), null);
                TouchSpecimenReactionsUpdated(run.SpecimenId);
            });
        }

        /// <summary>
        /// Compatibility overload: deletes reactions for the untreated default run.
        /// </summary>
        public void DeleteReactions(string specimenId, int panelId, string? reason = null)
        {
            EnsureCanRecord();
            EnsureSpecimenUnlocked(specimenId);
            foreach (var run in GetPanelRuns(specimenId, panelId).Where(r => r.IsUntreated))
                DeleteReactions(run.RunId, reason);
        }

        // ── Worklist ──────────────────────────────────────────────────────────

        /// <summary>
        /// Linked panels whose untreated run (or first run) lacks grades for
        /// any cell, as "Panel name (entered/total)" or the bare name when no
        /// run exists. Empty when every linked panel is fully graded.
        /// </summary>
        public List<string> GetIncompleteReactionPanels(string accessionNumber, List<Panel>? panels = null)
        {
            var missing = new List<string>();
            foreach (var p in panels ?? GetSpecimenPanels(accessionNumber))
            {
                var runs = GetPanelRuns(accessionNumber, p.PanelId);
                var run = runs.FirstOrDefault(r => r.IsUntreated) ?? runs.FirstOrDefault();
                if (run == null)
                {
                    missing.Add(p.Name);
                    continue;
                }
                var cells = GetPanelCells(p.PanelId);
                var rxns = GetReactions(run.RunId).ToDictionary(r => r.CellNumber);
                int entered = 0;
                foreach (var cell in cells)
                {
                    if (!rxns.TryGetValue(cell.CellNumber, out var rxn)) continue;
                    if (HasEnteredGrade(rxn)) entered++;
                }
                if (entered == 0 || entered < cells.Count)
                    missing.Add($"{p.Name} ({entered}/{cells.Count})");
            }
            return missing;
        }

        public int CountAuditEvents(string action, string entityId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM audit_events WHERE action = $a AND entity_id = $id";
            cmd.Parameters.AddWithValue("$a", action);
            cmd.Parameters.AddWithValue("$id", entityId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<WorklistItem> GetWorklistItems(int expirationWarningDays = 14)
        {
            var items = new List<WorklistItem>();
            var today = DateTime.Now.Date;
            var horizon = today.AddDays(expirationWarningDays).ToString("yyyy-MM-dd");
            var todayStr = today.ToString("yyyy-MM-dd");
            var currentSettingsJson = AppSettings.Current.ClinicalSettingsJson();
            var savedSettings = GetLatestSnapshotSettingsBySpecimen();

            var configChanged = LastAnalysisConfigurationChange();
            // The application refuses both orders (cancel with an ID, confirm when cancelled), so this
            // state only comes from older or imported data. Analytics leaves these records out.
            foreach (var s in GetAllSpecimens().Where(s => s.IsCancelled && s.HasFinalCall))
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.InconsistentStatus,
                    KindLabel = "Inconsistent status",
                    Title = s.AccessionNumber,
                    Detail = $"Cancelled {s.CancelledAt} but still holds confirmed identification {s.FinalAntibodies}. " +
                             "Analytics does not count it. " +
                             AccessControl.NextStep(this, Permission.ReinstateInvestigation,
                                 "Reinstate and keep the identification, or clear it with a reason.",
                                 "reinstate it and keep the identification, or clear the identification with a reason."),
                    UrgencyLabel = "Resolve",
                    SortOrder = 1,
                    AccessionNumber = s.AccessionNumber,
                    TargetTab = "Specimens",
                });
            foreach (var s in GetAllSpecimens().Where(s => s.IsActive && !s.IsCancelled))
            {
                var panels = GetSpecimenPanels(s.AccessionNumber);
                if (!s.HasFinalCall)
                {
                    if (panels.Count == 0)
                    {
                        items.Add(new WorklistItem
                        {
                            Kind = WorklistKind.IncompleteReactions,
                            KindLabel = "Incomplete",
                            Title = s.AccessionNumber,
                            Detail = "No panel attached.",
                            UrgencyLabel = "Needs panel",
                            SortOrder = 1,
                            AccessionNumber = s.AccessionNumber,
                            TargetTab = "Specimens"
                        });
                    }
                    else
                    {
                        var missing = GetIncompleteReactionPanels(s.AccessionNumber, panels);
                        if (missing.Count > 0)
                        {
                            items.Add(new WorklistItem
                            {
                                Kind = WorklistKind.IncompleteReactions,
                                KindLabel = "Incomplete",
                                Title = s.AccessionNumber,
                                Detail = "Reactions not finished: " + string.Join("; ", missing),
                                UrgencyLabel = "Needs grades",
                                SortOrder = 1,
                                AccessionNumber = s.AccessionNumber,
                                TargetTab = "Reactions"
                            });
                        }
                    }

                    var drifted = !s.HasFinalCall && s.LastAnalyzedAt != null &&
                        savedSettings.TryGetValue(s.AccessionNumber, out var savedJson) &&
                        LabSettings.ClinicalSettingsDiffer(savedJson, currentSettingsJson);
                    var staleReason = Services.AnalysisCurrency.StaleReason(s, configChanged, drifted);
                    var neverAnalyzed = s.LastAnalyzedAt == null && panels.Count > 0;
                    if (staleReason != null || neverAnalyzed)
                    {
                        items.Add(new WorklistItem
                        {
                            Kind = WorklistKind.StaleAnalysis,
                            KindLabel = "Stale analysis",
                            Title = s.AccessionNumber,
                            Detail = staleReason ?? "Never analyzed.",
                            UrgencyLabel = "Re-analyze",
                            SortOrder = 2,
                            AccessionNumber = s.AccessionNumber,
                            TargetTab = "Analysis"
                        });
                    }
                }

                if (Services.OverdueTesting.Item(this, s, Services.AppSettings.Current.TurnaroundThresholdDays, DateTime.Now)
                    is WorklistItem overdue)
                    items.Add(overdue);

                // Only specimens tested on a lot that has expired by now can have expired-reagent results.
                if ((!s.HasFinalCall || !s.IsReviewed) && panels.Any(p => p.IsExpired) &&
                    Services.ExpiredReagentCheck.Check(this, s.AccessionNumber) is { Count: > 0 } expired)
                {
                    items.Add(new WorklistItem
                    {
                        Kind = WorklistKind.ExpiredReagentUse,
                        KindLabel = "Expired reagent",
                        Title = s.AccessionNumber,
                        Detail = string.Join(" ", expired.Select(e =>
                            $"{e.Run.PanelName} ({e.Run.DisplayLabel}) recorded {e.RecordedOn}, lot expired {e.Expiration}.")) +
                                 " Review before relying on these results.",
                        UrgencyLabel = "Review",
                        SortOrder = 1,
                        AccessionNumber = s.AccessionNumber,
                        TargetTab = "Analysis",
                    });
                }

                // A specimen that expired while the application was open is still active (and listed) until the next
                // start-up; grades saved on it after expiry are shown like expired-reagent results.
                if ((!s.HasFinalCall || !s.IsReviewed) && s.IsExpired &&
                    Services.ExpiredReagentCheck.CheckSpecimen(this, s) is { Count: > 0 } afterExpiry)
                {
                    items.Add(new WorklistItem
                    {
                        Kind = WorklistKind.ExpiredReagentUse,
                        KindLabel = "Graded after specimen expiry",
                        Title = s.AccessionNumber,
                        Detail = string.Join(" ", afterExpiry.Select(e =>
                            $"{e.Run.PanelName} ({e.Run.DisplayLabel}) recorded {e.RecordedOn}, specimen expired {e.Expiration}.")) +
                                 " Review before relying on these results, or test a new specimen.",
                        UrgencyLabel = "Review",
                        SortOrder = 1,
                        AccessionNumber = s.AccessionNumber,
                        TargetTab = "Analysis",
                    });
                }

                if (s.HasFinalCall && !s.IsReviewed && Services.AppSettings.Current.RequireSupervisorReview)
                {
                    items.Add(new WorklistItem
                    {
                        Kind = WorklistKind.AwaitingSupervisorReview,
                        KindLabel = "Supervisor review",
                        Title = s.AccessionNumber,
                        Detail = $"Confirmed {s.FinalAntibodies} by {s.IdentifiedBy ?? "—"} on {s.IdentifiedAt ?? "—"}; awaiting supervisor review." +
                                 (Services.ConfirmationOverrides.ForCurrentConfirmation(this, s.AccessionNumber) is { Count: > 0 } ov
                                     ? $" Confirmed past {ov.Count} warning(s): " + string.Join(" ", ov)
                                     : ""),
                        UrgencyLabel = "Needs review",
                        SortOrder = 3,
                        AccessionNumber = s.AccessionNumber,
                        TargetTab = "Specimens"
                    });
                }

                AddExpirationWorklistItem(items, s.ExpirationDate, today, todayStr, horizon,
                    isPanel: false, title: s.AccessionNumber, accession: s.AccessionNumber,
                    panelId: null, lotNumber: null);
            }

            foreach (var (panel, antigen, cells) in FindUnverifiedAllNegativeExtraAntigens())
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.ExtraAntigenReview,
                    KindLabel = "Check typing",
                    Title = panel.Name,
                    Detail = $"Lot {panel.LotNumber}: {antigen} is negative on all {cells} cells. Extra antigens added before " +
                             "this check was introduced were filled in as negative automatically. Compare with the antigram, then " +
                             "Mark typing verified, or Edit the panel to correct or set NT.",
                    UrgencyLabel = "Verify",
                    SortOrder = 1,
                    PanelId = panel.PanelId,
                    Antigen = antigen,
                    TargetTab = "Panels",
                });

            var allPanels = GetAllPanels();
            foreach (var p in allPanels.Where(p => p.IsActive))
            {
                var cells = GetPanelCells(p.PanelId);
                var untyped = Services.PanelIntegrityCheck.UntypedCells(cells);
                if (untyped.Count == 0) continue;
                var total = cells.Count(c => !string.Equals(c.CellNumber, "AC", StringComparison.OrdinalIgnoreCase));
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.UntypedPanel,
                    KindLabel = "Untyped cells",
                    Title = p.Name,
                    Detail = Services.PanelIntegrityCheck.DescribeUntyped(p, untyped, total) +
                             " Edit the panel typing from the vendor antigram.",
                    UrgencyLabel = "Enter typing",
                    SortOrder = 1,
                    PanelId = p.PanelId,
                    TargetTab = "Panels",
                });
            }

            foreach (var group in Services.PanelIntegrityCheck.DuplicateLotGroups(allPanels))
            {
                var first = group.FirstOrDefault(g => g.IsActive) ?? group[0];
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.DuplicateLot,
                    KindLabel = "Duplicate lot",
                    Title = $"Lot {first.LotNumber!.Trim()}",
                    Detail = $"{group.Count} panels share this lot: {Services.PanelIntegrityCheck.DescribeDuplicates(group)}. " +
                             "Results may be recorded against the wrong copy. Keep one active, deactivate or correct the others.",
                    UrgencyLabel = "Resolve",
                    SortOrder = 2,
                    PanelId = first.PanelId,
                    TargetTab = "Panels",
                });
            }

            foreach (var f in Services.DataConsistencyCheck.Find(GetReactionRecords()))
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.DataInconsistency,
                    KindLabel = "Data inconsistency",
                    Title = f.Title,
                    Detail = f.Detail + " " + f.Action,
                    UrgencyLabel = "Resolve",
                    SortOrder = 1,
                    AccessionNumber = f.AccessionNumber,
                    PanelId = f.PanelId,
                    TargetTab = f.TargetTab,
                });

            foreach (var (title, detail) in Services.RuleValidation.Problems(GetAllRules(), GetKnownAntigens()))
                items.Add(new WorklistItem
                {
                    Kind = WorklistKind.RuleConflict,
                    KindLabel = "Rule problem",
                    Title = title,
                    Detail = detail,
                    UrgencyLabel = "Fix rule",
                    SortOrder = 1,
                    TargetTab = "Rules",
                });

            foreach (var p in allPanels.Where(p => p.IsActive))
            {
                AddExpirationWorklistItem(items, p.ExpirationDate, today, todayStr, horizon,
                    isPanel: true, title: p.Name, accession: null,
                    panelId: p.PanelId, lotNumber: p.LotNumber);
            }

            return items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Title)
                .ToList();
        }

        /// <summary>
        /// Extra-antigen columns typed "-" on every cell (two or more) whose typing has no
        /// record of being entered or verified: no <c>add_extra_antigen</c> entry (columns
        /// added since that entry exists start as NT), no audited typing edit for the
        /// antigen, and no <c>verify_extra_antigen</c> entry. These are likely the old
        /// automatic negative default rather than real typing.
        /// </summary>
        public List<(Panel Panel, string Antigen, int Cells)> FindUnverifiedAllNegativeExtraAntigens()
        {
            var found = new List<(Panel, string, int)>();
            foreach (var panel in GetAllPanels())
            {
                var extras = GetPanelExtraAntigens(panel.PanelId);
                if (extras.Count == 0) continue;
                var cells = GetPanelCells(panel.PanelId)
                    .Where(c => !string.Equals(c.CellNumber, "AC", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (cells.Count < 2) continue;
                List<AuditEvent>? audit = null;
                foreach (var antigen in extras)
                {
                    if (!cells.All(c => c.Antigens.TryGetValue(antigen, out var v) && v == "-")) continue;
                    audit ??= GetAuditEventsForEntity("panel", panel.PanelId.ToString());
                    bool Mentions(AuditEvent e, string json) =>
                        json != null && json.Contains($"\"antigen\":\"{antigen}\"", StringComparison.Ordinal);
                    if (audit.Any(e => (e.Action is "add_extra_antigen" or "verify_extra_antigen") && Mentions(e, e.AfterJson ?? "")))
                        continue;
                    if (audit.Any(e => e.Action == "edit_panel_typing" && (e.AfterJson ?? "").Contains($" {antigen}: ", StringComparison.Ordinal)))
                        continue;
                    found.Add((panel, antigen, cells.Count));
                }
            }
            return found;
        }

        /// <summary>
        /// Records that a person checked an all-negative extra-antigen column against the
        /// antigram and it is correct. Needs the supervisor-review permission. Changes no
        /// typing; only the audit log and the Worklist.
        /// </summary>
        public void VerifyExtraAntigenTyping(int panelId, string antigen)
        {
            EnsureCanRecord();
            Services.AccessControl.Demand(this, Services.Permission.SupervisorReview);
            if (!GetPanelExtraAntigens(panelId).Contains(antigen))
                throw new ArgumentException($"{antigen} is not an extra antigen on panel #{panelId}.");
            AppendAudit("verify_extra_antigen", "panel", panelId.ToString(), null, null,
                JsonSerializer.Serialize(new { antigen, verified = "all cells negative" }));
        }

        private static void AddExpirationWorklistItem(
            List<WorklistItem> items, string? expirationDate, DateTime today,
            string todayStr, string horizon, bool isPanel, string title,
            string? accession, int? panelId, string? lotNumber)
        {
            if (string.IsNullOrEmpty(expirationDate)) return;

            bool expired = string.Compare(expirationDate, todayStr, StringComparison.Ordinal) < 0;
            bool dueSoon = !expired &&
                string.Compare(expirationDate, horizon, StringComparison.Ordinal) <= 0;
            if (!expired && !dueSoon) return;

            int days = 0;
            if (DateTime.TryParse(expirationDate, out var expDate))
                days = (expDate.Date - today).Days;

            if (expired)
            {
                var ago = days < 0 ? $" ({-days} day(s) ago)" : "";
                items.Add(new WorklistItem
                {
                    Kind = isPanel ? WorklistKind.ExpiredPanel : WorklistKind.ExpiredSpecimen,
                    KindLabel = isPanel ? "Expired panel" : "Expired specimen",
                    Title = title,
                    Detail = isPanel
                        ? $"Lot {lotNumber ?? "N/A"} expired {expirationDate}{ago}."
                        : $"Expired {expirationDate}{ago}.",
                    UrgencyLabel = "Expired",
                    SortOrder = 0,
                    AccessionNumber = accession,
                    PanelId = panelId,
                    TargetTab = isPanel ? "Panels" : "Specimens"
                });
                return;
            }

            string when = days == 0
                ? $"Expires today ({expirationDate})."
                : $"Expires {expirationDate} ({days} day(s)).";
            items.Add(new WorklistItem
            {
                Kind = isPanel ? WorklistKind.ExpiringPanel : WorklistKind.ExpiringSpecimen,
                KindLabel = isPanel ? "Expiring panel" : "Expiring specimen",
                Title = title,
                Detail = isPanel ? $"Lot {lotNumber ?? "N/A"} — {when}" : when,
                UrgencyLabel = days == 0 ? "Today" : $"{days}d",
                SortOrder = isPanel ? 4 : 3,
                AccessionNumber = accession,
                PanelId = panelId,
                TargetTab = isPanel ? "Panels" : "Specimens"
            });
        }

        private static bool HasEnteredGrade(Reaction rxn) =>
            IsEntered(rxn.IS) || IsEntered(rxn.C37) || IsEntered(rxn.AHG);

        private static bool IsEntered(string v) =>
            !string.IsNullOrEmpty(v) && v != "NT";

        // ── Rules ─────────────────────────────────────────────────────────────

        /// <summary>Standard and warehouse antigens plus every extra antigen added to a panel.</summary>
        public List<string> GetKnownAntigens()
        {
            var list = new List<string>(AntigenConstants.AllKnownAntigens);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT antigen_name FROM panel_extra_antigens";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var ag = r.GetString(0);
                if (!list.Contains(ag, StringComparer.Ordinal)) list.Add(ag);
            }
            return list;
        }

        public int AddRule(string name, string? description, string antibody,
            string? exceptionAntigen, bool heterozygousOk, int minRuleoutCount,
            bool separationExempt = false, string? separationExemptFrom = null)
        {
            Services.AccessControl.Demand(this, Services.Permission.ConfigureRules);
            antibody = Services.RuleValidation.NormalizeAntibody(antibody, GetKnownAntigens(), GetAllRules(), null);
            minRuleoutCount = Services.RuleValidation.RequireRuleoutCount(minRuleoutCount);
            separationExemptFrom = NormalizeSeparationList(separationExempt, separationExemptFrom);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO rules (name, description, antibody, exception_antigen,
                                   heterozygous_ok, min_ruleout_count, separation_exempt, separation_exempt_from)
                VALUES ($name, $desc, $ab, $exc, $hetok, $min, $sep, $sepfrom);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ab", antibody);
            cmd.Parameters.AddWithValue("$exc", (object?)exceptionAntigen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hetok", heterozygousOk ? 1 : 0);
            cmd.Parameters.AddWithValue("$min", minRuleoutCount);
            cmd.Parameters.AddWithValue("$sep", separationExempt ? 1 : 0);
            cmd.Parameters.AddWithValue("$sepfrom", (object?)separationExemptFrom ?? DBNull.Value);
            var id = Convert.ToInt32(cmd.ExecuteScalar());
            AppendAudit("add_rule", "rule", id.ToString(), null,
                afterJson: RuleAuditJson(GetRule(id)));
            return id;
        }

        public List<Rule> GetAllRules()
        {
            var list = new List<Rule>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM rules ORDER BY name COLLATE NOCASE, rule_id";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(ReadRule(r));
            return list;
        }

        public void UpdateRule(int ruleId, string name, string? description, string antibody,
            string? exceptionAntigen, bool heterozygousOk, int minRuleoutCount,
            bool separationExempt = false, string? separationExemptFrom = null)
        {
            Services.AccessControl.Demand(this, Services.Permission.ConfigureRules);
            antibody = Services.RuleValidation.NormalizeAntibody(antibody, GetKnownAntigens(), GetAllRules(), ruleId);
            minRuleoutCount = Services.RuleValidation.RequireRuleoutCount(minRuleoutCount);
            separationExemptFrom = NormalizeSeparationList(separationExempt, separationExemptFrom);
            var before = RuleAuditJson(GetRule(ruleId));
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE rules SET name=$name, description=$desc, antibody=$ab,
                exception_antigen=$exc, heterozygous_ok=$hetok, min_ruleout_count=$min,
                separation_exempt=$sep, separation_exempt_from=$sepfrom
                WHERE rule_id=$id";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ab", antibody);
            cmd.Parameters.AddWithValue("$exc", (object?)exceptionAntigen ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hetok", heterozygousOk ? 1 : 0);
            cmd.Parameters.AddWithValue("$min", minRuleoutCount);
            cmd.Parameters.AddWithValue("$sep", separationExempt ? 1 : 0);
            cmd.Parameters.AddWithValue("$sepfrom", (object?)separationExemptFrom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", ruleId);
            cmd.ExecuteNonQuery();
            var after = RuleAuditJson(GetRule(ruleId));
            if (before != after)
                AppendAudit("update_rule", "rule", ruleId.ToString(), null, before, after);
        }

        public Rule? GetRule(int ruleId) => GetAllRules().FirstOrDefault(r => r.RuleId == ruleId);

        /// <summary>Every field that affects analysis, so an edit or delete keeps the old values.</summary>
        private static string? RuleAuditJson(Rule? r) => r == null ? null : JsonSerializer.Serialize(new
        {
            name = r.Name, description = r.Description, antibody = r.Antibody,
            exceptionAntigen = r.ExceptionAntigen, heterozygousOk = r.HeterozygousOk,
            minRuleoutCount = r.MinRuleoutCount, separationExempt = r.SeparationExempt,
            separationExemptFrom = r.SeparationExemptFrom,
        });

        /// <summary>Audit actions that change how an analysis is computed.</summary>
        private const string ConfigActionsSql = "('add_rule', 'update_rule', 'delete_rule', 'update_settings')";

        /// <summary>
        /// The latest audited change to analysis rules or clinical Preferences: its audit id, which orders it
        /// against analyses whatever the clock did, and its local time ("yyyy-MM-ddTHH:mm:ss") for display.
        /// Null when there has been none.
        /// </summary>
        public Services.AnalysisConfigurationChange? LastAnalysisConfigurationChange()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT id, occurred_at_utc FROM audit_events WHERE action IN " + ConfigActionsSql +
                              " ORDER BY id DESC LIMIT 1";
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            var id = r.GetInt64(0);
            var v = r.IsDBNull(1) ? null : r.GetString(1);
            var local = !string.IsNullOrEmpty(v) &&
                        DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc)
                ? utc.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                : "";
            return new Services.AnalysisConfigurationChange(id, local);
        }

        private static string? NormalizeSeparationList(bool exempt, string? list)
        {
            if (!exempt) return null;
            var parts = Services.MultipleAntibodySeparation.ParseAntigenList(list);
            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        public void DeleteRule(int ruleId)
        {
            Services.AccessControl.Demand(this, Services.Permission.ConfigureRules);
            var before = RuleAuditJson(GetRule(ruleId));
            if (before == null) return;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM rules WHERE rule_id = $id";
            cmd.Parameters.AddWithValue("$id", ruleId);
            cmd.ExecuteNonQuery();
            AppendAudit("delete_rule", "rule", ruleId.ToString(), null, before, null);
        }

        // ── Search ────────────────────────────────────────────────────────────

        public List<(Panel panel, PanelCell cell)> SearchCellsByProfile(
            Dictionary<string, string> antigenCriteria,
            string? positiveZygosity = null)
        {
            var whereClauses = new List<string>();
            int param = 0;
            var parameters = new List<(string name, string value)>();
            var requireHomo = string.Equals(positiveZygosity, AntigenConstants.ZygosityHomozygous,
                StringComparison.OrdinalIgnoreCase);
            var requireHet = string.Equals(positiveZygosity, AntigenConstants.ZygosityHeterozygous,
                StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in antigenCriteria)
            {
                if (kvp.Value != "+" && kvp.Value != "-") continue;
                if (kvp.Value == "+")
                {
                    if (!TryAddAntigenPositiveClause(kvp.Key, whereClauses, parameters, ref param))
                        continue;
                }
                else if (!TryAddAntigenEqualsClause(kvp.Key, kvp.Value, whereClauses, parameters, ref param))
                {
                    continue;
                }
            }
            if (whereClauses.Count == 0) return new();

            var where = string.Join(" AND ", whereClauses);
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT p.*, pc.*
                FROM panel_cells pc
                JOIN panels p ON pc.panel_id = p.panel_id
                WHERE {where}
                ORDER BY p.name, pc.cell_number";
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);

            var results = new List<(Panel, PanelCell)>();
            var cells = new List<PanelCell>();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var panel = new Panel
                    {
                        PanelId = r.GetInt32(r.GetOrdinal("panel_id")),
                        Name = r.GetString(r.GetOrdinal("name")),
                        LotNumber = r.IsDBNull(r.GetOrdinal("lot_number")) ? null : r.GetString(r.GetOrdinal("lot_number")),
                        Vendor = r.IsDBNull(r.GetOrdinal("vendor")) ? null : r.GetString(r.GetOrdinal("vendor")),
                        // Search spans every stored lot; carry the lot's status so a result from an inactive or
                        // expired lot is never mistaken for a cell in current use.
                        IsActive = r.GetInt32(r.GetOrdinal("is_active")) != 0,
                        ExpirationDate = r.IsDBNull(r.GetOrdinal("expiration_date")) ? null : r.GetString(r.GetOrdinal("expiration_date")),
                    };
                    var cell = ReadPanelCell(r);
                    results.Add((panel, cell));
                    cells.Add(cell);
                }
            }
            AttachExtraAntigens(cells);
            if (!requireHomo && !requireHet) return results;
            return results
                .Where(item => MatchesRequestedZygosity(item.Item2, antigenCriteria, requireHomo))
                .ToList();
        }

        private static bool MatchesRequestedZygosity(
            PanelCell cell,
            Dictionary<string, string> antigenCriteria,
            bool requireHomo)
        {
            foreach (var kvp in antigenCriteria)
            {
                if (kvp.Value != "+") continue;
                if (!AntigenConstants.AntitheticalPairs.ContainsKey(kvp.Key))
                    continue;
                if (requireHomo)
                {
                    if (!cell.IsHomozygousFor(kvp.Key)) return false;
                }
                else if (!cell.HasTypedAntigen(kvp.Key) ||
                         !AntigenConstants.AntitheticalPairs.TryGetValue(kvp.Key, out var partner) ||
                         !cell.HasTypedAntigen(partner) ||
                         cell.GetAntigen(partner) != "+")
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryAddAntigenPositiveClause(
            string antigen,
            List<string> whereClauses,
            List<(string name, string value)> parameters,
            ref int param)
        {
            if (AntigenConstants.IsStandard(antigen))
            {
                var pPlus = $"$v{param++}";
                var pHomo = $"$v{param++}";
                parameters.Add((pPlus, "+"));
                parameters.Add((pHomo, AntigenConstants.AntigenHomozygous));
                var col = AntigenMapper.GetColumn(antigen);
                whereClauses.Add($"pc.{col} IN ({pPlus}, {pHomo})");
                return true;
            }

            if (AntigenConstants.IsWarehouse(antigen))
            {
                var pPlus = $"$v{param++}";
                var pHomo = $"$v{param++}";
                var agName = $"$ag{param++}";
                parameters.Add((pPlus, "+"));
                parameters.Add((pHomo, AntigenConstants.AntigenHomozygous));
                parameters.Add((agName, antigen));
                whereClauses.Add($@"
                        EXISTS (
                            SELECT 1 FROM panel_cell_extra_antigens x
                            WHERE x.cell_id = pc.id
                              AND x.antigen_name = {agName}
                              AND x.value IN ({pPlus}, {pHomo})
                        )");
                return true;
            }

            return false;
        }

        private static bool TryAddAntigenEqualsClause(
            string antigen,
            string value,
            List<string> whereClauses,
            List<(string name, string value)> parameters,
            ref int param)
        {
            if (AntigenConstants.IsStandard(antigen))
            {
                var pName = $"$v{param++}";
                parameters.Add((pName, value));
                var col = AntigenMapper.GetColumn(antigen);
                whereClauses.Add($"pc.{col} = {pName}");
                return true;
            }

            if (AntigenConstants.IsWarehouse(antigen))
            {
                var pName = $"$v{param++}";
                var agName = $"$ag{param++}";
                parameters.Add((pName, value));
                parameters.Add((agName, antigen));
                whereClauses.Add($@"
                        EXISTS (
                            SELECT 1 FROM panel_cell_extra_antigens x
                            WHERE x.cell_id = pc.id
                              AND x.antigen_name = {agName}
                              AND x.value = {pName}
                        )");
                return true;
            }

            return false;
        }

        // ── Readers ───────────────────────────────────────────────────────────

        private static Specimen ReadSpecimen(SqliteDataReader r) => new Specimen
        {
            AccessionNumber = r.GetString(r.GetOrdinal("accession_number")),
            Type = r.GetString(r.GetOrdinal("type")),
            ExpirationDate = r.IsDBNull(r.GetOrdinal("expiration_date")) ? null : r.GetString(r.GetOrdinal("expiration_date")),
            CreatedDate = r.GetString(r.GetOrdinal("created_date")),
            ReactionsUpdatedAt = SafeGetString(r, "reactions_updated_at"),
            LastAnalyzedAt = SafeGetString(r, "last_analyzed_at"),
            AnalysisConfigSeq = SafeGetNullableLong(r, "analysis_config_seq"),
            IsActive = SafeGetInt(r, "is_active", 1) != 0,
            Notes = SafeGetString(r, "notes"),
            Phenotype = SafeGetString(r, "phenotype"),
            Genotype = SafeGetString(r, "genotype"),
            PreviousAntibodies = SafeGetString(r, "previous_antibodies"),
            DatResult = SafeGetString(r, "dat_result"),
            TransfusionStatus = TransfusionStatus.Normalize(SafeGetString(r, "transfusion_status")),
            LastTransfusionDate = SafeGetString(r, "last_transfusion_date"),
            TransfusionProducts = SafeGetString(r, "transfusion_products"),
            TransfusionUnits = int.TryParse(SafeGetString(r, "transfusion_units"), out var units) ? units : null,
            DatIgg = SafeGetString(r, "dat_igg"),
            Mrn = SafeGetString(r, "mrn"),
            SourceAccession = SafeGetString(r, "source_accession"),
            CollectedAt = SafeGetString(r, "collected_at"),
            Collector = SafeGetString(r, "collector"),
            CollectionLocation = SafeGetString(r, "collection_location"),
            OrderingLocation = SafeGetString(r, "ordering_location"),
            OrderingProvider = SafeGetString(r, "ordering_provider"),
            EncounterNumber = SafeGetString(r, "encounter_number"),
            PregnancyStatus = PregnancyStatus.Normalize(SafeGetString(r, "pregnancy_status")),
            PregnancyEndDate = SafeGetString(r, "pregnancy_end_date"),
            RhigDate = SafeGetString(r, "rhig_date"),
            ReceivedAt = SafeGetString(r, "received_at"),
            DatC3 = SafeGetString(r, "dat_c3"),
            Hemolysis = Services.SpecimenHemolysis.Normalize(SafeGetString(r, "hemolysis")),
            FinalAntibodies = SafeGetString(r, "final_antibodies"),
            FinalComment = SafeGetString(r, "final_comment"),
            IdentifiedBy = SafeGetString(r, "identified_by"),
            IdentifiedAt = SafeGetString(r, "identified_at"),
            ReviewedBy = SafeGetString(r, "reviewed_by"),
            CancelledAt = SafeGetString(r, "cancelled_at"),
            CancelReason = SafeGetString(r, "cancel_reason"),
            ReviewedAt = SafeGetString(r, "reviewed_at"),
        };

        private static Panel ReadPanel(SqliteDataReader r) => new Panel
        {
            PanelId = r.GetInt32(r.GetOrdinal("panel_id")),
            Name = r.GetString(r.GetOrdinal("name")),
            LotNumber = r.IsDBNull(r.GetOrdinal("lot_number")) ? null : r.GetString(r.GetOrdinal("lot_number")),
            Vendor = r.IsDBNull(r.GetOrdinal("vendor")) ? null : r.GetString(r.GetOrdinal("vendor")),
            NumCells = r.GetInt32(r.GetOrdinal("num_cells")),
            StartCell = SafeGetInt(r, "start_cell", 1),
            ExpirationDate = r.IsDBNull(r.GetOrdinal("expiration_date")) ? null : r.GetString(r.GetOrdinal("expiration_date")),
            IncludeAc = r.GetInt32(r.GetOrdinal("include_ac")) != 0,
            IsActive = SafeGetInt(r, "is_active", 1) != 0,
            CatalogNumber = SafeGetString(r, "catalog_number"),
            ProductLine = SafeGetString(r, "product_line"),
            EnzymeTreated = SafeGetInt(r, "enzyme_treated") != 0,
            SourceUrl = SafeGetString(r, "source_url"),
            SourceFormat = SafeGetString(r, "source_format"),
            ImportedAt = SafeGetString(r, "imported_at"),
            SourceSha256 = SafeGetString(r, "source_sha256"),
            SourceArtifactPath = SafeGetString(r, "source_artifact_path"),
            SpecialNotes = SafeGetString(r, "special_notes"),
        };

        private static PanelCell ReadPanelCell(SqliteDataReader r)
        {
            var cell = new PanelCell
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                PanelId = r.GetInt32(r.GetOrdinal("panel_id")),
                CellNumber = r.GetString(r.GetOrdinal("cell_number")),
                DonorId = SafeGetString(r, "donor_id"),
                RhPhenotype = SafeGetString(r, "rh_phenotype"),
                SpecialTypes = SafeGetString(r, "special_types"),
            };
            foreach (var ag in AntigenConstants.Antigens)
            {
                var col = AntigenMapper.GetColumn(ag);
                var ordinal = r.GetOrdinal(col);
                if (r.IsDBNull(ordinal)) continue;
                var value = r.GetString(ordinal);
                if (AntigenConstants.IsTypedAntigenValue(value) ||
                    value == AntigenConstants.AntigenNotTested)
                    cell.Antigens[ag] = value;
            }
            return cell;
        }

        private static PanelRun ReadPanelRun(SqliteDataReader r)
        {
            var run = new PanelRun
            {
                RunId = r.GetInt32(r.GetOrdinal("run_id")),
                SpecimenId = r.GetString(r.GetOrdinal("specimen_id")),
                PanelId = r.GetInt32(r.GetOrdinal("panel_id")),
                Label = SafeGetString(r, "label") ?? string.Empty,
                CreatedDate = SafeGetString(r, "created_date") ?? string.Empty,
                PanelName = SafeGetString(r, "panel_name") ?? string.Empty,
            };
            var ctStr = SafeGetString(r, "cell_treatment") ?? "None";
            var stStr = SafeGetString(r, "serum_treatment") ?? "None";
            run.CellTreatment = Enum.TryParse<CellTreatment>(ctStr, out var ct) ? ct : CellTreatment.None;
            run.SerumTreatment = Enum.TryParse<SerumTreatment>(stStr, out var st) ? st : SerumTreatment.None;
            return run;
        }

        /// <summary>
        /// Reads a reaction from a result set that includes denormalized panel_run columns
        /// (specimen_id, panel_id, cell_treatment, serum_treatment).
        /// </summary>
        private static Reaction ReadReactionFull(SqliteDataReader r)
        {
            var rxn = new Reaction
            {
                ReactionId = r.GetInt32(r.GetOrdinal("reaction_id")),
                RunId = r.GetInt32(r.GetOrdinal("run_id")),
                CellNumber = r.GetString(r.GetOrdinal("cell_number")),
                IS = SafeGetString(r, "IS") ?? "NT",
                C37 = SafeGetString(r, "C37") ?? "NT",
                AHG = SafeGetString(r, "AHG") ?? "NT",
                CC = SafeGetString(r, "CC") ?? "NT",
                ExtraPhases = ExtraPhaseParser.Deserialize(SafeGetString(r, "extra_phases")),
                SpecimenId = SafeGetString(r, "specimen_id") ?? string.Empty,
                PanelId = SafeGetInt(r, "panel_id"),
            };
            var ctStr = SafeGetString(r, "cell_treatment") ?? "None";
            var stStr = SafeGetString(r, "serum_treatment") ?? "None";
            rxn.CellTreatment = Enum.TryParse<CellTreatment>(ctStr, out var ct) ? ct : CellTreatment.None;
            rxn.SerumTreatment = Enum.TryParse<SerumTreatment>(stStr, out var st) ? st : SerumTreatment.None;
            return rxn;
        }

        private static Rule ReadRule(SqliteDataReader r) => new Rule
        {
            RuleId = r.GetInt32(r.GetOrdinal("rule_id")),
            Name = r.GetString(r.GetOrdinal("name")),
            Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString(r.GetOrdinal("description")),
            Antibody = r.GetString(r.GetOrdinal("antibody")),
            ExceptionAntigen = r.IsDBNull(r.GetOrdinal("exception_antigen")) ? null : r.GetString(r.GetOrdinal("exception_antigen")),
            HeterozygousOk = r.GetInt32(r.GetOrdinal("heterozygous_ok")) != 0,
            MinRuleoutCount = r.GetInt32(r.GetOrdinal("min_ruleout_count")),
            SeparationExempt = r.GetInt32(r.GetOrdinal("separation_exempt")) != 0,
            SeparationExemptFrom = r.IsDBNull(r.GetOrdinal("separation_exempt_from")) ? null : r.GetString(r.GetOrdinal("separation_exempt_from")),
        };

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ExecNonQuery(string sql)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static IEnumerable<string> GetAllAntigenColumns(Func<string, string> transform)
        {
            foreach (var ag in AntigenConstants.Antigens)
                yield return transform(AntigenMapper.GetColumn(ag));
        }

        private static string? SafeGetString(SqliteDataReader r, string col)
        {
            try
            {
                var ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? null : r.GetString(ord);
            }
            catch { return null; }
        }

        private static long? SafeGetNullableLong(SqliteDataReader r, string col)
        {
            try
            {
                var ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? null : r.GetInt64(ord);
            }
            catch { return null; }
        }

        private static int SafeGetInt(SqliteDataReader r, string col, int defaultValue = 0)
        {
            try
            {
                var ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? defaultValue : r.GetInt32(ord);
            }
            catch { return defaultValue; }
        }

        // ── Capacity / purge ──────────────────────────────────────────────────

        public static string CutoffForKeepDays(int days) =>
            DateTime.Today.AddDays(-Math.Max(0, days)).ToString("yyyy-MM-dd");

        public long GetFileSizeBytes()
        {
            try
            {
                var info = new FileInfo(DbPath);
                info.Refresh();
                return info.Exists ? info.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        public DatabaseCapacityStatus GetCapacityStatus(long maxBytes)
        {
            if (maxBytes <= 0) maxBytes = 1;
            var fileBytes = GetFileSizeBytes();
            var percent = fileBytes * 100.0 / maxBytes;
            return new DatabaseCapacityStatus
            {
                FileBytes = fileBytes,
                MaxBytes = maxBytes,
                PercentUsed = percent,
                IsNearCapacity = percent >= DatabaseCapacityStatus.WarningPercent
            };
        }

        public void ApplyMaxPageCount(long maxBytes)
        {
            if (maxBytes <= 0) return;
            var pageSize = PragmaLong("page_size");
            if (pageSize <= 0) return;
            var pageCount = PragmaLong("page_count");
            var maxPages = maxBytes / pageSize;
            if (maxPages < pageCount) maxPages = pageCount;
            if (maxPages < 1) maxPages = 1;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA max_page_count = {maxPages};";
            cmd.ExecuteNonQuery();
        }

        public int CountSpecimensCreatedBefore(string cutoffDate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM specimens WHERE created_date < $cutoff";
            cmd.Parameters.AddWithValue("$cutoff", cutoffDate);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int CountConfirmedSpecimensCreatedBefore(string cutoffDate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(*) FROM specimens
                WHERE created_date < $cutoff AND final_antibodies IS NOT NULL AND trim(final_antibodies) <> ''";
            cmd.Parameters.AddWithValue("$cutoff", cutoffDate);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public PurgeResult PurgeSpecimensCreatedBefore(string cutoffDate, string? archivePath = null)
        {
            if (string.IsNullOrWhiteSpace(cutoffDate))
                throw new ArgumentException("Cutoff date is required.", nameof(cutoffDate));

            Services.AccessControl.Demand(this, Services.Permission.PurgeData);
            var count = CountSpecimensCreatedBefore(cutoffDate);
            if (count == 0)
            {
                return new PurgeResult
                {
                    SpecimensDeleted = 0,
                    ArchivePath = null,
                    FileSizeBytesAfter = GetFileSizeBytes()
                };
            }

            if (string.IsNullOrWhiteSpace(archivePath))
            {
                var confirmed = CountConfirmedSpecimensCreatedBefore(cutoffDate);
                if (confirmed > 0)
                    throw new InvalidOperationException(
                        $"{confirmed} of the {count} specimen(s) created before {cutoffDate} have a confirmed identification. " +
                        "Choose an archive file so that history is kept; nothing was purged.");
            }

            string? createdArchive = null;
            if (!string.IsNullOrWhiteSpace(archivePath))
            {
                createdArchive = Path.GetFullPath(archivePath);
                if (string.Equals(createdArchive, Path.GetFullPath(DbPath), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Archive path cannot be the live database file.");
                RequireNewArchiveFile(createdArchive);
                CreateSpecimenArchive(cutoffDate, createdArchive);
            }

            RunInTransaction(() =>
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM specimens WHERE created_date < $cutoff";
                    cmd.Parameters.AddWithValue("$cutoff", cutoffDate);
                    cmd.ExecuteNonQuery();
                }
                AppendAudit("purge", "database", cutoffDate, createdArchive,
                    afterJson: JsonSerializer.Serialize(new { count, archive = createdArchive }));
            });
            Vacuum();

            return new PurgeResult
            {
                SpecimensDeleted = count,
                ArchivePath = createdArchive,
                FileSizeBytesAfter = GetFileSizeBytes()
            };
        }

        public ArchiveInspection InspectArchive(string archivePath)
        {
            var path = ResolveArchivePath(archivePath);
            var liveAccessions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in GetAllSpecimens())
                liveAccessions.Add(s.AccessionNumber);

            using var conn = OpenArchiveReadOnly(path);
            EnsureArchiveSpecimensTable(conn);

            var specimenCols = ArchiveColumnNames(conn, "specimens");
            var select = new List<string> { "accession_number", "type", "created_date" };
            foreach (var optional in new[] { "final_antibodies", "dat_result", "notes" })
            {
                if (specimenCols.Contains(optional))
                    select.Add(optional);
            }
            var specimens = new List<ArchiveSpecimenRow>();
            var antibodies = ArchiveAntibodiesBySpecimen(conn);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT {string.Join(", ", select)}
                    FROM specimens
                    ORDER BY created_date, accession_number";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var acc = reader.GetString(0);
                    string? finalId = null;
                    string? dat = null;
                    string? notes = null;
                    for (var i = 3; i < select.Count; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetString(i);
                        switch (select[i])
                        {
                            case "final_antibodies": finalId = value; break;
                            case "dat_result": dat = value; break;
                            case "notes": notes = value; break;
                        }
                    }
                    antibodies.TryGetValue(acc, out var listed);
                    specimens.Add(new ArchiveSpecimenRow
                    {
                        AccessionNumber = acc,
                        Type = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        CreatedDate = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        ExistsInLive = liveAccessions.Contains(acc),
                        Recorded = ArchiveSpecimenRow.FormatRecorded(finalId, dat, notes, listed)
                    });
                }
            }

            var panelCount = 0;
            if (TableExists(conn, "panels"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM panels";
                panelCount = Convert.ToInt32(cmd.ExecuteScalar());
            }

            string? earliest = null;
            string? latest = null;
            if (specimens.Count > 0)
            {
                earliest = specimens.Min(s => s.CreatedDate);
                latest = specimens.Max(s => s.CreatedDate);
            }

            var info = new FileInfo(path);
            return new ArchiveInspection
            {
                Path = path,
                FileBytes = info.Exists ? info.Length : 0,
                SpecimenCount = specimens.Count,
                PanelCount = panelCount,
                EarliestCreatedDate = earliest,
                LatestCreatedDate = latest,
                AlreadyInLiveCount = specimens.Count(s => s.ExistsInLive),
                Specimens = specimens
            };
        }

        public RestoreResult RestoreArchive(string archivePath)
        {
            EnsureCanRecord();
            var path = ResolveArchivePath(archivePath);
            using (var probe = OpenArchiveReadOnly(path))
                EnsureArchiveSpecimensTable(probe);

            var skip = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in GetAllSpecimens())
                skip.Add(s.AccessionNumber);

            var panelsBefore = CountRows("panels");
            var panelIdsBefore = GetAllPanels().Select(p => p.PanelId).ToHashSet();
            var archiveSpecimenCount = 0;
            var restored = 0;
            var restoredAccessions = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                AttachArchive(path);
                FillRestoreSkipTable(skip);

                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM archive.specimens";
                    archiveSpecimenCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT accession_number FROM archive.specimens
                                        WHERE accession_number NOT IN (SELECT accession_number FROM restore_skip)";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) restoredAccessions.Add(r.GetString(0));
                }

                FillRestoreSkipPanels();
                CopyFromArchive("panels");
                CopyFromArchive("panel_cells");
                if (AttachedTableExists("panel_antigen_order"))
                    CopyFromArchive(
                        "panel_antigen_order",
                        "panel_id NOT IN (SELECT panel_id FROM restore_skip_panels)");
                if (AttachedTableExists("panel_extra_antigens"))
                    CopyFromArchive("panel_extra_antigens", "panel_id IN (SELECT panel_id FROM panels)");
                if (AttachedTableExists("panel_cell_extra_antigens"))
                    CopyFromArchive("panel_cell_extra_antigens", "cell_id IN (SELECT id FROM panel_cells)");

                restored = CopyFromArchive(
                    "specimens",
                    "accession_number NOT IN (SELECT accession_number FROM restore_skip)");
                CopyFromArchive(
                    "specimen_antibodies",
                    "specimen_id NOT IN (SELECT accession_number FROM restore_skip)");
                CopyFromArchive(
                    "specimen_ruleouts",
                    "specimen_id NOT IN (SELECT accession_number FROM restore_skip)");
                CopyFromArchive(
                    "specimen_panels",
                    "specimen_id NOT IN (SELECT accession_number FROM restore_skip)");
                CopyFromArchive(
                    "panel_runs",
                    "specimen_id NOT IN (SELECT accession_number FROM restore_skip)");
                CopyFromArchive(
                    "reactions",
                    "run_id IN (SELECT run_id FROM panel_runs)");
                if (AttachedTableExists("analysis_snapshots"))
                    CopyFromArchive(
                        "analysis_snapshots",
                        "specimen_id NOT IN (SELECT accession_number FROM restore_skip)");
            }
            finally
            {
                try { ExecNonQuery("DROP TABLE IF EXISTS restore_skip"); } catch { /* ignore */ }
                try { ExecNonQuery("DROP TABLE IF EXISTS restore_skip_panels"); } catch { /* ignore */ }
                try { DetachArchive(); } catch { /* ignore if not attached */ }
            }

            BumpSequence("panels", "panel_id");
            BumpSequence("panel_cells", "id");
            BumpSequence("specimen_antibodies", "id");
            BumpSequence("specimen_ruleouts", "id");
            BumpSequence("specimen_panels", "id");
            BumpSequence("panel_runs", "run_id");
            BumpSequence("reactions", "reaction_id");
            BumpSequence("analysis_snapshots", "id");

            var findings = RestoreReviewFindings(restoredAccessions, panelIdsBefore);
            AppendAudit("restore", "archive", path, null,
                afterJson: JsonSerializer.Serialize(new { restored, skipped = archiveSpecimenCount - restored, reviewFindings = findings }));

            return new RestoreResult
            {
                SpecimensRestored = restored,
                SpecimensSkipped = Math.Max(0, archiveSpecimenCount - restored),
                PanelsRestored = Math.Max(0, CountRows("panels") - panelsBefore),
                FileSizeBytesAfter = GetFileSizeBytes(),
                ReviewFindings = findings,
            };
        }

        /// <summary>
        /// Stored typing values that are not +, -, ++ or NT, read from the raw columns: the normal cell reader
        /// drops them, so they would otherwise look untyped. "cell 2 Jka \"0\"" per value.
        /// </summary>
        private List<string> UnreadableTyping(int panelId)
        {
            static bool Readable(string? v) =>
                string.IsNullOrWhiteSpace(v) || AntigenConstants.IsTypedAntigenValue(v) || v == AntigenConstants.AntigenNotTested;
            var bad = new List<string>();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM panel_cells WHERE panel_id = $pid ORDER BY id";
                cmd.Parameters.AddWithValue("$pid", panelId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var cell = r.GetString(r.GetOrdinal("cell_number"));
                    foreach (var ag in AntigenConstants.Antigens)
                    {
                        var ordinal = r.GetOrdinal(AntigenMapper.GetColumn(ag));
                        var v = r.IsDBNull(ordinal) ? null : r.GetString(ordinal);
                        if (!Readable(v)) bad.Add($"cell {cell} {ag} \"{v}\"");
                    }
                }
            }
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT pc.cell_number, x.antigen_name, x.value FROM panel_cell_extra_antigens x
                                    JOIN panel_cells pc ON pc.id = x.cell_id WHERE pc.panel_id = $pid ORDER BY pc.id";
                cmd.Parameters.AddWithValue("$pid", panelId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var v = r.IsDBNull(2) ? null : r.GetString(2);
                    if (!Readable(v)) bad.Add($"cell {r.GetString(0)} {r.GetString(1)} \"{v}\"");
                }
            }
            return bad;
        }

        /// <summary>
        /// Restore copies archived rows as stored, so it can bring back values that the save checks now refuse.
        /// Lists them for the restored specimens and newly restored panels; nothing is changed.
        /// </summary>
        private List<string> RestoreReviewFindings(ISet<string> restoredAccessions, ISet<int> panelIdsBefore)
        {
            var findings = Services.DataConsistencyCheck.Find(
                    GetReactionRecords().Where(r => restoredAccessions.Contains(r.SpecimenId)))
                .Select(f => $"{f.Title}: {f.Detail} {f.Action}")
                .ToList();
            foreach (var panel in GetAllPanels().Where(p => !panelIdsBefore.Contains(p.PanelId)))
            {
                var bad = UnreadableTyping(panel.PanelId);
                if (bad.Count == 0) continue;
                findings.Add($"{PanelAuditName(panel.PanelId) ?? panel.Name}: unreadable typing {string.Join("; ", bad.Take(5))}" +
                             (bad.Count > 5 ? $"; and {bad.Count - 5} more" : "") +
                             ". Analysis reads these as not typed. Correct them with Edit on the Panels tab.");
            }
            return findings;
        }

        private string ResolveArchivePath(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath))
                throw new ArgumentException("Archive path is required.", nameof(archivePath));
            var path = Path.GetFullPath(archivePath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Archive file not found.", path);
            if (string.Equals(path, Path.GetFullPath(DbPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("That file is the live database, not an archive.");
            return path;
        }

        private static SqliteConnection OpenArchiveReadOnly(string path)
        {
            var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            try
            {
                conn.Open();
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }

        private static void EnsureArchiveSpecimensTable(SqliteConnection conn)
        {
            if (!TableExists(conn, "specimens"))
                throw new InvalidOperationException("This file is not a valid HemoIQ archive.");
        }

        private static HashSet<string> ArchiveColumnNames(SqliteConnection conn, string table)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                names.Add(reader.GetString(1));
            return names;
        }

        private static Dictionary<string, List<string>> ArchiveAntibodiesBySpecimen(SqliteConnection conn)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (!TableExists(conn, "specimen_antibodies"))
                return map;
            var columns = ArchiveColumnNames(conn, "specimen_antibodies");
            if (!columns.Contains("antibody"))
                return map;
            var order = columns.Contains("probability")
                ? "specimen_id, probability DESC, antibody"
                : "specimen_id, antibody";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT specimen_id, antibody
                FROM specimen_antibodies
                ORDER BY {order}";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                var id = reader.GetString(0);
                if (!map.TryGetValue(id, out var list))
                {
                    list = new List<string>();
                    map[id] = list;
                }
                list.Add(reader.GetString(1));
            }
            return map;
        }

        private static bool TableExists(SqliteConnection conn, string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", table);
            return cmd.ExecuteScalar() != null;
        }

        private bool AttachedTableExists(string table)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM archive.sqlite_master WHERE type = 'table' AND name = $n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", table);
            return cmd.ExecuteScalar() != null;
        }

        private int CountRows(string table)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private int CopyFromArchive(string table, string? extraWhere = null)
        {
            var liveCols = GetColumnNamesOrdered(table);
            var archiveCols = new HashSet<string>(GetAttachedColumnNamesOrdered(table), StringComparer.OrdinalIgnoreCase);
            var cols = liveCols.Where(archiveCols.Contains).ToList();
            if (cols.Count == 0) return 0;
            var quoted = string.Join(", ", cols.Select(QuoteIdent));
            var sql = $"INSERT OR IGNORE INTO {table} ({quoted}) SELECT {quoted} FROM archive.{table}";
            if (!string.IsNullOrEmpty(extraWhere))
                sql += " WHERE " + extraWhere;
            return ExecNonQueryCount(sql);
        }

        private List<string> GetColumnNamesOrdered(string table)
        {
            var list = new List<string>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(1));
            return list;
        }

        private List<string> GetAttachedColumnNamesOrdered(string table)
        {
            var list = new List<string>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA archive.table_info({table})";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(1));
            return list;
        }

        private static string QuoteIdent(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

        private void FillRestoreSkipPanels()
        {
            ExecNonQuery("DROP TABLE IF EXISTS restore_skip_panels");
            ExecNonQuery("CREATE TEMP TABLE restore_skip_panels (panel_id INTEGER PRIMARY KEY)");
            ExecNonQuery("INSERT INTO restore_skip_panels (panel_id) SELECT panel_id FROM panels");
        }

        private void FillRestoreSkipTable(HashSet<string> skip)
        {
            ExecNonQuery("DROP TABLE IF EXISTS restore_skip");
            ExecNonQuery("CREATE TEMP TABLE restore_skip (accession_number TEXT PRIMARY KEY)");
            foreach (var acc in skip)
            {
                using var cmd = _conn.CreateCommand();
                cmd.CommandText = "INSERT INTO restore_skip (accession_number) VALUES ($a)";
                cmd.Parameters.AddWithValue("$a", acc);
                cmd.ExecuteNonQuery();
            }
        }

        private int ExecNonQueryCount(string sql)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        private void BumpSequence(string table, string idColumn)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO sqlite_sequence(name, seq)
                SELECT '{table}', COALESCE(MAX({idColumn}), 0) FROM {table}
                WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = '{table}');
                UPDATE sqlite_sequence
                SET seq = MAX(seq, (SELECT COALESCE(MAX({idColumn}), 0) FROM {table}))
                WHERE name = '{table}';";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// A purge archive is always a new file. An existing file may be an earlier archive whose specimens are
        /// no longer in the live database; replacing it would destroy that history.
        /// </summary>
        private static void RequireNewArchiveFile(string archivePath)
        {
            if (File.Exists(archivePath))
                throw new InvalidOperationException(
                    $"The archive file {Path.GetFileName(archivePath)} already exists. Choose a new file name: replacing it " +
                    "would delete any specimens archived there earlier. Nothing was purged.");
        }

        private void CreateSpecimenArchive(string cutoffDate, string archivePath)
        {
            var dir = Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            RequireNewArchiveFile(archivePath);

            using (new DatabaseService(archivePath))
            {
                // Creates the same schema, then releases the file for ATTACH.
            }

            try
            {
                AttachArchive(archivePath);
                CopyArchiveTable(@"
                    INSERT INTO archive.panels
                    SELECT * FROM panels
                    WHERE panel_id IN (
                        SELECT panel_id FROM specimen_panels
                        WHERE specimen_id IN (SELECT accession_number FROM specimens WHERE created_date < $cutoff)
                        UNION
                        SELECT panel_id FROM panel_runs
                        WHERE specimen_id IN (SELECT accession_number FROM specimens WHERE created_date < $cutoff)
                    )", cutoffDate);
                ExecNonQuery(@"
                    INSERT INTO archive.panel_cells
                    SELECT * FROM panel_cells
                    WHERE panel_id IN (SELECT panel_id FROM archive.panels)");
                ExecNonQuery(@"
                    INSERT INTO archive.panel_antigen_order
                    SELECT * FROM panel_antigen_order
                    WHERE panel_id IN (SELECT panel_id FROM archive.panels)");
                ExecNonQuery(@"
                    INSERT INTO archive.panel_extra_antigens
                    SELECT * FROM panel_extra_antigens
                    WHERE panel_id IN (SELECT panel_id FROM archive.panels)");
                ExecNonQuery(@"
                    INSERT INTO archive.panel_cell_extra_antigens
                    SELECT * FROM panel_cell_extra_antigens
                    WHERE cell_id IN (SELECT id FROM archive.panel_cells)");
                CopyArchiveTable(
                    "INSERT INTO archive.specimens SELECT * FROM specimens WHERE created_date < $cutoff",
                    cutoffDate);
                ExecNonQuery(@"
                    INSERT INTO archive.specimen_antibodies
                    SELECT * FROM specimen_antibodies
                    WHERE specimen_id IN (SELECT accession_number FROM archive.specimens)");
                ExecNonQuery(@"
                    INSERT INTO archive.specimen_ruleouts
                    SELECT * FROM specimen_ruleouts
                    WHERE specimen_id IN (SELECT accession_number FROM archive.specimens)");
                ExecNonQuery(@"
                    INSERT INTO archive.specimen_panels
                    SELECT * FROM specimen_panels
                    WHERE specimen_id IN (SELECT accession_number FROM archive.specimens)");
                ExecNonQuery(@"
                    INSERT INTO archive.panel_runs
                    SELECT * FROM panel_runs
                    WHERE specimen_id IN (SELECT accession_number FROM archive.specimens)");
                ExecNonQuery(@"
                    INSERT INTO archive.reactions
                    SELECT * FROM reactions
                    WHERE run_id IN (SELECT run_id FROM archive.panel_runs)");
                ExecNonQuery(@"
                    INSERT INTO archive.analysis_snapshots
                    SELECT * FROM analysis_snapshots
                    WHERE specimen_id IN (SELECT accession_number FROM archive.specimens)");
            }
            finally
            {
                DetachArchive();
            }
        }

        private void CopyArchiveTable(string sql, string cutoffDate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$cutoff", cutoffDate);
            cmd.ExecuteNonQuery();
        }

        private void AttachArchive(string path)
        {
            var escaped = path.Replace("'", "''");
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"ATTACH DATABASE '{escaped}' AS archive";
            cmd.ExecuteNonQuery();
        }

        private void DetachArchive()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DETACH DATABASE archive";
            cmd.ExecuteNonQuery();
        }

        private void Vacuum()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "VACUUM";
            cmd.ExecuteNonQuery();
        }

        private long PragmaLong(string name)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"PRAGMA {name};";
            var value = cmd.ExecuteScalar();
            return value == null || value is DBNull ? 0 : Convert.ToInt64(value);
        }

        public bool IsSpecimenLocked(string specimenId)
        {
            var specimen = GetSpecimen(specimenId);
            return specimen?.HasFinalCall == true || specimen?.IsCancelled == true;
        }

        public void EnsureSpecimenUnlocked(string specimenId)
        {
            var specimen = GetSpecimen(specimenId);
            if (specimen?.IsCancelled == true)
                throw LockedError(specimenId, cancelled: true);
            if (specimen?.HasFinalCall == true)
                throw LockedError(specimenId, cancelled: false);
        }

        /// <summary>The locked-record error, naming who can unlock it when the current user cannot.</summary>
        private RecordLockedException LockedError(string accessionNumber, bool cancelled)
        {
            var unlock = cancelled ? Permission.ReinstateInvestigation : Permission.ClearIdentification;
            return new RecordLockedException(accessionNumber, cancelled,
                AccessControl.Can(this, unlock) ? null : AccessControl.WhoCan(unlock));
        }

        private int _savepointDepth;

        /// <summary>
        /// Runs <paramref name="body"/> as one unit: every write it makes, audit entries
        /// included, is kept only if it returns normally, and is rolled back if it throws.
        /// Uses SQL savepoints, so existing methods need no transaction object and calls
        /// may nest. The exception is rethrown after the rollback.
        /// </summary>
        public void RunInTransaction(Action body)
        {
            var name = $"sp_{++_savepointDepth}";
            try
            {
                ExecNonQuery($"SAVEPOINT {name}");
                try
                {
                    body();
                }
                catch
                {
                    ExecNonQuery($"ROLLBACK TO {name}");
                    ExecNonQuery($"RELEASE {name}");
                    throw;
                }
                ExecNonQuery($"RELEASE {name}");
            }
            finally
            {
                _savepointDepth--;
            }
        }

        /// <summary><see cref="RunInTransaction(Action)"/> for work that returns a value.</summary>
        public T RunInTransaction<T>(Func<T> body)
        {
            T result = default!;
            RunInTransaction(() => { result = body(); });
            return result;
        }

        public void AppendAudit(string action, string? entityType, string? entityId, string? reason,
            string? beforeJson = null, string? afterJson = null)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO audit_events
                    (occurred_at_utc, operator, action, entity_type, entity_id, reason, before_json, after_json)
                VALUES ($at, $op, $action, $type, $id, $reason, $before, $after)";
            cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$op", Services.AccessControl.CurrentLogin ?? "");
            cmd.Parameters.AddWithValue("$action", action);
            cmd.Parameters.AddWithValue("$type", (object?)entityType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", (object?)entityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$reason", (object?)reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$before", (object?)beforeJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$after", (object?)afterJson ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Audit events for one entity, oldest first.</summary>
        /// <summary>Rule and clinical Preferences changes, oldest first.</summary>
        public List<AuditEvent> GetAnalysisConfigurationEvents() =>
            QueryAuditEvents("action IN ('add_rule', 'update_rule', 'delete_rule', 'update_settings')", _ => { });

        public List<AuditEvent> GetAuditEventsForEntity(string entityType, string entityId) =>
            QueryAuditEvents("entity_type = $type AND entity_id = $id", cmd =>
            {
                cmd.Parameters.AddWithValue("$type", entityType);
                cmd.Parameters.AddWithValue("$id", entityId);
            });

        private List<AuditEvent> QueryAuditEvents(string where, Action<SqliteCommand> bind)
        {
            var list = new List<AuditEvent>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT id, occurred_at_utc, operator, action, entity_type, entity_id, reason, before_json, after_json
                FROM audit_events
                WHERE {where}
                ORDER BY id";
            bind(cmd);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AuditEvent
                {
                    Id = r.GetInt64(0),
                    OccurredAtUtc = r.GetString(1),
                    Operator = r.GetString(2),
                    Action = r.GetString(3),
                    EntityType = r.IsDBNull(4) ? null : r.GetString(4),
                    EntityId = r.IsDBNull(5) ? null : r.GetString(5),
                    Reason = r.IsDBNull(6) ? null : r.GetString(6),
                    BeforeJson = r.IsDBNull(7) ? null : r.GetString(7),
                    AfterJson = r.IsDBNull(8) ? null : r.GetString(8),
                });
            return list;
        }

        /// <summary>Audit events with any of the given actions, oldest first.</summary>
        public List<AuditEvent> GetAuditEventsByAction(params string[] actions)
        {
            var list = new List<AuditEvent>();
            if (actions.Length == 0) return list;
            using var cmd = _conn.CreateCommand();
            var names = actions.Select((a, i) => "$a" + i).ToList();
            cmd.CommandText = $@"
                SELECT id, occurred_at_utc, operator, action, entity_type, entity_id, reason, before_json, after_json
                FROM audit_events WHERE action IN ({string.Join(", ", names)}) ORDER BY id";
            for (int i = 0; i < actions.Length; i++) cmd.Parameters.AddWithValue(names[i], actions[i]);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AuditEvent
                {
                    Id = r.GetInt64(0),
                    OccurredAtUtc = r.GetString(1),
                    Operator = r.GetString(2),
                    Action = r.GetString(3),
                    EntityType = r.IsDBNull(4) ? null : r.GetString(4),
                    EntityId = r.IsDBNull(5) ? null : r.GetString(5),
                    Reason = r.IsDBNull(6) ? null : r.GetString(6),
                    BeforeJson = r.IsDBNull(7) ? null : r.GetString(7),
                    AfterJson = r.IsDBNull(8) ? null : r.GetString(8),
                });
            return list;
        }

        public int CountAuditEvents()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM audit_events";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Every audit event, newest first, read one at a time so a long log is never held in memory whole. Used by
        /// the audit-log export, which must contain the whole log, not only the events the viewer loaded.
        /// </summary>
        public IEnumerable<AuditEvent> ReadAllAuditEvents()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, occurred_at_utc, operator, action, entity_type, entity_id, reason, before_json, after_json
                FROM audit_events
                ORDER BY id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                yield return new AuditEvent
                {
                    Id = r.GetInt64(0),
                    OccurredAtUtc = r.GetString(1),
                    Operator = r.GetString(2),
                    Action = r.GetString(3),
                    EntityType = r.IsDBNull(4) ? null : r.GetString(4),
                    EntityId = r.IsDBNull(5) ? null : r.GetString(5),
                    Reason = r.IsDBNull(6) ? null : r.GetString(6),
                    BeforeJson = r.IsDBNull(7) ? null : r.GetString(7),
                    AfterJson = r.IsDBNull(8) ? null : r.GetString(8),
                };
        }

        public List<AuditEvent> GetAuditEvents(int limit = 500)
        {
            var list = new List<AuditEvent>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, occurred_at_utc, operator, action, entity_type, entity_id, reason, before_json, after_json
                FROM audit_events
                ORDER BY id DESC
                LIMIT $limit";
            cmd.Parameters.AddWithValue("$limit", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AuditEvent
                {
                    Id = r.GetInt64(0),
                    OccurredAtUtc = r.GetString(1),
                    Operator = r.GetString(2),
                    Action = r.GetString(3),
                    EntityType = r.IsDBNull(4) ? null : r.GetString(4),
                    EntityId = r.IsDBNull(5) ? null : r.GetString(5),
                    Reason = r.IsDBNull(6) ? null : r.GetString(6),
                    BeforeJson = r.IsDBNull(7) ? null : r.GetString(7),
                    AfterJson = r.IsDBNull(8) ? null : r.GetString(8)
                });
            }
            return list;
        }

        public void SaveAnalysisSnapshot(string specimenId, string softwareVersion, string settingsJson,
            string inputFingerprint, string? ruledOutJson, string? suspectedJson, string? acsJson,
            string? ruleEngineVersion = null, string? rulesJson = null, string? analyzedBy = null,
            string? absorptionJson = null, string? additionalTestingJson = null)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO analysis_snapshots
                    (specimen_id, analyzed_at_utc, software_version, settings_json, input_fingerprint,
                     ruled_out_json, suspected_json, acs_json, rule_engine_version, rules_json, analyzed_by,
                     absorption_json, additional_testing_json)
                VALUES ($sid, $at, $ver, $settings, $fp, $ro, $sus, $acs, $rev, $rules, $by, $abs, $more)";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$ver", softwareVersion);
            cmd.Parameters.AddWithValue("$settings", settingsJson);
            cmd.Parameters.AddWithValue("$fp", inputFingerprint);
            cmd.Parameters.AddWithValue("$ro", (object?)ruledOutJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$sus", (object?)suspectedJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$acs", (object?)acsJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rev", (object?)ruleEngineVersion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rules", (object?)rulesJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$by",
                (object?)(string.IsNullOrWhiteSpace(analyzedBy) ? null : analyzedBy.Trim()) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$abs", (object?)absorptionJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$more", (object?)additionalTestingJson ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        /// <summary>All analysis runs for a specimen, oldest first (time, operator, engine version only).</summary>
        public List<AnalysisSnapshot> GetAnalysisSnapshots(string specimenId)
        {
            var list = new List<AnalysisSnapshot>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, analyzed_at_utc, rule_engine_version, analyzed_by
                FROM analysis_snapshots WHERE specimen_id = $sid ORDER BY id";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new AnalysisSnapshot
                {
                    Id = r.GetInt64(0),
                    SpecimenId = specimenId,
                    AnalyzedAtUtc = r.GetString(1),
                    RuleEngineVersion = r.IsDBNull(2) ? null : r.GetString(2),
                    AnalyzedBy = r.IsDBNull(3) ? null : r.GetString(3),
                });
            return list;
        }

        public AnalysisSnapshot? GetLatestAnalysisSnapshot(string specimenId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, specimen_id, analyzed_at_utc, software_version, settings_json, input_fingerprint,
                       ruled_out_json, suspected_json, acs_json, rule_engine_version, rules_json, analyzed_by,
                       absorption_json, additional_testing_json
                FROM analysis_snapshots
                WHERE specimen_id = $sid
                ORDER BY id DESC
                LIMIT 1";
            cmd.Parameters.AddWithValue("$sid", specimenId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new AnalysisSnapshot
            {
                Id = r.GetInt64(0),
                SpecimenId = r.GetString(1),
                AnalyzedAtUtc = r.GetString(2),
                SoftwareVersion = r.GetString(3),
                SettingsJson = r.GetString(4),
                InputFingerprint = r.GetString(5),
                RuledOutJson = r.IsDBNull(6) ? null : r.GetString(6),
                SuspectedJson = r.IsDBNull(7) ? null : r.GetString(7),
                AcsJson = r.IsDBNull(8) ? null : r.GetString(8),
                RuleEngineVersion = r.FieldCount > 9 && !r.IsDBNull(9) ? r.GetString(9) : null,
                RulesJson = r.FieldCount > 10 && !r.IsDBNull(10) ? r.GetString(10) : null,
                AnalyzedBy = r.FieldCount > 11 && !r.IsDBNull(11) ? r.GetString(11) : null,
                AbsorptionJson = r.FieldCount > 12 && !r.IsDBNull(12) ? r.GetString(12) : null,
                AdditionalTestingJson = r.FieldCount > 13 && !r.IsDBNull(13) ? r.GetString(13) : null
            };
        }

        public void Dispose() => _conn?.Dispose();
    }
}
