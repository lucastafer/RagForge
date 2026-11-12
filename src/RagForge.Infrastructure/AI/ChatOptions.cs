namespace RagForge.Infrastructure.AI;

public enum ChatProvider
{
    OpenAI,
    Anthropic,
    DeepSeek
}

public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    public ChatProvider Provider { get; init; } = ChatProvider.OpenAI;

    public string ApiKey { get; init; } = string.Empty;

    // Empty values fall back to the provider defaults below.
    public string? Model { get; init; }

    public string? BaseUrl { get; init; }

    // Only sent to OpenAI-compatible providers, and only when set: reasoning models reject it.
    public double? Temperature { get; init; }

    public int TimeoutSeconds { get; init; } = 120;

    public string ResolvedModel => !string.IsNullOrWhiteSpace(Model)
        ? Model
        : Provider switch
        {
            ChatProvider.Anthropic => "claude-opus-5-5",
            ChatProvider.DeepSeek => "deepseek-v4-pro",
            _ => "gpt-4o-mini"
        };

    // Null for Anthropic means the SDK default (https://api.anthropic.com).
    public string? ResolvedBaseUrl => !string.IsNullOrWhiteSpace(BaseUrl)
        ? BaseUrl
        : Provider switch
        {
            ChatProvider.Anthropic => null,
            ChatProvider.DeepSeek => "https://api.deepseek.com/",
            _ => "https://api.openai.com/v1/"
        };
}
