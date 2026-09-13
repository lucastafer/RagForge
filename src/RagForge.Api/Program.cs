using Microsoft.AspNetCore.Http.Features;
using RagForge.Api.ExceptionHandling;
using RagForge.Application;
using RagForge.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();
