using System.Reflection;
using System.Text;

namespace AntibodyPanels.Services
{
    /// <summary>
    /// Product identity and intended-use labeling for 510(k) software documentation.
    /// </summary>
    public static class SoftwareIdentity
    {
        public const string ProductName = "Antibody Panel Management System";

        public const string IntendedUse =
            "Decision-support software for licensed immunohematology professionals to document " +
            "antibody-panel reactions and compute rule-out and probability statistics. " +
            "A qualified professional must confirm identification. This software does not issue " +
            "or release blood units and is not a substitute for licensed clinical judgment.";

        public static string Version
        {
            get
            {
                var asm = typeof(SoftwareIdentity).Assembly;
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info))
                    return info.Split('+')[0];
                return asm.GetName().Version?.ToString() ?? "2.0.0";
            }
        }

        public static string VersionLabel => $"Version {Version}";

        public const int ReportLineWidth = 96;

        public static string ReportPreamble()
        {
            return Wrap($"{ProductName}  {VersionLabel}", ReportLineWidth) + Environment.NewLine +
                   Wrap($"INTENDED USE: {IntendedUse}", ReportLineWidth) + Environment.NewLine + Environment.NewLine;
        }

        public static string Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width < 1 || text.Length <= width) return text;
            var sb = new StringBuilder();
            var column = 0;
            foreach (var word in text.Split(' '))
            {
                if (word.Length == 0) continue;
                if (column > 0 && column + 1 + word.Length > width)
                {
                    sb.Append(Environment.NewLine);
                    column = 0;
                }
                if (column > 0)
                {
                    sb.Append(' ');
                    column++;
                }
                sb.Append(word);
                column += word.Length;
            }
            return sb.ToString();
        }

        public static string AboutText()
        {
            return $"{ProductName}\n\n" +
                   $"{VersionLabel} (C# / WPF)\n\n" +
                   $"{IntendedUse}\n\n" +
                   "Press F1 for help on the current section.";
        }
    }
}
