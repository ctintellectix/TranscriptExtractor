using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TranscriptExtractor.Core.Maps;

namespace TranscriptExtractor.Core.Reports;

public sealed class QuestTranscriptPdfRenderer(
    IAddressGeocoder? geocoder = null,
    IStaticMapRenderer? staticMapRenderer = null) : ITranscriptPdfRenderer
{
    private static readonly Regex TagRegex = new("<.*?>", RegexOptions.Compiled);
    private static readonly Regex SectionRegex = new(
        "<section[^>]*data-section='(?<key>[^']+)'[^>]*>(?<content>.*?)</section>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex DivClassRegex = new(
        "<div[^>]*class='(?<class>[^']*)'[^>]*>(?<content>.*?)</div>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex LocationSummaryRegex = new(
        "<div[^>]*class='[^']*location-summary[^']*'[^>]*data-marker='(?<marker>[^']*)'[^>]*data-name='(?<name>[^']*)'[^>]*data-address='(?<address>[^']*)'[^>]*data-verified='(?<verified>[^']*)'[^>]*>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex ListItemRegex = new(
        "<li>(?<content>.*?)</li>",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public byte[] RenderPdf(string html, string templateVersion)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var sourceType = ExtractSourceType(html);
        var timelineItems = ExtractItems(html, "timeline");
        var allegationItems = ExtractItems(html, "allegations");
        var statementItems = ExtractItems(html, "statements");
        var objectItems = ExtractItems(html, "objects");
        var relationshipItems = ExtractRelationshipItems(html);
        var locationItems = ExtractLocationItems(html);
        var verifiedMapLocations = ExtractVerifiedMapLocations(locationItems);
        var mapImage = BuildMapImage(verifiedMapLocations);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor("#2b2018"));

                page.Header()
                    .ShowOnce()
                    .Column(column =>
                    {
                        column.Item().Background("#fbf4e8").Border(1).BorderColor("#dfcfb5").Padding(18).Column(header =>
                        {
                            header.Item().Text("Transcript Intelligence Brief").FontSize(11).FontColor("#b24c20").SemiBold();
                            header.Item().Text("Narrative Snapshot of Transcript").FontSize(24).Bold().FontFamily("Times New Roman");
                            header.Item().Text($"Template Version: {templateVersion} | Source type: {sourceType}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });

                page.Content()
                    .PaddingVertical(12)
                    .Column(content =>
                    {
                        content.Item().Row(row =>
                        {
                            row.RelativeItem(1.45f).Column(left =>
                            {
                                RenderCardSection(left, "Section 01", "Timeline of Events", timelineItems, "#c85e31");
                                left.Item().PaddingTop(12);
                                RenderCardSection(left, "Section 02", "Statements", statementItems, "#d18c46");
                            });

                            row.RelativeItem(.08f);

                            row.RelativeItem(.95f).Column(right =>
                            {
                                RenderCardSection(right, "Section 03", "Allegations", allegationItems, "#b24c20");
                                right.Item().PaddingTop(12);
                                RenderCardSection(right, "Section 04", "Key Objects", objectItems, "#8f7358");
                                right.Item().PaddingTop(12);
                                RenderRelationshipConstellation(right, relationshipItems);
                            });
                        });

                        content.Item().PageBreak();
                        RenderLocationPanel(content, locationItems, verifiedMapLocations, mapImage);
                    });
            });
        }).GeneratePdf();
    }

    private static void RenderCardSection(ColumnDescriptor column, string sectionNumber, string title, IReadOnlyList<string> items, string accent)
    {
        column.Item().Background("#fcf7ee").Border(1).BorderColor("#e8dece").Padding(14).Column(section =>
        {
            section.Item().Text(sectionNumber).FontSize(9).FontColor("#8f7358");
            section.Item().Text(title).FontSize(18).Bold().FontFamily("Times New Roman");

            if (items.Count == 0)
            {
                section.Item().PaddingTop(8).Text("No items extracted.").FontColor(Colors.Grey.Darken2);
                return;
            }

            foreach (var item in items)
            {
                section.Item().PaddingTop(8).BorderLeft(4).BorderColor(accent).PaddingLeft(10).Text(item);
            }
        });
    }

    private static void RenderRelationshipConstellation(ColumnDescriptor column, IReadOnlyList<string> relationships)
    {
        column.Item().Background("#fcf7ee").Border(1).BorderColor("#e8dece").Padding(14).Column(section =>
        {
            section.Item().Text("Section 05").FontSize(9).FontColor("#8f7358");
            section.Item().Text("Relationships").FontSize(18).Bold().FontFamily("Times New Roman");

            if (relationships.Count == 0)
            {
                section.Item().PaddingTop(8).Text("No relationships extracted.").FontColor(Colors.Grey.Darken2);
                return;
            }

            var first = relationships[0].Split(" - ", StringSplitOptions.TrimEntries);
            var subject = first.Length > 0 ? first[0] : string.Empty;
            var relation = first.Length > 1 ? first[1] : string.Empty;
            var target = first.Length > 2 ? first[2] : string.Empty;

            section.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Background("#efe3d0").PaddingVertical(8).PaddingHorizontal(10).Text(subject);
                row.ConstantItem(8);
                row.RelativeItem().AlignMiddle().Background("#f7eee0").Border(1).BorderColor("#dfc6ab").PaddingVertical(8).PaddingHorizontal(10).Text(relation).FontColor("#b24c20");
                row.ConstantItem(8);
                row.RelativeItem().AlignMiddle().Background("#efe3d0").PaddingVertical(8).PaddingHorizontal(10).Text(target);
            });
        });
    }

    private static void RenderLocationPanel(
        ColumnDescriptor column,
        IReadOnlyList<LocationRenderItem> locations,
        IReadOnlyList<LocationRenderItem> verifiedMapLocations,
        byte[]? mapImage)
    {
        column.Item().Background("#fcf7ee").Border(1).BorderColor("#e8dece").Padding(14).Column(section =>
        {
            section.Item().Text("Section 06").FontSize(9).FontColor("#8f7358");
            section.Item().Text("Key Locations").FontSize(18).Bold().FontFamily("Times New Roman");

            section.Item()
                .PaddingTop(10)
                .Padding(12)
                .Column(body =>
                {
                    if (verifiedMapLocations.Count > 0)
                    {
                        body.Item().Column(legend =>
                        {
                            legend.Item().Text("Legend").FontSize(10).FontColor("#8f7358");
                            foreach (var location in verifiedMapLocations)
                            {
                                legend.Item().PaddingTop(4).Text($"{location.MarkerNumber}. {location.Address}").FontSize(10);
                            }
                        });
                    }

                    if (mapImage is not null)
                    {
                        body.Item()
                            .PaddingTop(8)
                            .MinHeight(340)
                            .Image(mapImage)
                            .FitWidth();
                    }
                    else
                    {
                        body.Item()
                            .PaddingTop(8)
                            .MinHeight(260)
                            .AlignMiddle()
                            .AlignCenter()
                            .Text("No verified map locations available.")
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
        });
    }

    private static string ExtractSourceType(string html)
    {
        var marker = "Source type:";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return string.Empty;
        }

        var tail = html[(index + marker.Length)..];
        var end = tail.IndexOf('<');
        return WebUtility.HtmlDecode((end >= 0 ? tail[..end] : tail).Trim(' ', '|'));
    }

    private static List<string> ExtractItems(string html, string sectionKey)
    {
        var sectionContent = ExtractSectionContent(html, sectionKey);
        if (sectionContent is null)
        {
            return new List<string>();
        }

        return ListItemRegex.Matches(sectionContent)
            .Select(x => Clean(x.Groups["content"].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static List<string> ExtractRelationshipItems(string html)
    {
        var sectionContent = ExtractSectionContent(html, "relationships");
        if (sectionContent is null)
        {
            return new List<string>();
        }

        var segments = DivClassRegex.Matches(sectionContent)
            .Where(x => x.Groups["class"].Value.Contains("relationship-", StringComparison.OrdinalIgnoreCase))
            .Select(x => Clean(x.Groups["content"].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return segments.Count >= 3
            ? new List<string> { $"{segments[0]} - {segments[1]} - {segments[2]}" }
            : new List<string>();
    }

    private static IReadOnlyList<LocationRenderItem> ExtractVerifiedMapLocations(IReadOnlyList<LocationRenderItem> locations)
        => locations.Where(x => x.IsVerifiedAddress).ToList();

    private byte[]? BuildMapImage(IReadOnlyList<LocationRenderItem> verifiedMapLocations)
    {
        if (verifiedMapLocations.Count == 0 || staticMapRenderer is null || geocoder is null)
        {
            return null;
        }

        var pins = new List<StaticMapPin>();
        foreach (var location in verifiedMapLocations)
        {
            var geocoded = geocoder.GeocodeAsync(location.Address, CancellationToken.None).GetAwaiter().GetResult();
            if (geocoded is null)
            {
                continue;
            }

            pins.Add(new StaticMapPin(location.MarkerNumber, location.Name, location.Address, geocoded.Latitude, geocoded.Longitude));
        }

        if (pins.Count == 0)
        {
            return null;
        }

        return staticMapRenderer.RenderAsync(
                new StaticMapRequest
                {
                    Width = 640,
                    Height = 360,
                    Pins = pins
                },
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static List<LocationRenderItem> ExtractLocationItems(string html)
    {
        var sectionContent = ExtractSectionContent(html, "locations");
        if (sectionContent is null)
        {
            return new List<LocationRenderItem>();
        }

        return LocationSummaryRegex.Matches(sectionContent)
            .Select(x => new LocationRenderItem(
                ParseMarkerNumber(WebUtility.HtmlDecode(x.Groups["marker"].Value)),
                WebUtility.HtmlDecode(x.Groups["name"].Value),
                WebUtility.HtmlDecode(x.Groups["address"].Value),
                bool.TryParse(x.Groups["verified"].Value, out var isVerified) && isVerified))
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();
    }

    private static string? ExtractSectionContent(string html, string sectionKey)
    {
        var match = SectionRegex.Matches(html)
            .FirstOrDefault(x => x.Groups["key"].Value.Equals(sectionKey, StringComparison.OrdinalIgnoreCase));

        return match?.Groups["content"].Value;
    }

    private static string Clean(string htmlFragment)
    {
        return WebUtility.HtmlDecode(TagRegex.Replace(htmlFragment, string.Empty)).Trim();
    }

    private static int ParseMarkerNumber(string markerLabel)
    {
        var digits = new string(markerLabel.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : 0;
    }
    private sealed record LocationRenderItem(
        int MarkerNumber,
        string Name,
        string Address,
        bool IsVerifiedAddress);
}
