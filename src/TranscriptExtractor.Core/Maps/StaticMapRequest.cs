namespace TranscriptExtractor.Core.Maps;

public sealed class StaticMapRequest
{
    public int Width { get; init; } = 640;
    public int Height { get; init; } = 360;
    public IReadOnlyList<StaticMapPin> Pins { get; init; } = Array.Empty<StaticMapPin>();
}

public sealed record StaticMapPin(
    int MarkerNumber,
    string Name,
    string Address,
    double Latitude,
    double Longitude);

public sealed record GeocodedAddress(
    double Latitude,
    double Longitude);
