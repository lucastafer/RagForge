namespace RagForge.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";

    public string ApiKey { get; init; } = string.Empty;

    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    public string ChatModel { get; init; } = "gpt-4o-mini";

    public double Temperature { get; init; } = 0.1;

    public int TimeoutSeconds { get; init; } = 60;
}
