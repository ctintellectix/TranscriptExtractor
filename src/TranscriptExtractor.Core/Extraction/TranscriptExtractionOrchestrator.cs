using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core.Entities;
using TranscriptExtractor.Core.Prompts;

namespace TranscriptExtractor.Core.Extraction;

public sealed class TranscriptExtractionOrchestrator(
    TranscriptExtractorDbContext db,
    IPromptAssetLoader promptAssetLoader,
    ITranscriptExtractionClient extractionClient,
    string promptDirectory)
{
    public async Task<TranscriptExtractionProcessResult> ProcessOneWithResultAsync(CancellationToken cancellationToken)
    {
        var job = await db.ExtractionJobs
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Status == ExtractionJobStatus.Queued, cancellationToken);

        if (job is null)
        {
            return TranscriptExtractionProcessResult.Idle;
        }

        job.MarkProcessing();
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var transcript = await db.Transcripts.FindAsync([job.TranscriptId], cancellationToken);
            if (transcript is null)
            {
                job.MarkFailed("Transcript not found.");
                await db.SaveChangesAsync(cancellationToken);
                return TranscriptExtractionProcessResult.Failure("Transcript not found.");
            }

            var assets = promptAssetLoader.Load(promptDirectory);
            var request = ExtractionRequestBuilder.Build(transcript, assets);
            var result = await extractionClient.ExtractAsync(request, cancellationToken);

            var document = new ExtractionDocument(transcript.Id, job.Id, result.Json)
            {
                Model = result.Model,
                PromptVersion = assets.Version
            };

            db.ExtractionDocuments.Add(document);
            job.MarkCompleted(result.Model, assets.Version);
            await db.SaveChangesAsync(cancellationToken);
            return TranscriptExtractionProcessResult.Success();
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message);
            await db.SaveChangesAsync(cancellationToken);
            return TranscriptExtractionProcessResult.Failure(ex.Message);
        }
    }

    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        var result = await ProcessOneWithResultAsync(cancellationToken);
        return result.Processed;
    }
}

public sealed record TranscriptExtractionProcessResult(bool Processed, bool Succeeded, string? ErrorMessage)
{
    public static TranscriptExtractionProcessResult Idle { get; } = new(false, false, null);

    public static TranscriptExtractionProcessResult Success() => new(true, true, null);

    public static TranscriptExtractionProcessResult Failure(string errorMessage) => new(true, false, errorMessage);
}
