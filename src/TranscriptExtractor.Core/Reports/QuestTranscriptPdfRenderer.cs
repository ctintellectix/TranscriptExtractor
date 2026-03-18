using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TranscriptExtractor.Core.Reports;

public sealed class QuestTranscriptPdfRenderer : ITranscriptPdfRenderer
{
    private static readonly Regex TagRegex = new("<.*?>", RegexOptions.Compiled);

    public byte[] RenderPdf(string html, string templateVersion)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var lines = ExtractLines(html);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Transcript Intelligence Brief").FontSize(20).Bold();
                        column.Item().Text($"Template Version: {templateVersion}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                page.Content()
                    .PaddingVertical(12)
                    .Column(column =>
                    {
                        foreach (var line in lines)
                        {
                            column.Item().Text(line);
                        }
                    });
            });
        }).GeneratePdf();
    }

    private static List<string> ExtractLines(string html)
    {
        var normalized = html
            .Replace("</li>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</h1>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</h2>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</div>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("</section>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);

        var decoded = WebUtility.HtmlDecode(TagRegex.Replace(normalized, string.Empty));

        return decoded
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
