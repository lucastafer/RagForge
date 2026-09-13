using Pgvector;

namespace RagForge.Infrastructure.Persistence.Entities;

internal sealed class DocumentChunkEntity
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public required string Content { get; set; }

    public required Vector Embedding { get; set; }

    public DocumentEntity Document { get; set; } = null!;
}
