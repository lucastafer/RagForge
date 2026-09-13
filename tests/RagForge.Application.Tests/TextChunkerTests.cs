using RagForge.Application.Options;
using RagForge.Application.Services;
using Xunit;

namespace RagForge.Application.Tests;

public sealed class TextChunkerTests
{
    [Fact]
    public void Split_ReturnsSingleChunk_WhenTextFits()
    {
        var chunker = new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 20 });

        var chunks = chunker.Split("A short document.");

        var chunk = Assert.Single(chunks);
        Assert.Equal(0, chunk.Index);
        Assert.Equal("A short document.", chunk.Content);
    }

    [Fact]
    public void Split_CreatesOrderedOverlappingChunks()
    {
        var text = string.Join(' ', Enumerable.Repeat("knowledge", 40));
        var chunker = new TextChunker(new ChunkingOptions { ChunkSize = 120, Overlap = 20 });

        var chunks = chunker.Split(text);

        Assert.True(chunks.Count > 1);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.Index));
        Assert.All(chunks, chunk => Assert.InRange(chunk.Content.Length, 1, 120));
    }

    [Fact]
    public void Split_RejectsInvalidOverlap()
    {
        var chunker = new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 100 });

        var exception = Assert.Throws<InvalidOperationException>(() => chunker.Split("Valid input text."));

        Assert.Contains("Overlap", exception.Message, StringComparison.Ordinal);
    }
}
