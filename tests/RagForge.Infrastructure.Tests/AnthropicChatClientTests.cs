using System.Net;
using Anthropic;
using Microsoft.Extensions.Options;
using RagForge.Application.Exceptions;
using RagForge.Infrastructure.AI;
using Xunit;

namespace RagForge.Infrastructure.Tests;

public sealed class AnthropicChatClientTests
{
    [Fact]
    public async Task SendsMessagesRequestAndReturnsTextAfterThinking()
    {
        var handler = new RecordingHandler(_ => MessageResponse(
            "end_turn",
            """{"type":"thinking","thinking":"","signature":"sig"},{"type":"text","text":"Answer [Source 1]."}"""));
        var client = CreateClient(handler);

        var answer = await client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context);

        Assert.Equal("Answer [Source 1].", answer);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("claude-opus-5-5", request["model"]!.GetValue<string>());
        Assert.False(request.ContainsKey("temperature"));
        Assert.Contains("[Source 1: rag.md, chunk 0]", request["messages"]![0]!["content"]!.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("/v1/messages", handler.LastRequest!.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("Bearer anthropic-key", handler.LastRequest.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task ThrowsProviderException_WhenModelRefuses()
    {
        var handler = new RecordingHandler(_ => MessageResponse("refusal", ""));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context));

        Assert.Contains("declined", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThrowsProviderExceptionWithStatus_WhenKeyIsRejected()
    {
        var handler = new RecordingHandler(
            _ => """{"type":"error","error":{"type":"authentication_error","message":"invalid x-api-key"}}""",
            HttpStatusCode.Unauthorized);
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            client.GenerateGroundedAnswerAsync("What is RAG?", TestData.Context));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("Chat", exception.SettingsSection);
    }

    private static AnthropicChatClient CreateClient(HttpMessageHandler handler) =>
        new(
            new AnthropicClient { ApiKey = "anthropic-key", HttpClient = new HttpClient(handler), MaxRetries = 0 },
            Options.Create(new ChatOptions { Provider = ChatProvider.Anthropic, ApiKey = "anthropic-key" }));

    private static string MessageResponse(string stopReason, string content) =>
        $$"""
        {
          "id": "msg_test",
          "type": "message",
          "role": "assistant",
          "model": "claude-opus-5-5",
          "content": [{{content}}],
          "stop_reason": "{{stopReason}}",
          "stop_sequence": null,
          "usage": {"input_tokens": 10, "output_tokens": 5}
        }
        """;
}
