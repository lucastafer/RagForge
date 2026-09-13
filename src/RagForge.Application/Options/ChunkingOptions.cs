namespace RagForge.Application.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Rag:Chunking";

    public int ChunkSize { get; init; } = 1_000;

    public int Overlap { get; init; } = 150;
}
