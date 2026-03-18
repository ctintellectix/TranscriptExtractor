using QuestPDF.Infrastructure;
using TranscriptExtractor.Core.Maps;
using TranscriptExtractor.Core.Reports;

namespace TranscriptExtractor.Tests.Reports;

public class VerifiedAddressMapTests
{
    [Fact]
    public void RenderPdf_UsesVerifiedAddressMapImageWhenLocationsAreMappable()
    {
        var geocoder = new FakeAddressGeocoder(new GeocodedAddress(36.1699, -115.1398));
        var staticMapRenderer = new FakeStaticMapRenderer();
        var renderer = new QuestTranscriptPdfRenderer(geocoder, staticMapRenderer);

        var html = TranscriptReportHtmlRenderer.Render(
            new TranscriptReportViewModel
            {
                SourceType = "witness_interview",
                Locations =
                {
                    new TranscriptLocationViewModel
                    {
                        Name = "Marcus Residence",
                        Address = "1427 Walnut Street, Las Vegas, NV",
                        IsVerifiedAddress = true
                    }
                },
                VerifiedMapLocations =
                {
                    new TranscriptMapLocationViewModel
                    {
                        Name = "Marcus Residence",
                        Address = "1427 Walnut Street, Las Vegas, NV",
                        MarkerNumber = 1
                    }
                }
            },
            ReportTemplateVersion.Current);

        var bytes = renderer.RenderPdf(html, ReportTemplateVersion.Current);

        Assert.NotNull(staticMapRenderer.LastRequest);
        Assert.Single(staticMapRenderer.LastRequest!.Pins);
        Assert.Equal("1427 Walnut Street, Las Vegas, NV", staticMapRenderer.LastRequest.Pins[0].Address);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private sealed class FakeAddressGeocoder(GeocodedAddress? result) : IAddressGeocoder
    {
        public Task<GeocodedAddress?> GeocodeAsync(string address, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class FakeStaticMapRenderer : IStaticMapRenderer
    {
        private static readonly byte[] OnePixelPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9sot8mcAAAAASUVORK5CYII=");

        public StaticMapRequest? LastRequest { get; private set; }

        public Task<byte[]?> RenderAsync(StaticMapRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult<byte[]?>(OnePixelPng);
        }
    }
}
