using TranscriptExtractor.Core.Entities;

namespace TranscriptExtractor.Tests.Core;

public class TranscriptEntityTests
{
    [Fact]
    public void NewTranscript_InitializesWithStableDefaults()
    {
        var transcript = new Transcript();

        Assert.NotEqual(Guid.Empty, transcript.Id);
        Assert.True(transcript.ReceivedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(string.Empty, transcript.TranscriptText);
        Assert.Equal(string.Empty, transcript.SourceType);
    }
}
