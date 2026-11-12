using RagForge.Domain.Exceptions;

namespace RagForge.Domain.Entities;

public sealed class Document
{
    private readonly List<DocumentChunk> _chunks = [];

    public Document(string name, string contentType, string contentHash)
    {
        Name = NormalizeRequired(name, nameof(name));
        ContentType = NormalizeRequired(contentType, nameof(contentType));
        ContentHash = NormalizeRequired(contentHash, nameof(contentHash));
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string ContentType { get; }

    public string ContentHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

    public DocumentChunk AddChunk(int index, string content)
    {
        if (index != _chunks.Count)
        {
            throw new DomainException("Document chunks must be added in sequential order.");
        }

        var chunk = new DocumentChunk(Id, index, content);
        _chunks.Add(chunk);
        return chunk;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
