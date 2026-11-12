using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;
using RagForge.Domain.Entities;
using RagForge.Infrastructure.Persistence.Entities;

namespace RagForge.Infrastructure.Persistence;

internal sealed class PgVectorStore(RagForgeDbContext dbContext) : IVectorStore
{
    public async Task StoreDocumentAsync(
        Document sourceDocument,
        IReadOnlyList<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var document = new DocumentEntity
        {
            Id = sourceDocument.Id,
            Name = sourceDocument.Name,
            ContentType = sourceDocument.ContentType,
            ContentHash = sourceDocument.ContentHash,
            CreatedAt = sourceDocument.CreatedAt,
            Chunks = chunks.Select(chunk => new DocumentChunkEntity
            {
                Id = chunk.Id,
                ChunkIndex = chunk.Index,
                Content = chunk.Content,
                Embedding = new Vector(chunk.Embedding)
            }).ToList()
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IndexedDocument?> FindByContentHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default) =>
        await SelectIndexedDocuments(dbContext.Documents.Where(document => document.ContentHash == contentHash))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(
        CancellationToken cancellationToken = default) =>
        await SelectIndexedDocuments(dbContext.Documents.OrderByDescending(document => document.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] embedding,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(embedding);
        var matches = await dbContext.DocumentChunks
            .AsNoTracking()
            .OrderBy(chunk => chunk.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .Select(chunk => new
            {
                ChunkId = chunk.Id,
                chunk.DocumentId,
                DocumentName = chunk.Document.Name,
                chunk.ChunkIndex,
                chunk.Content,
                Distance = chunk.Embedding.CosineDistance(queryVector)
            })
            .ToListAsync(cancellationToken);

        return matches
            .Select(match => new RetrievedChunk(
                match.ChunkId,
                match.DocumentId,
                match.DocumentName,
                match.ChunkIndex,
                match.Content,
                Math.Clamp(1d - match.Distance, -1d, 1d)))
            .ToArray();
    }

    private static IQueryable<IndexedDocument> SelectIndexedDocuments(IQueryable<DocumentEntity> documents) =>
        documents
            .AsNoTracking()
            .Select(document => new IndexedDocument(
                document.Id,
                document.Name,
                document.ContentType,
                document.Chunks.Count,
                document.CreatedAt));
}
