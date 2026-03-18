namespace TranscriptExtractor.Core.Maps;

public interface IStaticMapRenderer
{
    Task<byte[]?> RenderAsync(StaticMapRequest request, CancellationToken cancellationToken);
}
