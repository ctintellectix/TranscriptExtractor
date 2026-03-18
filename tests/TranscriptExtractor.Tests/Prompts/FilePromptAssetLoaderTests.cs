using TranscriptExtractor.Core.Prompts;

namespace TranscriptExtractor.Tests.Prompts;

public class FilePromptAssetLoaderTests
{
    [Fact]
    public void Load_LoadsPromptAssetsFromDirectory()
    {
        var rootDirectory = FindProjectRoot();
        var promptDirectory = Path.Combine(rootDirectory, "prompts", "lvpd");
        var loader = new FilePromptAssetLoader("lvpd-v1");

        var assets = loader.Load(promptDirectory);

        Assert.Equal("lvpd-v1", assets.Version);
        Assert.Contains("information extraction system", assets.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<<<TRANSCRIPT_TEXT>>>", assets.UserPromptTemplate, StringComparison.Ordinal);
        Assert.Contains("\"transcript_metadata\"", assets.SchemaText, StringComparison.Ordinal);
    }

    private static string FindProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TranscriptExtractor.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }
}
