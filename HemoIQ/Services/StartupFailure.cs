using System;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Services
{
    /// <summary>
    /// Plain-language reason HemoIQ could not open its database at start-up, with the file path and what to do.
    /// The underlying SQLite error may be wrapped (for example in a XAML exception from the main window).
    /// </summary>
    public static class StartupFailure
    {
        public static string Describe(Exception error, string databasePath)
        {
            var sqlite = Find<SqliteException>(error);
            var head = $"HemoIQ could not open its database:\n{databasePath}\n\n";
            var reason = sqlite?.SqliteErrorCode switch
            {
                8 => "The database file or its folder is read-only. HemoIQ must write to it when it starts (to record " +
                     "audit entries and keep the file up to date). Check the file's read-only setting and your permission " +
                     "on the folder, or ask IT to make the share writable.",
                5 or 6 => "Another program is using the database (for example a second copy of HemoIQ, a backup or a sync " +
                          "tool). Close it and start HemoIQ again.",
                14 => "The file could not be opened. Check that the folder exists and that you have permission to it.",
                11 or 26 => "The file is damaged or is not a HemoIQ database. Restore it from your laboratory's backup; do " +
                            "not delete it.",
                _ => "The error was: " + (sqlite?.Message ?? Innermost(error).Message),
            };
            return head + reason + "\n\nNothing was changed. HemoIQ will now close.";
        }

        private static T? Find<T>(Exception? e) where T : Exception
        {
            for (; e != null; e = e.InnerException)
                if (e is T match) return match;
            return null;
        }

        private static Exception Innermost(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e;
        }
    }
}
