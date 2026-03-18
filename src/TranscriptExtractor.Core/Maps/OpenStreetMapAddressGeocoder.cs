using System.Net.Http.Json;

namespace TranscriptExtractor.Core.Maps;

public sealed class OpenStreetMapAddressGeocoder(HttpClient httpClient) : IAddressGeocoder
{
    public async Task<GeocodedAddress?> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q={Uri.EscapeDataString(address)}");
        request.Headers.UserAgent.ParseAdd("TranscriptExtractor/1.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken);
        var match = payload?.FirstOrDefault();
        if (match is null)
        {
            return null;
        }

        return double.TryParse(match.Lat, out var latitude) &&
               double.TryParse(match.Lon, out var longitude)
            ? new GeocodedAddress(latitude, longitude)
            : null;
    }

    private sealed class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}
