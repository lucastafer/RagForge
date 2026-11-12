using RagForge.Application.Models;

namespace RagForge.Infrastructure.AI;

internal static class GroundedPrompt
{
    public const string System =
        "Answer only from the supplied sources. If the sources do not contain the answer, say that the indexed documents do not provide enough information. Be concise and cite sources inline as [Source N]. Reply in the same language as the question.";

    public static string BuildUserMessage(string question, IReadOnlyList<RetrievedChunk> context)
    {
        var sources = string.Join(
            "\n\n",
            context.Select((chunk, index) =>
                $"[Source {index + 1}: {chunk.DocumentName}, chunk {chunk.ChunkIndex}]\n{chunk.Content}"));

        return $"Question:\n{question}\n\nSources:\n{sources}";
    }
}
