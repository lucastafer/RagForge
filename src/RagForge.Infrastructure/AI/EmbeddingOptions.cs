namespace RagForge.Infrastructure.AI;

/// <summary>
/// Settings for an OpenAI-compatible <c>/embeddings</c> endpoint. Embeddings are configured separately
/// from chat because several chat providers, such as Anthropic and DeepSeek, do not offer embeddings.
/// </summary>
public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "text-embedding-3-small";

    // OpenAI accepts at most 2,048 inputs per embeddings request; smaller batches also stay under the token limit.
    public int BatchSize { get; init; } = 100;

    public int TimeoutSeconds { get; init; } = 60;
}
