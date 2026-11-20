using System.Text;
using HemoIQ.Services;

namespace HemoIQ.Help;

public static class UserManual
{
    public static string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {SoftwareIdentity.ProductName}");
        sb.AppendLine();
        sb.AppendLine("# User Manual");
        sb.AppendLine();
        sb.AppendLine(SoftwareIdentity.VersionLabel);
        sb.AppendLine();
        sb.AppendLine("## Intended use");
        sb.AppendLine();
        sb.AppendLine(SoftwareIdentity.IntendedUse);
        sb.AppendLine();
        sb.AppendLine("This file is the same text as **Help → User Manual**. Chapters follow the topic list. Press **F1** to open the chapter for the screen you are on. **Save manual** writes this manual as PDF, Markdown, or text.");
        sb.AppendLine();
        sb.AppendLine("## Contents");
        sb.AppendLine();
        foreach (var topic in HelpCatalog.InReadingOrder())
        {
            var indent = topic.ParentId == null ? "" : "    ";
            sb.AppendLine($"{indent}- {topic.Title}");
        }
        sb.AppendLine();
        foreach (var topic in HelpCatalog.InReadingOrder())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(topic.Markdown.Trim());
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd() + Environment.NewLine;
    }
}
