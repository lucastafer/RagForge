namespace RagForge.Application.DTOs.Documents;

public sealed class DocumentSummaryDto
{
    public Guid DocumentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
