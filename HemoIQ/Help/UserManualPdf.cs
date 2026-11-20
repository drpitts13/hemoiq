using System.Text;
using HemoIQ.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace HemoIQ.Help;

public static class UserManualPdf
{
    public static void Save(string path)
    {
        var doc = new PdfDocument();
        doc.Info.Title = $"{SoftwareIdentity.ProductName} — User Manual";
        doc.Info.Author = SoftwareIdentity.ProductName;

        var body = new XFont("Segoe UI", 11);
        var bodyBold = new XFont("Segoe UI", 11, XFontStyleEx.Bold);
        var heading = new XFont("Segoe UI", 16, XFontStyleEx.Bold);
        var subheading = new XFont("Segoe UI", 13, XFontStyleEx.Bold);

        var page = doc.AddPage();
        page.Size = PdfSharp.PageSize.Letter;
        var gfx = XGraphics.FromPdfPage(page);
        double y = 54;
        var bottom = page.Height.Point - 54;

        void NewPage()
        {
            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PdfSharp.PageSize.Letter;
            gfx = XGraphics.FromPdfPage(page);
            y = 54;
        }

        void Ensure(double height)
        {
            if (y + height > bottom)
                NewPage();
        }

        void DrawWrapped(string text, XFont font, double indent, double gapAfter)
        {
            var max = page.Width.Point - 108 - indent;
            var line = new StringBuilder();
            foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (gfx.MeasureString(candidate, font).Width > max && line.Length > 0)
                {
                    Ensure(16);
                    gfx.DrawString(line.ToString(), font, XBrushes.Black,
                        new XRect(54 + indent, y, max, 16), XStringFormats.TopLeft);
                    y += 15;
                    line.Clear();
                }
                if (line.Length > 0)
                    line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0)
            {
                Ensure(16);
                gfx.DrawString(line.ToString(), font, XBrushes.Black,
                    new XRect(54 + indent, y, max, 16), XStringFormats.TopLeft);
                y += 15;
            }
            y += gapAfter;
        }

        foreach (var raw in UserManual.Build().Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                y += 6;
                continue;
            }
            if (line.Trim() == "---")
            {
                Ensure(12);
                gfx.DrawLine(XPens.LightGray, 54, y + 4, page.Width.Point - 54, y + 4);
                y += 14;
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                y += 4;
                DrawWrapped(StripMarkers(trimmed[2..]), heading, 0, 6);
                continue;
            }
            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                y += 2;
                DrawWrapped(StripMarkers(trimmed[3..]), subheading, 0, 4);
                continue;
            }
            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                DrawWrapped(StripMarkers(trimmed[4..]), bodyBold, 0, 3);
                continue;
            }
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                DrawWrapped("•  " + StripMarkers(trimmed[2..]), body, 12, 2);
                continue;
            }
            var number = 0;
            var dot = trimmed.IndexOf(". ", StringComparison.Ordinal);
            if (dot > 0 && dot < 4 && int.TryParse(trimmed[..dot], out number))
            {
                DrawWrapped(trimmed[..(dot + 1)] + "  " + StripMarkers(trimmed[(dot + 2)..]), body, 12, 2);
                continue;
            }

            DrawWrapped(StripMarkers(trimmed), body, 0, 3);
        }

        gfx.Dispose();
        doc.Save(path);
        doc.Dispose();
    }

    private static string StripMarkers(string text) => text.Replace("**", "").Replace("`", "");
}
