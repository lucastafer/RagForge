using Microsoft.Extensions.Options;
using RagForge.Application.Interfaces;

namespace RagForge.Infrastructure.AI;

internal sealed class OpenAiCompatibleEmbeddingClient(
    HttpClient httpClient,
    IOptions<EmbeddingOptions> options) : IEmbeddingService
{
    private readonly EmbeddingOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        var vectors = new List<float[]>(inputs.Count);
        foreach (var batch in inputs.Chunk(_options.BatchSize))
        {
            vectors.AddRange(await GenerateBatchAsync(batch, cancellationToken));
        }

        return vectors;
    }

    private async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken)
    {
        var payload = await OpenAiCompatibleHttp.PostAsync<EmbeddingResponse>(
            httpClient,
            "embeddings",
            _options.ApiKey,
            new EmbeddingRequest(_options.Model, inputs),
            EmbeddingOptions.SectionName,
            cancellationToken);

        var vectors = payload.Data
            .OrderBy(item => item.Index)
            .Select(item => item.Embedding)
            .ToArray();

        if (vectors.Length != inputs.Count || vectors.Any(vector => vector.Length == 0))
        {
            throw new InvalidOperationException("The embedding provider returned missing or empty vectors.");
        }

        return vectors;
    }

    private sealed record EmbeddingRequest(string Model, IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingItem> Data);

    private sealed record EmbeddingItem(int Index, float[] Embedding);
}
