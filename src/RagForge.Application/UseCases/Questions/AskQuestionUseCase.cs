using RagForge.Application.DTOs.Questions;
using RagForge.Application.Interfaces;
using RagForge.Application.UseCases.Questions.Interfaces;

namespace RagForge.Application.UseCases.Questions;

public sealed class AskQuestionUseCase(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IChatService chatService) : IAskQuestionUseCase
{
    public async Task<AskQuestionResponseDto> ExecuteAsync(
        AskQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);

        if (request.TopK is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TopK), "TopK must be between 1 and 10.");
        }

        var vectors = await embeddingService.GenerateAsync([request.Question.Trim()], cancellationToken);
        var context = await vectorStore.SearchAsync(vectors[0], request.TopK, cancellationToken);

        var answer = context.Count == 0
            ? "I could not find relevant information in the indexed documents."
            : await chatService.GenerateGroundedAnswerAsync(request.Question.Trim(), context, cancellationToken);

        return new AskQuestionResponseDto
        {
            Answer = answer,
            Sources = context.Select(chunk => new SourceChunkDto
            {
                ChunkId = chunk.ChunkId,
                DocumentId = chunk.DocumentId,
                DocumentName = chunk.DocumentName,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                Similarity = chunk.Similarity
            }).ToArray()
        };
    }
}
