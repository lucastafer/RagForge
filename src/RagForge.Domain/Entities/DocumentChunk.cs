using RagForge.Domain.Exceptions;

namespace RagForge.Domain.Entities;

public sealed class DocumentChunk
{
    internal DocumentChunk(Guid documentId, int index, string content)
    {
        if (index < 0)
        {
            throw new DomainException("Chunk index cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Chunk content is required.");
        }

        Id = Guid.NewGuid();
        DocumentId = documentId;
        Index = index;
        Content = content.Trim();
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public int Index { get; }

    public string Content { get; }
}
