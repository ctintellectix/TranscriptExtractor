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
    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        var job = await db.ExtractionJobs
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Status == ExtractionJobStatus.Queued, cancellationToken);

        if (job is null)
        {
            return false;
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
                return true;
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
            return true;
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
