using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RagForge.Application.Exceptions;

namespace RagForge.Infrastructure.AI;

internal static class OpenAiCompatibleHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<TResponse> PostAsync<TResponse>(
        HttpClient httpClient,
        string path,
        string apiKey,
        object body,
        string settingsSection,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, body.GetType(), options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AiProviderException(
                settingsSection,
                exception.StatusCode,
                $"The {settingsSection} provider could not be reached: {exception.Message}",
                exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                const int maximumLength = 500;
                throw new AiProviderException(
                    settingsSection,
                    response.StatusCode,
                    $"The {settingsSection} provider returned status {(int)response.StatusCode}: {(error.Length <= maximumLength ? error : error[..maximumLength])}");
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException($"The {settingsSection} provider returned an empty response.");
        }
    }
}
