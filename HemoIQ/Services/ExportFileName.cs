using System.IO;
using System.Linq;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Default file names suggested by Save dialogs for data the user typed (a panel name or lot). Characters that
    /// Windows refuses in a file name are replaced, so the dialog never opens on an invalid name, and the lot is
    /// included so two lots of the same panel are not offered the same file.
    /// </summary>
    public static class ExportFileName
    {
        public static string ForPanel(Panel panel, string suffix, string extension)
        {
            var lot = string.IsNullOrWhiteSpace(panel.LotNumber) ? "" : $" lot {panel.LotNumber.Trim()}";
            var tail = string.IsNullOrWhiteSpace(suffix) ? "" : " " + suffix.Trim();
            return Clean($"{panel.Name.Trim()}{lot}{tail}") + "." + extension.TrimStart('.');
        }

        /// <summary>Replaces characters not allowed in a Windows file name, trims dots and spaces, caps the length.</summary>
        public static string Clean(string name)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).ToHashSet();
            var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray())
                .Trim().TrimEnd('.', ' ');
            if (cleaned.Length > 120) cleaned = cleaned[..120].TrimEnd('.', ' ');
            return cleaned.Length == 0 ? "panel" : cleaned;
        }
    }
}
