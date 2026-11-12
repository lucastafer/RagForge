using System.Security.Cryptography;
using System.Text;
using RagForge.Application.DTOs.Documents;
using RagForge.Application.Exceptions;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;
using RagForge.Application.Options;
using RagForge.Application.Services;
using RagForge.Application.UseCases.Documents.Interfaces;
using RagForge.Domain.Entities;

namespace RagForge.Application.UseCases.Documents;

public sealed class IngestDocumentUseCase(
    TextChunker chunker,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    DocumentUploadOptions uploadOptions) : IIngestDocumentUseCase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md",
        ".markdown"
    };

    public async Task<IngestDocumentResponseDto> ExecuteAsync(
        IngestDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var fileName = ValidateAndNormalize(request);
        using var reader = new StreamReader(
            request.ContentStream,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DocumentValidationException("The uploaded file contains no text.");
        }

        var contentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        var existing = await vectorStore.FindByContentHashAsync(contentHash, cancellationToken);
        if (existing is not null)
        {
            return new IngestDocumentResponseDto
            {
                DocumentId = existing.Id,
                Name = existing.Name,
                ChunkCount = existing.ChunkCount,
                AlreadyIndexed = true
            };
        }

        var segments = chunker.Split(content);
        var document = new Document(fileName, request.ContentType, contentHash);
        var documentChunks = segments
            .Select(segment => document.AddChunk(segment.Index, segment.Content))
            .ToArray();

        var embeddings = await embeddingService.GenerateAsync(
            documentChunks.Select(chunk => chunk.Content).ToArray(),
            cancellationToken);

        if (embeddings.Count != documentChunks.Length)
        {
            throw new InvalidOperationException("The embedding provider returned an unexpected number of vectors.");
        }

        var embeddedChunks = documentChunks
            .Select((chunk, index) => new EmbeddedChunk(chunk.Id, chunk.Index, chunk.Content, embeddings[index]))
            .ToArray();

        await vectorStore.StoreDocumentAsync(document, embeddedChunks, cancellationToken);

        return new IngestDocumentResponseDto
        {
            DocumentId = document.Id,
            Name = document.Name,
            ChunkCount = document.Chunks.Count
        };
    }

    private string ValidateAndNormalize(IngestDocumentRequestDto request)
    {
        if (request.Length <= 0)
        {
            throw new DocumentValidationException("The uploaded file is empty.");
        }

        if (request.Length > uploadOptions.MaxBytes)
        {
            throw new DocumentValidationException(
                $"The uploaded file exceeds the maximum size of {uploadOptions.MaxBytes} bytes.");
        }

        var fileName = Path.GetFileName(request.Name);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DocumentValidationException("The uploaded file name is required.");
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(fileName)))
        {
            throw new DocumentValidationException("Upload a .txt, .md, or .markdown file.");
        }

        if (!request.ContentStream.CanRead)
        {
            throw new DocumentValidationException("The uploaded file cannot be read.");
        }

        return fileName;
    }
}
