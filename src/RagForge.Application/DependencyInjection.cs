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
            .Validate(options => options.Overlap >= 0 && options.Overlap < options.ChunkSize, "Rag:Chunking:Overlap must be smaller than ChunkSize.")
            .ValidateOnStart();

        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName))
            .Validate(options => options.MaxBytes is > 0 and <= 20 * 1024 * 1024, "DocumentUpload:MaxBytes must be between 1 byte and 20 MB.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChunkingOptions>>().Value);
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentUploadOptions>>().Value);
        services.AddScoped<TextChunker>();
        services.AddScoped<IIngestDocumentUseCase, IngestDocumentUseCase>();
        services.AddScoped<IAskQuestionUseCase, AskQuestionUseCase>();

        return services;
    }
}
