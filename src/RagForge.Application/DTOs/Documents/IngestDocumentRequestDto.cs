namespace RagForge.Application.DTOs.Documents;

public sealed class IngestDocumentRequestDto
{
    public string Name { get; set; } = string.Empty;

    public string ContentType { get; set; } = "text/plain";

    public long Length { get; set; }

    public Stream ContentStream { get; set; } = Stream.Null;
}
