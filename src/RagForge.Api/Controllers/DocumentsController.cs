using Microsoft.AspNetCore.Mvc;
using RagForge.Application.DTOs.Documents;
using RagForge.Application.UseCases.Documents.Interfaces;

namespace RagForge.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(IIngestDocumentUseCase ingestDocumentUseCase) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IngestDocumentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngestDocumentResponseDto>> Ingest(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await ingestDocumentUseCase.ExecuteAsync(
            new IngestDocumentRequestDto
            {
                Name = file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "text/plain" : file.ContentType,
                Length = file.Length,
                ContentStream = stream
            },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
