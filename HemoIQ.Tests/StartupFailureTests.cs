using HemoIQ.Data;
using HemoIQ.Services;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// When the database cannot be opened for writing (a read-only file or share, a file another program holds), HemoIQ
/// must say so plainly with the path, release the file, and close, instead of failing later on the first save or
/// running on with no window.
/// </summary>
[Collection("PersistentDatabase")]
public class StartupFailureTests
{
    [Theory]
    [InlineData(8, "read-only")]
    [InlineData(5, "Another program is using the database")]
    [InlineData(6, "Another program is using the database")]
    [InlineData(14, "could not be opened")]
    [InlineData(26, "damaged or is not a HemoIQ database")]
    public void Describe_ExplainsEachSqliteFailure_WithThePath(int code, string expected)
    {
        var wrapped = new InvalidOperationException("outer", new SqliteException("inner", code));
        var text = StartupFailure.Describe(wrapped, @"C:\Lab\hemoiq.db");
        Assert.StartsWith("HemoIQ could not open its database:\nC:\\Lab\\hemoiq.db", text);
        Assert.Contains(expected, text);
        Assert.EndsWith("Nothing was changed. HemoIQ will now close.", text);
    }

    [Fact]
    public void Describe_OtherErrors_ShowTheInnermostMessage()
    {
        var text = StartupFailure.Describe(new Exception("outer", new IOException("disk gone")), "x.db");
        Assert.Contains("The error was: disk gone", text);
    }

    [Fact]
    public void ReadOnlyDatabase_IsRefusedAtOpen_AndTheFileIsReleased()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_ro_{Guid.NewGuid():N}.db");
        try
        {
            // An up-to-date database: opening it again would make no schema changes.
            using (new DatabaseService(path)) { }
            File.SetAttributes(path, FileAttributes.ReadOnly);

            var ex = Assert.ThrowsAny<Exception>(() => new DatabaseService(path));
            Assert.True(StartupFailure.Describe(ex, path).Contains("read-only"), ex.ToString());

            // The refused connection was disposed; after the usual pool clear, nothing holds the file.
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }
    }
}
