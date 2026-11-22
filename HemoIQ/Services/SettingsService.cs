using System;
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

        public static void Load(string? path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<LabSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        loaded.Clamp();
                        AppSettings.Current = loaded;
                        _lastSaved = JsonSerializer.Serialize(loaded, JsonOptions);
                        AppSettings.RaiseChanged();
                        return;
                    }
                }
            }
            catch
            {
                // Keep defaults if the file is missing or corrupt.
            }

            AppSettings.Current = LabSettings.CreateDefault();
            _lastSaved = JsonSerializer.Serialize(AppSettings.Current, JsonOptions);
            AppSettings.RaiseChanged();
        }

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
