using RagForge.Application.DTOs.Questions;

namespace RagForge.Application.UseCases.Questions.Interfaces;

public interface IAskQuestionUseCase
{
    Task<AskQuestionResponseDto> ExecuteAsync(
        AskQuestionRequestDto request,
        CancellationToken cancellationToken = default);
}
