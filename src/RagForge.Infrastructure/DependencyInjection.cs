using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RagForge.Application.Interfaces;
using RagForge.Infrastructure.AI;
using RagForge.Infrastructure.Persistence;

namespace RagForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);
        services.AddDbContext<RagForgeDbContext>(options =>
            options.UseNpgsql(dataSource, postgres => postgres.UseVector()));
        services.AddScoped<IVectorStore, PgVectorStore>();

        services.AddOptions<EmbeddingOptions>()
            .Bind(configuration.GetSection(EmbeddingOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Embedding:BaseUrl must be an absolute URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Embedding:ApiKey is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "Embedding:Model is required.")
            .Validate(options => options.BatchSize is >= 1 and <= 2048, "Embedding:BatchSize must be between 1 and 2048.")
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 300, "Embedding:TimeoutSeconds must be between 5 and 300.")
            .ValidateOnStart();

        services.AddOptions<ChatOptions>()
            .Bind(configuration.GetSection(ChatOptions.SectionName))
            .Validate(options => Enum.IsDefined(options.Provider), "Chat:Provider must be OpenAI, Anthropic, or DeepSeek.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Chat:ApiKey is required.")
            .Validate(options => options.ResolvedBaseUrl is null || Uri.TryCreate(options.ResolvedBaseUrl, UriKind.Absolute, out _), "Chat:BaseUrl must be an absolute URL.")
            .Validate(options => options.Temperature is null or (>= 0 and <= 2), "Chat:Temperature must be between 0 and 2 when set.")
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 600, "Chat:TimeoutSeconds must be between 5 and 600.")
            .ValidateOnStart();

        services.AddHttpClient<IEmbeddingService, OpenAiCompatibleEmbeddingClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            client.BaseAddress = WithTrailingSlash(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // The chat provider is chosen once at startup; OpenAI and DeepSeek share the OpenAI-compatible client.
        var chatProvider = configuration.GetSection(ChatOptions.SectionName).GetValue<ChatProvider>(nameof(ChatOptions.Provider));
        if (chatProvider == ChatProvider.Anthropic)
        {
            services.AddSingleton<IAnthropicClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ChatOptions>>().Value;
                var client = new AnthropicClient
                {
                    ApiKey = options.ApiKey,
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                };
                return options.ResolvedBaseUrl is { } baseUrl ? client.WithOptions(o => o with { BaseUrl = baseUrl }) : client;
            });
            services.AddScoped<IChatService, AnthropicChatClient>();
        }
        else
        {
            services.AddHttpClient<IChatService, OpenAiCompatibleChatClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ChatOptions>>().Value;
                client.BaseAddress = WithTrailingSlash(options.ResolvedBaseUrl!);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        }

        return services;
    }

    private static Uri WithTrailingSlash(string url) => new(url.EndsWith('/') ? url : $"{url}/");

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RagForgeDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
