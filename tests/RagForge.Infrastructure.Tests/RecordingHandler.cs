using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace RagForge.Infrastructure.Tests;

internal sealed class RecordingHandler(
    Func<JsonObject, string> respond,
    HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    public List<JsonObject> Requests { get; } = [];

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject();
        Requests.Add(body);
        LastRequest = request;

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(respond(body), Encoding.UTF8, "application/json")
        };
    }
}
