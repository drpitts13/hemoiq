using System.Text.RegularExpressions;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class RecordResultsPermissionTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    /// <summary>Admin "adm", technologist "tech", read-only "view"; specimen RR-1 linked to a panel.</summary>
    private static (IsolatedDatabase Iso, int Panel) WithRegistry()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("RR", "RR-L", "V", 3, null, false);
        iso.Db.AddSpecimen("RR-1", "serum", null);
        iso.Db.LinkSpecimenPanel("RR-1", panel);
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Role = UserRole.Technologist });
            iso.Db.SaveUser(new AppUser { Login = "view", Initials = "VW", Role = UserRole.ReadOnly });
        });
        return (iso, panel);
    }

    [Fact]
    public void ReadOnlyUser_CanReadEverything_ButCannotChangeData()
    {
        var (iso, panel) = WithRegistry();
        using var _ = iso;
        As("view", () =>
        {
            Assert.NotNull(iso.Db.GetSpecimen("RR-1"));
            Assert.Equal(3, iso.Db.GetPanelCells(panel).Count);
            var analysis = iso.Analyzer.AnalyzeSpecimen("RR-1", updateDb: false);
            Assert.NotNull(analysis);

            var ex = Assert.Throws<UnauthorizedAccessException>(() => iso.Db.SaveReaction("RR-1", panel, "1", "0", "0", "2+", "NT"));
            Assert.Contains("(Read-only) cannot record or change laboratory data. This requires the Technologist or Supervisor or Administrator role.", ex.Message);
            Assert.Throws<UnauthorizedAccessException>(() => iso.Db.AddSpecimen("RR-2", "serum", null));
            Assert.Throws<UnauthorizedAccessException>(() => iso.Db.SetSpecimenFinalCall("RR-1", "anti-E", null, "VW"));
            Assert.Throws<UnauthorizedAccessException>(() => iso.Db.AddPanel("X", "X-L", "V", 3, null, false));
            Assert.Throws<UnauthorizedAccessException>(() => iso.Db.SavePatient(new Patient { Mrn = "M-RO" }));
        });
        Assert.Null(iso.Db.GetSpecimen("RR-2"));
        Assert.True(string.IsNullOrEmpty(iso.Db.GetSpecimen("RR-1")!.FinalAntibodies));
    }

    [Fact]
    public void UnregisteredLogin_CannotChangeData_OnceUsersAreRegistered()
    {
        var (iso, _) = WithRegistry();
        using var __ = iso;
        As("stranger", () =>
            Assert.Contains("Windows login 'stranger' is not an active HemoIQ user",
                Assert.Throws<UnauthorizedAccessException>(() => iso.Db.AddSpecimen("RR-3", "serum", null)).Message));
    }

    [Fact]
    public void Technologist_RecordsResults_AsBefore()
    {
        var (iso, panel) = WithRegistry();
        using var _ = iso;
        As("tech", () =>
        {
            iso.Db.SaveReaction("RR-1", panel, "1", "0", "0", "2+", "NT");
            iso.Db.AddSpecimen("RR-4", "serum", null);
            iso.Db.SetSpecimenFinalCall("RR-1", "anti-E", null, "TE");
        });
        Assert.Equal("anti-E", iso.Db.GetSpecimen("RR-1")!.FinalAntibodies);
    }

    [Fact]
    public void EmptyRegistry_StillAllowsEverything()
    {
        using var iso = new IsolatedDatabase();
        As("anyone", () => iso.Db.AddSpecimen("RR-5", "serum", null));
        Assert.NotNull(iso.Db.GetSpecimen("RR-5"));
    }

    [Fact]
    public void OldDatabase_UsersTableIsRebuilt_ToAcceptTheReadOnlyRole_KeepingItsUsers()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq-roles-{Guid.NewGuid():N}.db");
        try
        {
            using (var conn = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE users (
                        login TEXT PRIMARY KEY COLLATE NOCASE CHECK (length(login) BETWEEN 1 AND 128),
                        initials TEXT NOT NULL CHECK (length(initials) BETWEEN 1 AND 12),
                        name TEXT,
                        role TEXT NOT NULL CHECK (role IN ('technologist','supervisor','administrator')),
                        active INTEGER NOT NULL DEFAULT 1);
                    INSERT INTO users (login, initials, name, role, active) VALUES ('adm', 'AD', 'Admin', 'administrator', 1);";
                cmd.ExecuteNonQuery();
            }
            using var db = new HemoIQ.Data.DatabaseService(path);
            Assert.Equal("adm", Assert.Single(db.GetUsers()).Login);
            As("adm", () => db.SaveUser(new AppUser { Login = "view", Initials = "VW", Role = UserRole.ReadOnly }));
            Assert.Equal(UserRole.ReadOnly, db.GetUsers().Single(u => u.Login == "view").Role);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(path); } catch (IOException) { }
        }
    }

    /// <summary>
    /// Every public DatabaseService method that changes data (directly, or through another writer)
    /// must check a permission, or be listed here with the reason it needs none.
    /// </summary>
    private static readonly (string Method, string Reason)[] Unguarded =
    {
        ("AppendAudit", "the audit log itself; callers are guarded"),
        ("DatabaseService", "constructor: schema creation and migrations"),
        ("AddSpecimenAntibody", "derived analysis output, rebuilt whenever anyone views an analysis"),
        ("AddSpecimenRuleout", "derived analysis output"),
        ("ClearSpecimenAntibodies", "derived analysis output"),
        ("ClearSpecimenRuleouts", "derived analysis output"),
        ("SaveAnalysisSnapshot", "record of an analysis that was run"),
        ("SetSpecimenLastAnalyzed", "timestamp of an analysis that was run"),
        ("TouchSpecimenReactionsUpdated", "timestamp; the reaction save that calls it is guarded"),
        ("AddPanelCell", "called by guarded panel creation and replacement"),
        ("SetPanelVendorMetadata", "called only while importing, after the guarded AddPanel"),
        ("GetOrCreateDefaultRun", "creates the empty run a viewer needs to open the reaction grid; no results"),
    };

    [Fact]
    public void EveryPublicDatabaseWriter_ChecksAPermission_OrIsListed()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Data", "DatabaseService.cs")).Replace("\r\n", "\n");
        var methods = new Dictionary<string, (bool Public, string Body)>();
        foreach (var body in Regex.Split(src, @"\n(?=        (?:public|private|internal)[^\n]*\()"))
        {
            var m = Regex.Match(body, @"^\s*(public|private|internal)[^\n(]*?(\w+)\s*\(");
            if (!m.Success) continue;
            var name = m.Groups[2].Value;
            var prior = methods.TryGetValue(name, out var p) ? p.Body : "";
            methods[name] = (m.Groups[1].Value == "public" || (p.Public && prior.Length > 0), prior + body);
        }
        var writers = methods.Where(kv => Regex.IsMatch(kv.Value.Body, @"\b(UPDATE \w+\s+SET|DELETE FROM \w+|INSERT (?:OR \w+ )?INTO \w+)|AppendAudit\("))
            .Select(kv => kv.Key).ToHashSet();
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var (name, (_, body)) in methods)
                if (!writers.Contains(name) && writers.Any(w => w != name && Regex.IsMatch(body, $@"\b{w}\(")))
                    changed = writers.Add(name);
        }
        var missing = writers
            .Where(n => methods[n].Public)
            .Where(n => !methods[n].Body.Contains("EnsureCanRecord(") && !methods[n].Body.Contains("AccessControl.Demand("))
            .Where(n => Unguarded.All(u => u.Method != n))
            .OrderBy(n => n)
            .ToList();
        Assert.True(missing.Count == 0,
            "Public DatabaseService writers without a permission check. Call EnsureCanRecord() (or a stricter Demand), " +
            "or list the method in RecordResultsPermissionTests.Unguarded with the reason: " + string.Join(", ", missing));
        var stale = Unguarded.Where(u => !writers.Contains(u.Method) || !methods[u.Method].Public).Select(u => u.Method).ToList();
        Assert.True(stale.Count == 0, "Remove stale Unguarded entries: " + string.Join(", ", stale));
    }
}
