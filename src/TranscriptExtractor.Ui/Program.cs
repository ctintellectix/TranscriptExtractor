using TranscriptExtractor.Ui.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient<TranscriptExtractorApiClient>((sp, httpClient) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiBaseUrl = configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("Configuration value 'ApiBaseUrl' is required.");

    httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

public partial class Program;
