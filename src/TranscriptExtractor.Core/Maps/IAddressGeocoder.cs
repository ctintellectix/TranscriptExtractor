namespace TranscriptExtractor.Core.Maps;

public interface IAddressGeocoder
{
    Task<GeocodedAddress?> GeocodeAsync(string address, CancellationToken cancellationToken);
}
