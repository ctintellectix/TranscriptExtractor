using TranscriptExtractor.Core.Reports;

namespace TranscriptExtractor.Tests.Reports;

public class TranscriptReportHtmlRendererTests
{
    [Fact]
    public void Render_IncludesPrimarySectionsAndTemplateVersion()
    {
        var report = new TranscriptReportViewModel
        {
            SourceType = "witness_interview",
            Timeline =
            {
                new TranscriptTimelineItemViewModel
                {
                    Description = "Argument outside the residence.",
                    LocationName = "Michael Turner's Residence"
                }
            },
            Allegations =
            {
                new TranscriptAllegationViewModel
                {
                    Description = "Daniel Brooks threatened Emily Carter."
                }
            },
            Statements =
            {
                new TranscriptStatementViewModel
                {
                    SpeakerName = "Emily Carter",
                    Summary = "She saw the argument."
                }
            },
            Objects =
            {
                new TranscriptObjectViewModel
                {
                    Name = "Black SUV",
                    Type = "vehicle"
                }
            },
            Relationships =
            {
                new TranscriptRelationshipViewModel
                {
                    SubjectName = "Emily Carter",
                    RelationshipType = "neighbor",
                    ObjectName = "Daniel Brooks"
                }
            },
            Locations =
            {
                new TranscriptLocationViewModel
                {
                    Name = "Michael Turner's Residence",
                    Address = "1427 Walnut Street"
                }
            }
        };

        var html = TranscriptReportHtmlRenderer.Render(report, ReportTemplateVersion.Current);

        Assert.Contains("Timeline of Events", html, StringComparison.Ordinal);
        Assert.Contains("Allegations", html, StringComparison.Ordinal);
        Assert.Contains("Statements", html, StringComparison.Ordinal);
        Assert.Contains("Key Objects", html, StringComparison.Ordinal);
        Assert.Contains("Relationships", html, StringComparison.Ordinal);
        Assert.Contains("Key Locations", html, StringComparison.Ordinal);
        Assert.Contains(ReportTemplateVersion.Current, html, StringComparison.Ordinal);
    }
}
