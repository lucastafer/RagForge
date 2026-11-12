using RagForge.Application.DTOs.Documents;
using RagForge.Application.DTOs.Questions;
using RagForge.Application.Exceptions;
using RagForge.Application.Interfaces;
using RagForge.Application.Models;
using RagForge.Application.Options;
using RagForge.Application.Services;
using RagForge.Application.UseCases.Documents;
using RagForge.Application.UseCases.Questions;
using RagForge.Domain.Entities;
using System.Text;
using Xunit;

namespace RagForge.Application.Tests;

public sealed class UseCaseTests
{
    [Fact]
    public async Task IngestDocument_CreatesEmbeddingsAndStoresDocument()
    {
        var embeddingService = new FakeEmbeddingService();
        var vectorStore = new FakeVectorStore();
        var useCase = new IngestDocumentUseCase(
            new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 20 }),
            embeddingService,
            vectorStore,
            new DocumentUploadOptions { MaxBytes = 1024 });

        var content = Encoding.UTF8.GetBytes(string.Join(' ', Enumerable.Repeat("retrieval", 30)));

        var response = await useCase.ExecuteAsync(new IngestDocumentRequestDto
        {
            Name = "architecture.md",
            ContentType = "text/markdown",
            Length = content.Length,
            ContentStream = new MemoryStream(content)
        });

        Assert.NotEqual(Guid.Empty, response.DocumentId);
        Assert.True(response.ChunkCount > 1);
        Assert.Equal(response.ChunkCount, embeddingService.InputCount);
        Assert.Equal(response.ChunkCount, vectorStore.StoredChunks.Count);
    }

    [Fact]
    public async Task IngestDocument_RejectsUnsupportedFileInApplicationLayer()
    {
        var useCase = CreateIngestDocumentUseCase(maxBytes: 1024);
        var content = Encoding.UTF8.GetBytes("unsupported content");

        var exception = await Assert.ThrowsAsync<DocumentValidationException>(() =>
            useCase.ExecuteAsync(new IngestDocumentRequestDto
            {
                Name = "document.pdf",
                ContentType = "application/pdf",
                Length = content.Length,
                ContentStream = new MemoryStream(content)
            }));

        Assert.Contains(".txt", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IngestDocument_RejectsFileAboveConfiguredLimit()
    {
        var useCase = CreateIngestDocumentUseCase(maxBytes: 5);
        var content = Encoding.UTF8.GetBytes("too large");

        var exception = await Assert.ThrowsAsync<DocumentValidationException>(() =>
            useCase.ExecuteAsync(new IngestDocumentRequestDto
            {
                Name = "document.txt",
                ContentType = "text/plain",
                Length = content.Length,
                ContentStream = new MemoryStream(content)
            }));

        Assert.Contains("maximum size", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AskQuestion_ReturnsGroundedAnswerAndSources()
    {
        var vectorStore = new FakeVectorStore
        {
            SearchResults =
            [
                new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "rag.md", 0, "RAG retrieves relevant chunks.", 0.92)
            ]
        };
        var useCase = new AskQuestionUseCase(
            new FakeEmbeddingService(),
            vectorStore,
            new FakeChatService(),
            new RetrievalOptions { MinSimilarity = 0.2 });

        var response = await useCase.ExecuteAsync(new AskQuestionRequestDto
        {
            Question = "What does RAG retrieve?",
            TopK = 3
        });

        Assert.Equal("A grounded test answer [Source 1].", response.Answer);
        Assert.Equal("test-model", response.ChatModel);
        var source = Assert.Single(response.Sources);
        Assert.Equal("rag.md", source.DocumentName);
        Assert.Equal(0.92, source.Similarity);
    }

    [Fact]
    public async Task IngestDocument_ReturnsExistingDocument_WhenContentWasAlreadyIndexed()
    {
        var existingId = Guid.NewGuid();
        var embeddingService = new FakeEmbeddingService();
        var vectorStore = new FakeVectorStore
        {
            ExistingDocument = new IndexedDocument(existingId, "rag.md", "text/markdown", 3, DateTimeOffset.UtcNow)
        };
        var useCase = new IngestDocumentUseCase(
            new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 20 }),
            embeddingService,
            vectorStore,
            new DocumentUploadOptions { MaxBytes = 1024 });
        var content = Encoding.UTF8.GetBytes("Retrieval content that was uploaded before.");

        var response = await useCase.ExecuteAsync(new IngestDocumentRequestDto
        {
            Name = "copy-of-rag.md",
            ContentType = "text/markdown",
            Length = content.Length,
            ContentStream = new MemoryStream(content)
        });

        Assert.True(response.AlreadyIndexed);
        Assert.Equal(existingId, response.DocumentId);
        Assert.Equal(3, response.ChunkCount);
        Assert.Equal(0, embeddingService.InputCount);
        Assert.Empty(vectorStore.StoredChunks);
    }

    [Fact]
    public async Task AskQuestion_SkipsChatModel_WhenNoChunkReachesMinimumSimilarity()
    {
        var chatService = new FakeChatService();
        var vectorStore = new FakeVectorStore
        {
            SearchResults =
            [
                new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), "rag.md", 0, "Unrelated content.", 0.05)
            ]
        };
        var useCase = new AskQuestionUseCase(
            new FakeEmbeddingService(),
            vectorStore,
            chatService,
            new RetrievalOptions { MinSimilarity = 0.2 });

        var response = await useCase.ExecuteAsync(new AskQuestionRequestDto
        {
            Question = "What is the weather today?",
            TopK = 3
        });

        Assert.Contains("could not find", response.Answer, StringComparison.Ordinal);
        Assert.Empty(response.Sources);
        Assert.False(chatService.WasCalled);
        Assert.Null(response.ChatModel);
    }

    private static IngestDocumentUseCase CreateIngestDocumentUseCase(long maxBytes) =>
        new(
            new TextChunker(new ChunkingOptions { ChunkSize = 100, Overlap = 20 }),
            new FakeEmbeddingService(),
            new FakeVectorStore(),
            new DocumentUploadOptions { MaxBytes = maxBytes });

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public int InputCount { get; private set; }

        public Task<IReadOnlyList<float[]>> GenerateAsync(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default)
        {
            InputCount = inputs.Count;
            IReadOnlyList<float[]> embeddings = inputs.Select(_ => new[] { 1f, 0f, 0f }).ToArray();
            return Task.FromResult(embeddings);
        }
    }

    private sealed class FakeChatService : IChatService
    {
        public string ModelName => "test-model";

        public bool WasCalled { get; private set; }

        public Task<string> GenerateGroundedAnswerAsync(
            string question,
            IReadOnlyList<RetrievedChunk> context,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult("A grounded test answer [Source 1].");
        }
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        public IReadOnlyList<EmbeddedChunk> StoredChunks { get; private set; } = [];

        public IReadOnlyList<RetrievedChunk> SearchResults { get; init; } = [];

        public IndexedDocument? ExistingDocument { get; init; }

        public Task StoreDocumentAsync(
            Document document,
            IReadOnlyList<EmbeddedChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            StoredChunks = chunks;
            return Task.CompletedTask;
        }

        public Task<IndexedDocument?> FindByContentHashAsync(
            string contentHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingDocument);

        public Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IndexedDocument>>(ExistingDocument is null ? [] : [ExistingDocument]);

        public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
            float[] embedding,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchResults);
    }
}
