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

    [Fact]
    public void Split_RejectsOverlapAboveHalfOfChunkSize()
    {
        var chunker = new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 60 });

        Assert.Throws<InvalidOperationException>(() => chunker.Split("Valid input text."));
    }

    [Fact]
    public void Split_AlwaysAdvances_WhenSentenceBoundariesShortenChunks()
    {
        var sentence = new string('a', 66) + ".";
        var text = string.Concat(Enumerable.Repeat(sentence, 30));
        var chunker = new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 50 });

        var chunks = chunker.Split(text);

        Assert.InRange(chunks.Count, 2, text.Length);
        Assert.EndsWith(".", chunks[^1].Content, StringComparison.Ordinal);
    }
}
