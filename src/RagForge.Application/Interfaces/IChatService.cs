using RagForge.Application.Models;

namespace RagForge.Application.Interfaces;

public interface IChatService
{
    Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<RetrievedChunk> context,
        CancellationToken cancellationToken = default);
}
