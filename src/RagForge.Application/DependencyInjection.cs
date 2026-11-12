using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RagForge.Application.Options;
using RagForge.Application.Services;
using RagForge.Application.UseCases.Documents;
using RagForge.Application.UseCases.Documents.Interfaces;
using RagForge.Application.UseCases.Questions;
using RagForge.Application.UseCases.Questions.Interfaces;

namespace RagForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ChunkingOptions>()
            .Bind(configuration.GetSection(ChunkingOptions.SectionName))
            .Validate(options => options.ChunkSize >= 100, "Rag:Chunking:ChunkSize must be at least 100.")
            .Validate(options => options.Overlap >= 0 && options.Overlap <= options.ChunkSize / 2, "Rag:Chunking:Overlap must be between 0 and half of ChunkSize.")
            .ValidateOnStart();

        services.AddOptions<RetrievalOptions>()
            .Bind(configuration.GetSection(RetrievalOptions.SectionName))
            .Validate(options => options.MinSimilarity is >= -1 and <= 1, "Rag:Retrieval:MinSimilarity must be between -1 and 1.")
            .ValidateOnStart();

        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName))
            .Validate(options => options.MaxBytes is > 0 and <= 20 * 1024 * 1024, "DocumentUpload:MaxBytes must be between 1 byte and 20 MB.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChunkingOptions>>().Value);
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentUploadOptions>>().Value);
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RetrievalOptions>>().Value);
        services.AddScoped<TextChunker>();
        services.AddScoped<IIngestDocumentUseCase, IngestDocumentUseCase>();
        services.AddScoped<IListDocumentsUseCase, ListDocumentsUseCase>();
        services.AddScoped<IAskQuestionUseCase, AskQuestionUseCase>();

        return services;
    }
}
