using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// A purge archive may be the only copy of specimens already removed from the live database, so a purge never
/// replaces an existing file.
/// </summary>
[Collection("PersistentDatabase")]
public class ArchiveOverwriteTests
{
    private static long CountSpecimens(string archive)
    {
        SqliteConnection.ClearAllPools();
        using var conn = new SqliteConnection($"Data Source={archive};Mode=ReadOnly;Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM specimens";
        return (long)cmd.ExecuteScalar()!;
    }

    [Fact]
    public void SecondPurgeToTheSameArchive_IsRefused_AndKeepsBothTheArchiveAndTheLiveSpecimen()
    {
        using var iso = new IsolatedDatabase();
        var archive = Path.Combine(Path.GetTempPath(), $"hemoiq_arch_{Guid.NewGuid():N}.db");
        try
        {
            iso.Db.AddSpecimen("OLD-1", "serum", null, createdDate: "2019-03-01");
            iso.Db.PurgeSpecimensCreatedBefore("2020-01-01", archive);
            SqliteConnection.ClearAllPools();
            Assert.Equal(1, CountSpecimens(archive));

            iso.Db.AddSpecimen("OLD-2", "serum", null, createdDate: "2020-06-01");
            var ex = Assert.Throws<InvalidOperationException>(() =>
                iso.Db.PurgeSpecimensCreatedBefore("2021-01-01", archive));

            Assert.Equal($"The archive file {Path.GetFileName(archive)} already exists. Choose a new file name: replacing it " +
                         "would delete any specimens archived there earlier. Nothing was purged.", ex.Message);
            Assert.NotNull(iso.Db.GetSpecimen("OLD-2"));
            Assert.Equal(1, CountSpecimens(archive));
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(archive); }
    }

    [Fact]
    public void ExistingNonArchiveFile_IsNotReplaced()
    {
        using var iso = new IsolatedDatabase();
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_other_{Guid.NewGuid():N}.db");
        File.WriteAllText(path, "keep me");
        try
        {
            iso.Db.AddSpecimen("OLD-3", "serum", null, createdDate: "2019-03-01");
            Assert.Throws<InvalidOperationException>(() => iso.Db.PurgeSpecimensCreatedBefore("2020-01-01", path));
            Assert.Equal("keep me", File.ReadAllText(path));
            Assert.NotNull(iso.Db.GetSpecimen("OLD-3"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void NewArchiveFile_StillWorks()
    {
        using var iso = new IsolatedDatabase();
        var archive = Path.Combine(Path.GetTempPath(), "hemoiq_new_" + Guid.NewGuid().ToString("N"), "a.db");
        try
        {
            iso.Db.AddSpecimen("OLD-4", "serum", null, createdDate: "2019-03-01");
            var result = iso.Db.PurgeSpecimensCreatedBefore("2020-01-01", archive);
            Assert.Equal(1, result.SpecimensDeleted);
            Assert.Null(iso.Db.GetSpecimen("OLD-4"));
            Assert.Equal(1, CountSpecimens(archive));
        }
        finally { SqliteConnection.ClearAllPools(); Directory.Delete(Path.GetDirectoryName(archive)!, true); }
    }
}
