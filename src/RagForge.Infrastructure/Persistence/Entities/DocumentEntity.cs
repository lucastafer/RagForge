namespace RagForge.Infrastructure.Persistence.Entities;

internal sealed class DocumentEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string ContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<DocumentChunkEntity> Chunks { get; set; } = [];
}
