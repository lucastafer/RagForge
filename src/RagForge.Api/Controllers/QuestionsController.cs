using Microsoft.AspNetCore.Mvc;
using RagForge.Application.DTOs.Questions;
using RagForge.Application.UseCases.Questions.Interfaces;

namespace RagForge.Api.Controllers;

[ApiController]
[Route("api/questions")]
public sealed class QuestionsController(IAskQuestionUseCase askQuestionUseCase) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AskQuestionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AskQuestionResponseDto>> Ask(
        AskQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await askQuestionUseCase.ExecuteAsync(request, cancellationToken);

        return Ok(result);
    }
}
