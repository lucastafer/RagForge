namespace RagForge.Application.DTOs.Questions;

public sealed class AskQuestionResponseDto
{
    public string Answer { get; set; } = string.Empty;

    // Null when no chunk was relevant enough and the chat model was not called.
    public string? ChatModel { get; set; }

    public IReadOnlyList<SourceChunkDto> Sources { get; set; } = [];
}
