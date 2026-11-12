using RagForge.Application.DTOs.Documents;
using RagForge.Application.Interfaces;
using RagForge.Application.UseCases.Documents.Interfaces;

namespace RagForge.Application.UseCases.Documents;

public sealed class ListDocumentsUseCase(IVectorStore vectorStore) : IListDocumentsUseCase
{
    public async Task<IReadOnlyList<DocumentSummaryDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var documents = await vectorStore.ListDocumentsAsync(cancellationToken);

        return documents
            .Select(document => new DocumentSummaryDto
            {
                DocumentId = document.Id,
                Name = document.Name,
                ContentType = document.ContentType,
                ChunkCount = document.ChunkCount,
                CreatedAt = document.CreatedAt
            })
            .ToArray();
    }
}
