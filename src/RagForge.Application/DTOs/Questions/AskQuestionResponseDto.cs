namespace RagForge.Application.DTOs.Questions;

public sealed class AskQuestionResponseDto
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<SourceChunkDto> Sources { get; set; } = [];
}
