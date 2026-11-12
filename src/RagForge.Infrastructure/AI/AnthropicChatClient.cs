using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using RagForge.Application.Exceptions;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;

namespace RagForge.Infrastructure.AI;

/// <summary>Chat through the Claude Messages API, using the official Anthropic SDK.</summary>
internal sealed class AnthropicChatClient(
    IAnthropicClient client,
    IOptions<ChatOptions> options) : IChatService
{
    // Thinking tokens count toward this limit, so it leaves room well beyond a concise answer.
    private const int MaxTokens = 16_000;

    private readonly ChatOptions _options = options.Value;

    public string ModelName => _options.ResolvedModel;

    public async Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<RetrievedChunk> context,
        CancellationToken cancellationToken = default)
    {
        // Temperature is intentionally not sent: current Claude models reject sampling parameters.
        var parameters = new MessageCreateParams
        {
            Model = _options.ResolvedModel,
            MaxTokens = MaxTokens,
            System = GroundedPrompt.System,
            Messages = [new UserMessageParam { Content = GroundedPrompt.BuildUserMessage(question, context) }]
        };

        Message response;
        try
        {
            response = await client.Messages.Create(parameters, cancellationToken: cancellationToken);
        }
        catch (AnthropicApiException exception)
        {
            throw new AiProviderException(
                ChatOptions.SectionName,
                exception.StatusCode,
                $"The Chat provider returned status {(int)exception.StatusCode}: {exception.Message}",
                exception);
        }
        catch (AnthropicIOException exception)
        {
            throw new AiProviderException(
                ChatOptions.SectionName,
                null,
                $"The Chat provider could not be reached: {exception.Message}",
                exception);
        }

        if (response.StopReason == StopReason.Refusal)
        {
            throw new AiProviderException(ChatOptions.SectionName, null, "The chat model declined to answer this request.");
        }

        var answer = string.Concat(response.Content.Select(block => block.Value).OfType<TextBlock>().Select(text => text.Text)).Trim();
        return !string.IsNullOrWhiteSpace(answer)
            ? answer
            : throw new InvalidOperationException("The chat provider returned no answer.");
    }
}
