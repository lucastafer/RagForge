using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;

namespace RagForge.Infrastructure.AI;

internal sealed class OpenAiCompatibleClient(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options) : IEmbeddingService, IChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenAiOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
        {
            return [];
        }

        using var request = CreateRequest(
            HttpMethod.Post,
            "embeddings",
            JsonContent.Create(new EmbeddingRequest(_options.EmbeddingModel, inputs), options: JsonOptions));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("The embedding provider returned an empty response.");

        var vectors = payload.Data
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding)
            .ToArray();

        if (vectors.Any(vector => vector.Length == 0))
        {
            throw new InvalidOperationException("The embedding provider returned an empty vector.");
        }

        return vectors;
    }

    public async Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<RetrievedChunk> context,
        CancellationToken cancellationToken = default)
    {
        var contextText = string.Join(
            "\n\n",
            context.Select((chunk, index) =>
                $"[Source {index + 1}: {chunk.DocumentName}, chunk {chunk.ChunkIndex}]\n{chunk.Content}"));

        var messages = new[]
        {
            new ChatMessage(
                "system",
                "Answer only from the supplied sources. If the sources do not contain the answer, say that the indexed documents do not provide enough information. Be concise and cite sources inline as [Source N]."),
            new ChatMessage(
                "user",
                $"Question:\n{question}\n\nSources:\n{contextText}")
        };

        using var request = CreateRequest(
            HttpMethod.Post,
            "chat/completions",
            JsonContent.Create(
                new ChatCompletionRequest(_options.ChatModel, messages, _options.Temperature),
                options: JsonOptions));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("The chat provider returned an empty response.");

        var answer = payload.Choices.FirstOrDefault()?.Message.Content?.Trim();
        return !string.IsNullOrWhiteSpace(answer)
            ? answer
            : throw new InvalidOperationException("The chat provider returned no answer.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, HttpContent content)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        const int maximumLength = 500;
        var safeBody = body.Length <= maximumLength ? body : body[..maximumLength];
        throw new HttpRequestException(
            $"AI provider request failed with status {(int)response.StatusCode}: {safeBody}",
            null,
            response.StatusCode);
    }

    private sealed record EmbeddingRequest(string Model, IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingItem> Data);

    private sealed record EmbeddingItem(int Index, float[] Embedding);

    private sealed record ChatCompletionRequest(string Model, IReadOnlyList<ChatMessage> Messages, double Temperature);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
