namespace TranscriptExtractor.Core.Extraction;

public interface ITranscriptExtractionClient
{
    Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken);
}
