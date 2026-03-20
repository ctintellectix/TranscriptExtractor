using TranscriptExtractor.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
builder.Services.Configure<PromptAssetOptions>(builder.Configuration.GetSection("PromptAssets"));
builder.Services.Configure<OpenAiExtractionOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<IPromptAssetLoader>(sp =>
{
    var options = sp.GetRequiredService<IOptions<PromptAssetOptions>>().Value;
    return new FilePromptAssetLoader(options.Version);
});
builder.Services.AddHttpClient<ITranscriptExtractionClient, OpenAiTranscriptExtractionClient>((sp, httpClient) =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiExtractionOptions>>().Value;

    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    }
});
builder.Services.AddSingleton(new WorkerIdentity(
    builder.Environment.ApplicationName ?? "TranscriptExtractor.Worker"));
builder.Services.AddScoped(sp => new TranscriptExtractionOrchestrator(
    sp.GetRequiredService<TranscriptExtractorDbContext>(),
    sp.GetRequiredService<IPromptAssetLoader>(),
    sp.GetRequiredService<ITranscriptExtractionClient>(),
    ResolvePromptDirectory(
        builder.Environment.ContentRootPath,
        sp.GetRequiredService<IOptions<PromptAssetOptions>>().Value.Directory)));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static string ResolvePromptDirectory(string contentRootPath, string configuredDirectory)
{
    if (Path.IsPathRooted(configuredDirectory))
    {
        return configuredDirectory;
    }

    return Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", configuredDirectory));
}
