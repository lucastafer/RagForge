namespace RagForge.Application.Models;

public sealed record TextSegment(int Index, string Content);

public sealed record EmbeddedChunk(Guid Id, int Index, string Content, float[] Embedding);

public sealed record RetrievedChunk(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    int ChunkIndex,
    string Content,
    double Similarity);
