using System.Text;

namespace TranscriptExtractor.Core.Reports;

public sealed class StubTranscriptPdfRenderer : ITranscriptPdfRenderer
{
    public byte[] RenderPdf(string html, string templateVersion)
    {
        return Encoding.UTF8.GetBytes($"PDF-STUB:{templateVersion}\n{html}");
    }
}
