namespace RagForge.Application.DTOs.Questions;

public sealed class SourceChunkDto
{
    public Guid ChunkId { get; set; }

    public Guid DocumentId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public double Similarity { get; set; }
}
