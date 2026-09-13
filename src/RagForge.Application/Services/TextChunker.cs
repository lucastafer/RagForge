using RagForge.Application.Models;
using RagForge.Application.Options;

namespace RagForge.Application.Services;

public sealed class TextChunker(ChunkingOptions options)
{
    public IReadOnlyList<TextSegment> Split(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (options.ChunkSize < 100)
        {
            throw new InvalidOperationException("ChunkSize must be at least 100 characters.");
        }

        if (options.Overlap < 0 || options.Overlap >= options.ChunkSize)
        {
            throw new InvalidOperationException("Overlap must be non-negative and smaller than ChunkSize.");
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var chunks = new List<TextSegment>();
        var start = 0;

        while (start < normalized.Length)
        {
            var remaining = normalized.Length - start;
            var length = Math.Min(options.ChunkSize, remaining);
            var end = start + length;

            if (end < normalized.Length)
            {
                end = FindNaturalBoundary(normalized, start, end);
            }

            var content = normalized[start..end].Trim();
            if (content.Length > 0)
            {
                chunks.Add(new TextSegment(chunks.Count, content));
            }

            if (end >= normalized.Length)
            {
                break;
            }

            var nextStart = Math.Max(0, end - options.Overlap);
            start = MoveToWordBoundary(normalized, nextStart, end);
        }

        return chunks;
    }

    private static int FindNaturalBoundary(string text, int start, int proposedEnd)
    {
        var minimumEnd = start + ((proposedEnd - start) * 2 / 3);
        var searchLength = proposedEnd - minimumEnd;
        var paragraphBreak = text.LastIndexOf("\n\n", proposedEnd - 1, searchLength, StringComparison.Ordinal);
        if (paragraphBreak >= minimumEnd)
        {
            return paragraphBreak + 2;
        }

        var sentenceBreak = text.LastIndexOfAny(['.', '!', '?', '\n'], proposedEnd - 1, searchLength);
        if (sentenceBreak >= minimumEnd)
        {
            return sentenceBreak + 1;
        }

        var whitespace = text.LastIndexOfAny([' ', '\t'], proposedEnd - 1, searchLength);
        return whitespace >= minimumEnd ? whitespace + 1 : proposedEnd;
    }

    private static int MoveToWordBoundary(string text, int nextStart, int previousEnd)
    {
        while (nextStart > 0 && nextStart < previousEnd && !char.IsWhiteSpace(text[nextStart - 1]))
        {
            nextStart++;
        }

        return nextStart;
    }
}
