using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "OpenAI:BaseUrl must be an absolute URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "OpenAI:ApiKey is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.EmbeddingModel), "OpenAI:EmbeddingModel is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ChatModel), "OpenAI:ChatModel is required.")
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 300, "OpenAI:TimeoutSeconds must be between 5 and 300.")
            .ValidateOnStart();

        services.AddHttpClient<OpenAiCompatibleClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IEmbeddingService>(serviceProvider => serviceProvider.GetRequiredService<OpenAiCompatibleClient>());
        services.AddScoped<IChatService>(serviceProvider => serviceProvider.GetRequiredService<OpenAiCompatibleClient>());

        return services;
    }

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RagForgeDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
