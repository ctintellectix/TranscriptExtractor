using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;

namespace TranscriptExtractor.Core.Maps;

public sealed class OpenStreetMapStaticMapRenderer(HttpClient httpClient) : IStaticMapRenderer
{
    private const int TileSize = 256;

    public async Task<byte[]?> RenderAsync(StaticMapRequest request, CancellationToken cancellationToken)
    {
        if (request.Pins.Count == 0)
        {
            return null;
        }

        using var map = await BuildMapAsync(request, cancellationToken);
        using var output = new MemoryStream();
        await map.SaveAsync(output, PngFormat.Instance, cancellationToken);
        return output.ToArray();
    }

    private async Task<Image<Rgba32>> BuildMapAsync(StaticMapRequest request, CancellationToken cancellationToken)
    {
        var zoom = 14;
        var centerLat = request.Pins.Average(x => x.Latitude);
        var centerLon = request.Pins.Average(x => x.Longitude);

        var centerWorld = ToWorldPixel(centerLat, centerLon, zoom);
        var topLeftWorldX = centerWorld.X - (request.Width / 2d);
        var topLeftWorldY = centerWorld.Y - (request.Height / 2d);
        var minTileX = (int)Math.Floor(topLeftWorldX / TileSize);
        var minTileY = (int)Math.Floor(topLeftWorldY / TileSize);
        var maxTileX = (int)Math.Floor((topLeftWorldX + request.Width) / TileSize);
        var maxTileY = (int)Math.Floor((topLeftWorldY + request.Height) / TileSize);

        var canvas = new Image<Rgba32>(request.Width, request.Height, Color.White);

        for (var tileX = minTileX; tileX <= maxTileX; tileX++)
        {
            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                var tileBytes = await GetTileAsync(tileX, tileY, zoom, cancellationToken);
                if (tileBytes is null)
                {
                    continue;
                }

                using var tile = Image.Load<Rgba32>(tileBytes);
                var drawX = (int)Math.Round((tileX * TileSize) - topLeftWorldX);
                var drawY = (int)Math.Round((tileY * TileSize) - topLeftWorldY);
                canvas.Mutate(x => x.DrawImage(tile, new Point(drawX, drawY), 1f));
            }
        }

        foreach (var pin in request.Pins)
        {
            var pinWorld = ToWorldPixel(pin.Latitude, pin.Longitude, zoom);
            var pinX = (int)Math.Round(pinWorld.X - topLeftWorldX);
            var pinY = (int)Math.Round(pinWorld.Y - topLeftWorldY);
            DrawPin(canvas, pinX, pinY);
            DrawAddressLabel(canvas, pinX, pinY, pin.Address);
        }

        return canvas;
    }

    private async Task<byte[]?> GetTileAsync(int tileX, int tileY, int zoom, CancellationToken cancellationToken)
    {
        var maxIndex = 1 << zoom;
        if (tileX < 0 || tileY < 0 || tileX >= maxIndex || tileY >= maxIndex)
        {
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://tile.openstreetmap.org/{zoom}/{tileX}/{tileY}.png");
        request.Headers.UserAgent.ParseAdd("TranscriptExtractor/1.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static (double X, double Y) ToWorldPixel(double latitude, double longitude, int zoom)
    {
        var sinLatitude = Math.Sin(latitude * Math.PI / 180d);
        var scale = TileSize * Math.Pow(2, zoom);
        var x = ((longitude + 180d) / 360d) * scale;
        var y = (0.5d - (Math.Log((1d + sinLatitude) / (1d - sinLatitude)) / (4d * Math.PI))) * scale;
        return (x, y);
    }

    private static void DrawPin(Image<Rgba32> image, int centerX, int centerY)
    {
        const int radius = 8;
        var outer = new Rgba32(255, 255, 255, 255);
        var inner = new Rgba32(191, 96, 47, 255);

        for (var y = -radius - 2; y <= radius + 2; y++)
        {
            for (var x = -radius - 2; x <= radius + 2; x++)
            {
                var targetX = centerX + x;
                var targetY = centerY + y;

                if (targetX < 0 || targetY < 0 || targetX >= image.Width || targetY >= image.Height)
                {
                    continue;
                }

                var distanceSquared = (x * x) + (y * y);
                if (distanceSquared <= (radius + 2) * (radius + 2))
                {
                    image[targetX, targetY] = outer;
                }

                if (distanceSquared <= radius * radius)
                {
                    image[targetX, targetY] = inner;
                }
            }
        }
    }

    private static void DrawAddressLabel(Image<Rgba32> image, int pinX, int pinY, string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var font = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
        var paddingX = 12;
        var paddingY = 8;
        var position = new PointF(
            Math.Clamp(pinX + 18, 8, Math.Max(8, image.Width - 240)),
            Math.Clamp(pinY - 34, 8, Math.Max(8, image.Height - 40)));

        var textSize = TextMeasurer.MeasureSize(address, new TextOptions(font));
        var boxWidth = MathF.Min(textSize.Width + (paddingX * 2), image.Width - position.X - 8);
        var boxHeight = textSize.Height + (paddingY * 2);
        var rectangle = new RectangularPolygon(position.X, position.Y, boxWidth, boxHeight);

        image.Mutate(ctx =>
        {
            ctx.Fill(new Rgba32(255, 248, 236, 235), rectangle);
            ctx.Draw(Pens.Solid(new Rgba32(234, 215, 189, 255), 1), rectangle);
            ctx.DrawText(
                address,
                font,
                new Rgba32(91, 71, 56, 255),
                new PointF(position.X + paddingX, position.Y + paddingY));
        });
    }
}
