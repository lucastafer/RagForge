using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RagForge.Application.Exceptions;
using RagForge.Domain.Exceptions;

namespace RagForge.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            DocumentValidationException =>
                (StatusCodes.Status400BadRequest, "Invalid document", exception.Message),
            DomainException =>
                (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            AiProviderException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } provider =>
                (StatusCodes.Status502BadGateway, "AI provider rejected the credentials", $"Check that {provider.SettingsSection}:ApiKey is valid and has API access."),
            AiProviderException { StatusCode: HttpStatusCode.TooManyRequests or HttpStatusCode.PaymentRequired } provider =>
                (StatusCodes.Status503ServiceUnavailable, "AI provider rate limit", $"The {provider.SettingsSection} provider rate limit or quota was exceeded. Check your API billing and try again later."),
            AiProviderException { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.BadRequest } provider =>
                (StatusCodes.Status502BadGateway, "AI provider rejected the request", $"Check {provider.SettingsSection}:BaseUrl and that {provider.SettingsSection}:Model is available for this API key."),
            AiProviderException { StatusCode: null } provider =>
                (StatusCodes.Status502BadGateway, "AI provider error", provider.Message),
            AiProviderException provider =>
                (StatusCodes.Status502BadGateway, "AI provider error", $"The {provider.SettingsSection} provider could not complete the request."),
            TaskCanceledException { InnerException: TimeoutException } =>
                (StatusCodes.Status504GatewayTimeout, "AI provider timeout", "The AI provider did not respond within the configured TimeoutSeconds."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.")
        };

        logger.Log(
            statusCode >= 500 ? LogLevel.Error : LogLevel.Warning,
            exception,
            "Request failed with status code {StatusCode}",
            statusCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}
