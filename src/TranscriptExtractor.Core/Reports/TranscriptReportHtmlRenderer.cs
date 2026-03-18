using System.Net;
using System.Text;

namespace TranscriptExtractor.Core.Reports;

public static class TranscriptReportHtmlRenderer
{
    public static string Render(TranscriptReportViewModel report, string templateVersion)
    {
        static string H(string value) => WebUtility.HtmlEncode(value);

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang='en'><head><meta charset='utf-8' />");
        html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1' />");
        html.AppendLine("<title>Transcript Report</title>");
        html.AppendLine("</head><body style='font-family:Segoe UI, Arial, sans-serif; color:#1a1a1a; margin:24px;'>");
        html.AppendLine($"<div style='font-size:12px;color:#666;'>Template: {H(templateVersion)}</div>");
        html.AppendLine($"<h1 style='margin-bottom:8px;'>Transcript Intelligence Brief</h1>");
        html.AppendLine($"<div style='margin-bottom:20px;'>Source type: {H(report.SourceType)}</div>");

        AppendSection(html, "Timeline of Events", report.Timeline.Select(x => $"{H(x.Description)} ({H(x.LocationName)})"));
        AppendSection(html, "Allegations", report.Allegations.Select(x => H(x.Description)));
        AppendSection(html, "Statements", report.Statements.Select(x => $"{H(x.SpeakerName)}: {H(x.Summary)}"));
        AppendSection(html, "Key Objects", report.Objects.Select(x => $"{H(x.Name)} ({H(x.Type)})"));
        AppendSection(html, "Relationships", report.Relationships.Select(x => $"{H(x.SubjectName)} - {H(x.RelationshipType)} - {H(x.ObjectName)}"));
        AppendSection(html, "Key Locations", report.Locations.Select(x => $"{H(x.Name)} - {H(x.Address)}"));

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static void AppendSection(StringBuilder html, string title, IEnumerable<string> items)
    {
        html.AppendLine($"<section style='margin-top:18px;'><h2>{WebUtility.HtmlEncode(title)}</h2>");
        html.AppendLine("<ul>");
        foreach (var item in items)
        {
            html.AppendLine($"<li>{item}</li>");
        }

        html.AppendLine("</ul></section>");
    }
}
