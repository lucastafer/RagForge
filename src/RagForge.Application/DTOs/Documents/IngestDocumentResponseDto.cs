namespace RagForge.Application.DTOs.Documents;

public sealed class IngestDocumentResponseDto
{
    public Guid DocumentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public bool AlreadyIndexed { get; set; }
}
