using RagForge.Application.Models;
using RagForge.Domain.Entities;

namespace RagForge.Application.Interfaces;

public interface IVectorStore
{
    Task StoreDocumentAsync(
        Document document,
        IReadOnlyList<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] embedding,
        int limit,
        CancellationToken cancellationToken = default);
}
