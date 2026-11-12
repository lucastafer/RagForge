using Microsoft.AspNetCore.Mvc;
using RagForge.Application.DTOs.Documents;
using RagForge.Application.UseCases.Documents.Interfaces;

namespace RagForge.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    IIngestDocumentUseCase ingestDocumentUseCase,
    IListDocumentsUseCase listDocumentsUseCase) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await listDocumentsUseCase.ExecuteAsync(cancellationToken));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IngestDocumentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<IngestDocumentResponseDto>(StatusCodes.Status200OK)]
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

        return result.AlreadyIndexed ? Ok(result) : StatusCode(StatusCodes.Status201Created, result);
    }
}
