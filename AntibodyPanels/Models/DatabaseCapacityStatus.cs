namespace AntibodyPanels.Models
{
    public sealed class DatabaseCapacityStatus
    {
        public const double WarningPercent = 80.0;

        public long FileBytes { get; init; }
        public long MaxBytes { get; init; }
        public double PercentUsed { get; init; }
        public bool IsNearCapacity { get; init; }

        public static long BytesFromMb(int megabytes) => (long)megabytes * 1024L * 1024L;

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024L * 1024)
                return $"{bytes / 1024.0:0.#} KB";
            if (bytes < 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):0.#} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##} GB";
        }
    }

    public sealed class PurgeResult
    {
        public int SpecimensDeleted { get; init; }
        public string? ArchivePath { get; init; }
        public long FileSizeBytesAfter { get; init; }
    }

    public sealed class ArchiveSpecimenRow
    {
        public string AccessionNumber { get; init; } = "";
        public string Type { get; init; } = "";
        public string CreatedDate { get; init; } = "";
        public bool ExistsInLive { get; init; }
        public string Recorded { get; init; } = "";
        public string Status => ExistsInLive ? "Already in database" : "Will restore";

        /// <summary>
        /// Stored final identification, otherwise the antibodies saved with the
        /// specimen, plus DAT and notes. This is the archive record, not a new identification.
        /// </summary>
        public static string FormatRecorded(
            string? finalAntibodies, string? datResult, string? notes, IEnumerable<string>? antibodies)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(finalAntibodies))
                parts.Add(finalAntibodies.Trim());
            else if (antibodies != null)
            {
                var listed = antibodies
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (listed.Count > 0)
                    parts.Add(string.Join("; ", listed));
            }
            if (!string.IsNullOrWhiteSpace(datResult))
                parts.Add("DAT " + datResult.Trim());
            if (!string.IsNullOrWhiteSpace(notes))
                parts.Add(notes.Trim());
            return string.Join("; ", parts);
        }
    }

    public sealed class ArchiveInspection
    {
        public string Path { get; init; } = "";
        public long FileBytes { get; init; }
        public int SpecimenCount { get; init; }
        public int PanelCount { get; init; }
        public string? EarliestCreatedDate { get; init; }
        public string? LatestCreatedDate { get; init; }
        public int AlreadyInLiveCount { get; init; }
        public int RestorableCount => SpecimenCount - AlreadyInLiveCount;
        public List<ArchiveSpecimenRow> Specimens { get; init; } = new();
    }

    public sealed class RestoreResult
    {
        public int SpecimensRestored { get; init; }
        public int SpecimensSkipped { get; init; }
        public int PanelsRestored { get; init; }
        public long FileSizeBytesAfter { get; init; }
    }
}
