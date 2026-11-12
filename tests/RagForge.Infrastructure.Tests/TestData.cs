using RagForge.Application.Models;

namespace RagForge.Infrastructure.Tests;

internal static class TestData
{
    public static IReadOnlyList<RetrievedChunk> Context { get; } =
        [new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "rag.md", 0, "RAG grounds answers.", 0.9)];
}
