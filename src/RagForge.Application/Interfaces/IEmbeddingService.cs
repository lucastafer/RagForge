namespace RagForge.Application.Interfaces;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}
