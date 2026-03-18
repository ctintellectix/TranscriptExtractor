using TranscriptExtractor.Core.Maps;
using TranscriptExtractor.Core.Reports;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: TranscriptExtractor.SampleRenderer <input-json-path> <output-pdf-path>");
    return 1;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input JSON file not found: {inputPath}");
    return 2;
}

var json = await File.ReadAllTextAsync(inputPath);
var report = TranscriptReportComposer.Compose(json);
var html = TranscriptReportHtmlRenderer.Render(report, ReportTemplateVersion.Current);
using var geocoderClient = new HttpClient();
using var staticMapClient = new HttpClient();
var renderer = new QuestTranscriptPdfRenderer(
    new OpenStreetMapAddressGeocoder(geocoderClient),
    new OpenStreetMapStaticMapRenderer(staticMapClient));
var pdfBytes = renderer.RenderPdf(html, ReportTemplateVersion.Current);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllBytesAsync(outputPath, pdfBytes);

Console.WriteLine($"PDF written to: {outputPath}");
return 0;
