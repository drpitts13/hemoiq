using System;
using System.IO;

namespace HemoIQ.Services
{
    /// <summary>
    /// Local application log. Do not write reaction grades, comments, or other PHI.
    /// The log is a plain-text file outside the database's access controls, so exception messages written by HemoIQ
    /// itself (which name accessions, MRNs and lots, for example "Specimen X not found") are withheld; the exception
    /// type and the method that threw are logged instead, which is enough to locate the fault. Messages from the
    /// framework's own storage, file and network errors carry no patient data and are kept for diagnosis.
    /// </summary>
    public static class AppLog
    {
        public static string LogDirectory =>
            Path.Combine(AppDataPaths.Root, "logs");

        public static void Info(string message) => Write("INFO", message);

        public static void Error(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                Write("ERROR", message);
                return;
            }

            Write("ERROR", $"{message} {Describe(ex)}");
        }

        /// <summary>The exception as logged: type, a message only when it cannot name a record, and where it was thrown.</summary>
        public static string Describe(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;
            var text = ex.GetType().Name;
            if (!ReferenceEquals(inner, ex)) text += $" (inner {inner.GetType().Name})";
            text += IsFrameworkError(inner)
                ? $": {inner.Message}"
                : ": message withheld because it may name a patient record";
            var site = inner.TargetSite;
            if (site != null) text += $" at {site.DeclaringType?.Name}.{site.Name}";
            return text;
        }

        /// <summary>Storage, file and network errors raised by the framework, whose text is about the system, not a record.</summary>
        private static bool IsFrameworkError(Exception e) =>
            e is Microsoft.Data.Sqlite.SqliteException
              or IOException
              or System.Net.Http.HttpRequestException
              or TimeoutException
              or OutOfMemoryException;

        // Two threads appending at once can collide on the file; the loser's line would be silently dropped.
        private static readonly object Gate = new();

        private static void Write(string level, string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var path = Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
                var line = $"{DateTime.Now:o} [{level}] v{SoftwareIdentity.Version} {message}{Environment.NewLine}";
                var bytes = System.Text.Encoding.UTF8.GetBytes(line);
                lock (Gate)
                {
                    // Share read and write, so a log viewer or backup holding the file open does not make every line
                    // fail; retry briefly if another process is mid-write.
                    for (var attempt = 0; ; attempt++)
                    {
                        try
                        {
                            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write,
                                FileShare.ReadWrite | FileShare.Delete);
                            stream.Write(bytes, 0, bytes.Length);
                            break;
                        }
                        catch (IOException) when (attempt < 5)
                        {
                            System.Threading.Thread.Sleep(20);
                        }
                    }
                }
            }
            catch
            {
                // Logging must never break clinical workflow.
            }
        }
    }
}
