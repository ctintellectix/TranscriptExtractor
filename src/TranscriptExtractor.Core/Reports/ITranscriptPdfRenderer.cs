namespace TranscriptExtractor.Core.Reports;

public interface ITranscriptPdfRenderer
{
    byte[] RenderPdf(string html, string templateVersion);
}
