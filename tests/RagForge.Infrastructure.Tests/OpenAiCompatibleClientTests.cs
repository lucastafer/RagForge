using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RagForge.Application.Exceptions;
using RagForge.Infrastructure.AI;
using Xunit;

namespace RagForge.Infrastructure.Tests;

public sealed class OpenAiCompatibleClientTests
{
    private const string ChatResponse = """{"choices":[{"message":{"role":"assistant","content":"Answer [Source 1]."}}]}""";

    [Fact]
    public async Task Embeddings_SplitInputsIntoBatchesAndKeepOrder()
    {
        var handler = new RecordingHandler(body =>
        {
            var inputs = body["input"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            var data = inputs
                .Select((input, index) => new { index, embedding = new[] { float.Parse(input) } })
                .Reverse();
            return JsonSerializer.Serialize(new { data });
        });
        var client = new OpenAiCompatibleEmbeddingClient(
            CreateHttpClient(handler, "https://embeddings.test/v1/"),
            Options.Create(new EmbeddingOptions { ApiKey = "embedding-key", BatchSize = 2 }));

        var vectors = await client.GenerateAsync(["1", "2", "3", "4", "5"]);

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal([1f, 2f, 3f, 4f, 5f], vectors.Select(vector => vector[0]));
        Assert.Equal("Bearer embedding-key", handler.LastRequest!.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Chat_OmitsTemperature_WhenNotConfigured()
    {
        var handler = new RecordingHandler(_ => ChatResponse);
        var client = CreateChatClient(handler, new ChatOptions { ApiKey = "chat-key" });

        var answer = await client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context);

        Assert.Equal("Answer [Source 1].", answer);
        var request = Assert.Single(handler.Requests);
        Assert.False(request.ContainsKey("temperature"));
        Assert.Equal("gpt-4o-mini", request["model"]!.GetValue<string>());
    }

    [Fact]
    public async Task Chat_UsesDeepSeekDefaults_WhenProviderIsDeepSeek()
    {
        var handler = new RecordingHandler(_ => ChatResponse);
        var options = new ChatOptions { Provider = ChatProvider.DeepSeek, ApiKey = "deepseek-key" };
        var client = CreateChatClient(handler, options);

        await client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context);

        Assert.Equal("deepseek-v4-pro", Assert.Single(handler.Requests)["model"]!.GetValue<string>());
        Assert.Equal("https://api.deepseek.com/chat/completions", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer deepseek-key", handler.LastRequest.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Chat_ThrowsProviderExceptionWithStatus_WhenRequestFails()
    {
        var handler = new RecordingHandler(_ => """{"error":{"message":"Incorrect API key"}}""", HttpStatusCode.Unauthorized);
        var client = CreateChatClient(handler, new ChatOptions { ApiKey = "invalid" });

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("Chat", exception.SettingsSection);
    }

    private static OpenAiCompatibleChatClient CreateChatClient(HttpMessageHandler handler, ChatOptions options) =>
        new(CreateHttpClient(handler, options.ResolvedBaseUrl!), Options.Create(options));

    private static HttpClient CreateHttpClient(HttpMessageHandler handler, string baseUrl) =>
        new(handler) { BaseAddress = new Uri(baseUrl) };
}
