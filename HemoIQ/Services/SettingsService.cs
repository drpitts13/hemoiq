using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public static class AppSettings
    {
        public static LabSettings Current { get; set; } = LabSettings.CreateDefault();

        public static event EventHandler? Changed;

        public static void RaiseChanged() => Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>The Preferences file could not be written; the settings in effect are the ones last saved.</summary>
    public sealed class SettingsSaveException : IOException
    {
        public SettingsSaveException(string message, Exception inner) : base(message, inner) { }
    }

    public static class SettingsService
    {
        /// <summary>The settings as last loaded or saved, restored in memory when a save fails.</summary>
        private static string? _lastSaved;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string DefaultPath =>
            Path.Combine(AppDataPaths.Root, "settings.json");

        /// <summary>
        /// Why the last <see cref="Load"/> fell back to defaults although a Preferences file existed, or null. Start-up
        /// shows it, so a lab is never left testing on default ID rules without knowing.
        /// </summary>
        public static string? LastLoadProblem { get; private set; }

        public static void Load(string? path = null)
        {
            path ??= DefaultPath;
            LastLoadProblem = null;
            if (File.Exists(path))
            {
                string? reason = null;
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<LabSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        var asStored = loaded.ClinicalSettingsJson();
                        loaded.Clamp();
                        // An analysis setting outside its allowed range (a hand-edited file, or one from another
                        // version) is replaced, sometimes by its default rather than the nearest limit. Say which,
                        // and keep the file as it was before a save writes the replacement values.
                        var adjusted = LabSettings.DescribeClinicalChanges(asStored, loaded.ClinicalSettingsJson());
                        if (adjusted.Count > 0)
                        {
                            LastLoadProblem = DescribeAdjusted(path, adjusted, KeepCopy(path, "settings.adjusted"));
                            AppLog.Info($"Preferences: {adjusted.Count} analysis setting(s) were out of range and replaced; the user was told.");
                        }
                        AppSettings.Current = loaded;
                        _lastSaved = JsonSerializer.Serialize(loaded, JsonOptions);
                        AppSettings.RaiseChanged();
                        return;
                    }
                    reason = "the file is empty";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
                {
                    reason = ex is JsonException ? "the file is damaged or not a HemoIQ Preferences file" : ex.Message;
                }
                LastLoadProblem = DescribeUnreadable(path, reason!, KeepCopy(path, "settings.unreadable"));
                AppLog.Info("Preferences could not be read; defaults are in effect and the user was told."); // the message names a path
            }

            AppSettings.Current = LabSettings.CreateDefault();
            _lastSaved = JsonSerializer.Serialize(AppSettings.Current, JsonOptions);
            AppSettings.RaiseChanged();
        }

        /// <summary>
        /// Copies an unreadable Preferences file aside before anything can overwrite it, so the lab's settings can be
        /// recovered or re-entered from it. Returns the copy's path, or null when it could not be copied.
        /// </summary>
        private static string? KeepCopy(string path, string prefix)
        {
            try
            {
                var dir = Path.GetDirectoryName(path) ?? ".";
                var copy = Path.Combine(dir, $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Copy(path, copy, overwrite: false);
                return copy;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        public static string DescribeAdjusted(string path, IReadOnlyList<string> adjusted, string? copy) =>
            $"The Preferences file {path} held analysis settings that were out of range or not in the expected form. HemoIQ replaced them:\n\n" +
            string.Join("\n", adjusted.Select(a => "• " + a)) + "\n\n" +
            (copy != null ? $"The file as it was is kept as {copy}. " : "") +
            "Analysis uses the replacement values. Review them in Preferences before testing.";

        public static string DescribeUnreadable(string path, string reason, string? copy) =>
            $"The Preferences file {path} could not be read ({reason}). HemoIQ started with the default Preferences, " +
            "including the default identification and rule-out settings. " +
            (copy != null
                ? $"The unreadable file was kept as {copy}. "
                : "A copy of the unreadable file could not be kept; do not save Preferences until it has been copied elsewhere. ") +
            "Review Preferences (or import a Preferences or configuration export) before testing.";

        /// <summary>
        /// Saves the settings in effect. When the file cannot be written (read-only, locked, disk full) the
        /// settings in memory go back to the ones last saved, so the session never runs on Preferences that are
        /// neither on disk nor audited and would silently revert at the next start, and
        /// <see cref="SettingsSaveException"/> says so.
        /// </summary>
        public static void Save(string? path = null)
        {
            AppSettings.Current.Clamp();
            try
            {
                Persist(AppSettings.Current, path);
            }
            catch (SettingsSaveException)
            {
                if (_lastSaved != null && JsonSerializer.Deserialize<LabSettings>(_lastSaved, JsonOptions) is { } previous)
                    AppSettings.Current = previous;
                AppSettings.RaiseChanged();
                throw;
            }
            AppSettings.RaiseChanged();
        }

        /// <summary>
        /// Writes <paramref name="settings"/> to the Preferences file without touching the settings in effect. The file
        /// is written beside the target and then moved over it, so a write cut short (a full disk) leaves the previous
        /// file whole instead of a truncated one that the next start would read as corrupt and replace with defaults.
        /// </summary>
        public static void Persist(LabSettings settings, string? path = null)
        {
            path ??= DefaultPath;
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tmp = path + ".tmp";
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(tmp, json);
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                try { File.Delete(tmp); } catch (Exception) { /* best effort */ }
                throw new SettingsSaveException(
                    $"Preferences could not be saved to {path}: {ex.Message} " +
                    "The Preferences saved before are still in effect; nothing was changed.", ex);
            }
            _lastSaved = json;
        }
    }
}
