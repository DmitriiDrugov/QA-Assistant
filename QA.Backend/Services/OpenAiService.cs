using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QA.Backend.Options;

namespace QA.Backend.Services;

public sealed class OpenAiService(HttpClient httpClient, IOptions<AiOptions> aiOptions) : IAiService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly AiOptions _aiOptions = aiOptions.Value;

    public async Task<string> GenerateAnswerAsync(string question, string context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiOptions.ApiKey))
        {
            throw new AiProviderException("AI API key is not configured. Set Ai:ApiKey (or environment variable AI__APIKEY).", new InvalidOperationException());
        }

        if (!Uri.TryCreate(_aiOptions.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new AiProviderException("AI base URL is invalid. Check Ai:BaseUrl configuration.", new InvalidOperationException());
        }

        var endpointPath = _aiOptions.ChatCompletionsPath.TrimStart('/');
        var endpointUri = new Uri(baseUri, endpointPath);

        // STEP 6: Keep AI provider-specific request logic isolated in one service.
        var requestBody = new
        {
            model = _aiOptions.Model,
            messages = new[]
            {
                new { role = "system", content = _aiOptions.SystemPrompt },
                new { role = "user", content = $"Context:\n{context}\n\nQuestion:\n{question}" }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = JsonContent.Create(requestBody)
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiOptions.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AiProviderException("AI provider call failed before receiving a response.", ex);
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException($"AI provider returned {(int)response.StatusCode}. Response: {responseContent}", new HttpRequestException());
        }

        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var answer = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new AiProviderException("AI provider response did not include an answer.", new InvalidDataException());
            }

            return answer.Trim();
        }
        catch (AiProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AiProviderException("Failed to parse AI provider response.", ex);
        }
    }
}
