using TranscriptExtractor.Worker;
using Microsoft.EntityFrameworkCore;
using TranscriptExtractor.Core;
using TranscriptExtractor.Core.Extraction;
using TranscriptExtractor.Core.Persistence;
using TranscriptExtractor.Core.Prompts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<TranscriptExtractorDbContext>(options =>
    TranscriptExtractorDbContextConfigurator.ConfigurePostgres(
        options,
        builder.Configuration.GetConnectionString("TranscriptExtractor")
            ?? throw new InvalidOperationException("Connection string 'TranscriptExtractor' is required.")));
builder.Services.AddSingleton<IPromptAssetLoader>(_ =>
    new FilePromptAssetLoader("lvpd-v1"));
builder.Services.AddSingleton<ITranscriptExtractionClient, NotImplementedTranscriptExtractionClient>();
builder.Services.AddScoped(sp => new TranscriptExtractionOrchestrator(
    sp.GetRequiredService<TranscriptExtractorDbContext>(),
    sp.GetRequiredService<IPromptAssetLoader>(),
    sp.GetRequiredService<ITranscriptExtractionClient>(),
    Path.Combine(AppContext.BaseDirectory, "prompts", "lvpd")));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

internal sealed class NotImplementedTranscriptExtractionClient : ITranscriptExtractionClient
{
    public Task<TranscriptExtractionResult> ExtractAsync(ExtractionRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Transcript extraction client is not wired yet.");
    }
}
