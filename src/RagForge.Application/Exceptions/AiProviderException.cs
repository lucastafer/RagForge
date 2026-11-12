using System.Net;

namespace RagForge.Application.Exceptions;

/// <summary>
/// A failed call to an AI provider. <see cref="SettingsSection"/> names the configuration section
/// ("Embedding" or "Chat") whose provider failed, so error responses can point at the right settings.
/// </summary>
public sealed class AiProviderException(
    string settingsSection,
    HttpStatusCode? statusCode,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string SettingsSection { get; } = settingsSection;

    public HttpStatusCode? StatusCode { get; } = statusCode;
}
