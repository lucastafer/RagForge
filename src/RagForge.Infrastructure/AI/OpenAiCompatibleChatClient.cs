using Microsoft.Extensions.Options;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;

namespace RagForge.Infrastructure.AI;

/// <summary>Chat through the OpenAI <c>/chat/completions</c> format, used for OpenAI and DeepSeek.</summary>
internal sealed class OpenAiCompatibleChatClient(
    HttpClient httpClient,
    IOptions<ChatOptions> options) : IChatService
{
    private readonly ChatOptions _options = options.Value;

    public string ModelName => _options.ResolvedModel;

    public async Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<RetrievedChunk> context,
        CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages =
        [
            new("system", GroundedPrompt.System),
            new("user", GroundedPrompt.BuildUserMessage(question, context))
        ];

        var payload = await OpenAiCompatibleHttp.PostAsync<ChatCompletionResponse>(
            httpClient,
            "chat/completions",
            _options.ApiKey,
            new ChatCompletionRequest(_options.ResolvedModel, messages, _options.Temperature),
            ChatOptions.SectionName,
            cancellationToken);

        var answer = payload.Choices.FirstOrDefault()?.Message.Content?.Trim();
        return !string.IsNullOrWhiteSpace(answer)
            ? answer
            : throw new InvalidOperationException("The chat provider returned no answer.");
    }

    private sealed record ChatCompletionRequest(string Model, IReadOnlyList<ChatMessage> Messages, double? Temperature);

    private sealed record ChatMessage(string Role, string? Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
