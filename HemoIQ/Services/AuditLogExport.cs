using System.IO;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>
    /// The audit-log CSV. It covers the whole log, filtered by the Find text, read from the database: the viewer
    /// loads only the newest events, and an export that silently stopped there would be handed on as the full log.
    /// </summary>
    public static class AuditLogExport
    {
        public const string Header = "OccurredAtUtc,Operator,Action,EntityType,EntityId,Reason,BeforeJson,AfterJson";

        /// <summary>Writes the header and every matching event, newest first; returns the number of events written.</summary>
        public static int Write(DatabaseService db, TextWriter writer, string? filter)
        {
            writer.WriteLine(Header);
            var count = 0;
            foreach (var ev in db.ReadAllAuditEvents())
            {
                if (!ev.MatchesFilter(filter)) continue;
                writer.WriteLine(string.Join(",",
                    CsvText.Field(ev.OccurredAtUtc), CsvText.Field(ev.Operator), CsvText.Field(ev.Action),
                    CsvText.Field(ev.EntityType), CsvText.Field(ev.EntityId), CsvText.Field(ev.Reason),
                    CsvText.Field(ev.BeforeJson), CsvText.Field(ev.AfterJson)));
                count++;
            }
            return count;
        }

        /// <summary>The viewer's count, naming the whole log's size when only the newest events were loaded.</summary>
        public static string CountLabel(int shown, int loaded, int total)
        {
            if (loaded >= total)
                return shown == loaded ? $"{shown} event(s)" : $"Showing {shown} of {loaded}";
            return shown == loaded ? $"Newest {loaded} of {total}" : $"Showing {shown} of the newest {loaded} (log: {total})";
        }

        /// <summary>The line under Find when the log is longer than what was loaded, or null.</summary>
        public static string? TruncationNote(int loaded, int total) =>
            loaded >= total
                ? null
                : $"The log holds {total} events; the newest {loaded} are listed and searched here. " +
                  "Export CSV writes every event that matches Find, from the whole log.";
    }
}
