using Microsoft.AspNetCore.Http.Features;
using RagForge.Api.ExceptionHandling;
using RagForge.Application;
using RagForge.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Fail at startup, in every environment, when the selected chat provider is not wired correctly.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

// This is a demo API, so the OpenAPI document and the Scalar reference at /scalar stay available in every environment.
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();
