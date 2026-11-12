using RagForge.Application.DTOs.Documents;

namespace RagForge.Application.UseCases.Documents.Interfaces;

public interface IListDocumentsUseCase
{
    Task<IReadOnlyList<DocumentSummaryDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
