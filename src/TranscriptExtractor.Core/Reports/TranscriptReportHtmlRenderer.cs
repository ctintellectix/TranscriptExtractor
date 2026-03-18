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
        html.AppendLine("""
            <style>
              body { font-family: "Segoe UI", Arial, sans-serif; color:#1a1a1a; margin:0; background:linear-gradient(180deg,#f7f2e8 0%,#efe6d7 100%); }
              .editorial-infographic { padding:28px; }
              .editorial-shell { max-width:1024px; margin:0 auto; background:#fffdf9; border:1px solid #dccfb8; border-radius:18px; box-shadow:0 18px 40px rgba(90,66,38,.10); overflow:hidden; }
              .report-hero { padding:26px 28px 18px; border-bottom:1px solid #e9dfd0; background:radial-gradient(circle at top right, rgba(191,96,47,.16), transparent 32%), linear-gradient(135deg,#fffaf1 0%,#f7efe0 100%); }
              .eyebrow { font-size:11px; letter-spacing:.22em; text-transform:uppercase; color:#b24c20; font-weight:700; }
              .hero-title { font-family: Georgia, serif; font-size:34px; line-height:1.08; color:#2b2018; margin-top:10px; }
              .hero-meta { margin-top:14px; color:#684a36; font-size:13px; }
              .content-grid { padding:22px 28px 18px; display:grid; grid-template-columns:1.45fr .95fr; gap:18px; }
              .full-width-row { padding:0 28px 28px; }
              .content-column { display:grid; gap:18px; }
              .report-section { background:#fcf7ee; border:1px solid #e8dece; border-radius:16px; padding:18px; }
              .section-number { font-size:11px; letter-spacing:.18em; text-transform:uppercase; color:#8f7358; }
              .section-title { margin:8px 0 12px; font-family:Georgia,serif; font-size:24px; color:#241c17; }
              .timeline-item, .statement-card, .object-card, .allegation-card, .location-summary { background:#fffdf9; border:1px solid #eadfce; border-radius:12px; padding:12px 14px; margin-bottom:10px; }
              .timeline-item { border-left:4px solid #c85e31; }
              .relationship-constellation { position:relative; min-height:220px; overflow:hidden; }
              .relationship-node { position:absolute; padding:10px 14px; border-radius:999px; background:#efe3d0; color:#5e4939; }
              .relationship-edge { position:absolute; padding:8px 12px; border-radius:999px; background:#f7eee0; border:1px solid #dfc6ab; color:#b24c20; font-size:13px; }
              .location-map-panel { display:block; }
              .map-canvas { height:420px; border-radius:14px; position:relative; overflow:hidden; background:radial-gradient(circle at 30% 34%, rgba(255,255,255,.55), transparent 16%), linear-gradient(180deg,#efe4d2 0%,#eadbc4 100%); border:1px solid #e3d4be; }
              .map-road { position:absolute; background:#d6c1a5; border-radius:999px; opacity:.8; }
              .map-marker { position:absolute; width:16px; height:16px; border-radius:50%; border:3px solid #fff7eb; box-shadow:0 0 0 1px rgba(70,45,22,.18); }
              .map-label { position:absolute; background:#fff8ec; border:1px solid #ead7bd; border-radius:12px; padding:8px 10px; color:#5b4738; font-size:12px; box-shadow:0 8px 18px rgba(66,44,23,.08); }
              .map-unavailable { height:100%; display:flex; align-items:center; justify-content:center; text-align:center; color:#6f5a48; font-size:15px; padding:18px; }
              .location-data-store { display:none; }
              ul.clean-list { list-style:none; padding:0; margin:0; }
            </style>
            """);
        html.AppendLine($"</head><body><div class='editorial-infographic' data-template-version='{H(templateVersion)}'>");
        html.AppendLine("<div class='editorial-shell'>");
        html.AppendLine("<div class='report-hero'>");
        html.AppendLine($"<div class='eyebrow'>Transcript Intelligence Brief</div>");
        html.AppendLine("<div class='hero-title'>Narrative Snapshot of Transcript</div>");
        html.AppendLine($"<div class='hero-meta'>Template: {H(templateVersion)} | Source type: {H(report.SourceType)}</div>");
        html.AppendLine("</div>");
        html.AppendLine("<div class='content-grid'>");
        html.AppendLine("<div class='content-column'>");
        AppendSection(html, "01", "Timeline of Events", "timeline",
            report.Timeline.Select(x => $"<div class='timeline-item'><strong>{H(x.Description)}</strong><div>{H(x.LocationName)}</div></div>"));
        AppendSection(html, "02", "Statements", "statements",
            report.Statements.Select(x => $"<div class='statement-card'><strong>{H(x.SpeakerName)}</strong><div>{H(x.Summary)}</div></div>"));
        html.AppendLine("</div>");
        html.AppendLine("<div class='content-column'>");
        AppendSection(html, "03", "Allegations", "allegations",
            report.Allegations.Select(x => $"<div class='allegation-card'>{H(x.Description)}</div>"));
        AppendSection(html, "04", "Key Objects", "objects",
            report.Objects.Select(x => $"<div class='object-card'><strong>{H(x.Name)}</strong><div>{H(x.Type)}</div></div>"));
        AppendRelationshipConstellation(html, report);
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        html.AppendLine("<div class='full-width-row'>");
        AppendLocationMapPanel(html, report);
        html.AppendLine("</div>");
        html.AppendLine("</div></div>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static void AppendSection(StringBuilder html, string number, string title, string sectionKey, IEnumerable<string> items)
    {
        html.AppendLine($"<section class='report-section' data-section='{WebUtility.HtmlEncode(sectionKey)}'>");
        html.AppendLine($"<div class='section-number'>Section {WebUtility.HtmlEncode(number)}</div>");
        html.AppendLine($"<h2 class='section-title'>{WebUtility.HtmlEncode(title)}</h2>");
        html.AppendLine("<ul class='clean-list'>");
        foreach (var item in items)
        {
            html.AppendLine($"<li>{item}</li>");
        }

        html.AppendLine("</ul></section>");
    }

    private static void AppendRelationshipConstellation(StringBuilder html, TranscriptReportViewModel report)
    {
        html.AppendLine("<section class='report-section relationship-constellation' data-section='relationships'>");
        html.AppendLine("<div class='section-number'>Section 05</div>");
        html.AppendLine("<h2 class='section-title'>Relationships</h2>");

        if (report.Relationships.Count == 0)
        {
            html.AppendLine("<div class='statement-card'>No relationships extracted.</div>");
        }
        else
        {
            var relationship = report.Relationships[0];
            html.AppendLine($"<div class='relationship-node' style='left:18px;top:110px;'>{WebUtility.HtmlEncode(relationship.SubjectName)}</div>");
            html.AppendLine($"<div class='relationship-edge' style='left:175px;top:84px;'>{WebUtility.HtmlEncode(relationship.RelationshipType)}</div>");
            html.AppendLine($"<div class='relationship-node' style='right:18px;top:110px;'>{WebUtility.HtmlEncode(relationship.ObjectName)}</div>");
        }

        html.AppendLine("</section>");
    }

    private static void AppendLocationMapPanel(StringBuilder html, TranscriptReportViewModel report)
    {
        html.AppendLine("<section class='report-section location-map-panel' data-section='locations'>");
        html.AppendLine("<div>");
        html.AppendLine("<div class='section-number'>Section 06</div>");
        html.AppendLine("<h2 class='section-title'>Key Locations</h2>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='map-canvas'>");
        if (report.VerifiedMapLocations.Count == 0)
        {
            html.AppendLine("<div class='map-unavailable'>No verified map locations available.</div>");
        }
        else
        {
            html.AppendLine("<div class='map-road' style='left:24px;top:74px;width:420px;height:10px;transform:rotate(2deg);'></div>");
            html.AppendLine("<div class='map-road' style='left:64px;top:222px;width:360px;height:10px;transform:rotate(-8deg);'></div>");
            html.AppendLine("<div class='map-road' style='left:246px;top:22px;width:10px;height:240px;'></div>");

            foreach (var location in report.VerifiedMapLocations)
            {
                var index = location.MarkerNumber - 1;
                var markerColor = index switch
                {
                    0 => "#bf602f",
                    1 => "#d58c49",
                    _ => "#7f9355"
                };
                var left = index switch
                {
                    0 => "120px",
                    1 => "280px",
                    _ => "210px"
                };
                var top = index switch
                {
                    0 => "120px",
                    1 => "84px",
                    _ => "230px"
                };
                var labelLeft = index switch
                {
                    0 => "146px",
                    1 => "298px",
                    _ => "228px"
                };
                var labelTop = index switch
                {
                    0 => "96px",
                    1 => "58px",
                    _ => "242px"
                };

                html.AppendLine($"<div class='map-marker' style='left:{left};top:{top};background:{markerColor};'></div>");
                html.AppendLine($"<div class='map-label' style='left:{labelLeft};top:{labelTop};'>{WebUtility.HtmlEncode(location.Address)}</div>");
            }
        }

        html.AppendLine("</div>");
        html.AppendLine("<div class='location-data-store'>");
        foreach (var location in report.Locations.Select((value, index) => new { value, index }))
        {
            var markerLabel = "Text location";
            if (location.value.IsVerifiedAddress)
            {
                var markerNumber = report.VerifiedMapLocations
                    .FirstOrDefault(x => x.Name == location.value.Name && x.Address == location.value.Address)
                    ?.MarkerNumber ?? (location.index + 1);
                markerLabel = $"Marker {markerNumber}";
            }

            html.AppendLine($"<div class='location-summary' data-marker='{markerLabel}' data-name='{WebUtility.HtmlEncode(location.value.Name)}' data-address='{WebUtility.HtmlEncode(location.value.Address)}' data-verified='{location.value.IsVerifiedAddress.ToString().ToLowerInvariant()}'>");
            html.AppendLine($"<div class='section-number'>{markerLabel}</div>");
            html.AppendLine($"<div class='section-title' style='font-size:22px;margin-top:6px;'>{WebUtility.HtmlEncode(location.value.Name)}</div>");
            html.AppendLine($"<div class='location-address'>{WebUtility.HtmlEncode(location.value.Address)}</div>");
            html.AppendLine("</div>");
        }

        if (report.Locations.Count == 0)
        {
            html.AppendLine("<div class='location-summary'>No locations extracted.</div>");
        }

        html.AppendLine("</div>");
        html.AppendLine("</section>");
    }
}
