namespace TranscriptExtractor.Core.Prompts;

public interface IPromptAssetLoader
{
    PromptAssets Load(string promptDirectory);
}
