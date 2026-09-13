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
            HttpRequestException =>
                (StatusCodes.Status502BadGateway, "AI provider error", "The configured AI provider could not complete the request."),
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
