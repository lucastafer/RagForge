using System.ComponentModel.DataAnnotations;

namespace RagForge.Application.DTOs.Questions;

public sealed class AskQuestionRequestDto
{
    [Required]
    [MinLength(2)]
    [MaxLength(2_000)]
    public string Question { get; set; } = string.Empty;

    [Range(1, 10)]
    public int TopK { get; set; } = 5;
}
