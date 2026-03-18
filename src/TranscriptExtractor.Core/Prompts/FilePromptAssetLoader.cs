namespace TranscriptExtractor.Core.Prompts;

public sealed class FilePromptAssetLoader(string version) : IPromptAssetLoader
{
    public PromptAssets Load(string promptDirectory)
    {
        var systemPromptPath = Path.Combine(promptDirectory, "system.txt");
        var userPromptPath = Path.Combine(promptDirectory, "user.txt");
        var schemaPath = Path.Combine(promptDirectory, "schema.json.txt");

        return new PromptAssets(
            Version: version,
            SystemPrompt: File.ReadAllText(systemPromptPath),
            UserPromptTemplate: File.ReadAllText(userPromptPath),
            SchemaText: File.ReadAllText(schemaPath));
    }
}
