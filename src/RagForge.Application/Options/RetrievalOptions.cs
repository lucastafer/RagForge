namespace RagForge.Application.Options;

public sealed class RetrievalOptions
{
    public const string SectionName = "Rag:Retrieval";

    public double MinSimilarity { get; init; } = 0.2;
}
