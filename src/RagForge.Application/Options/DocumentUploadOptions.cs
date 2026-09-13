namespace RagForge.Application.Options;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "DocumentUpload";

    public long MaxBytes { get; init; } = 5 * 1024 * 1024;
}
