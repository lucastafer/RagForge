using RagForge.Application.DTOs.Documents;

namespace RagForge.Application.UseCases.Documents.Interfaces;

public interface IIngestDocumentUseCase
{
    Task<IngestDocumentResponseDto> ExecuteAsync(
        IngestDocumentRequestDto request,
        CancellationToken cancellationToken = default);
}
