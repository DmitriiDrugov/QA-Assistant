using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

        var normalizedBaseUrl = baseUri.AbsoluteUri.TrimEnd('/');
        var normalizedEndpointPath = _aiOptions.ChatCompletionsPath.Trim().TrimStart('/');
        if (!Uri.TryCreate($"{normalizedBaseUrl}/{normalizedEndpointPath}", UriKind.Absolute, out var endpointUri))
        {
            throw new AiProviderException("AI chat completions URL is invalid. Check Ai:BaseUrl and Ai:ChatCompletionsPath configuration.", new InvalidOperationException());
        }

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
            var answer = ExtractAnswer(document.RootElement);

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
            throw new AiProviderException($"Failed to parse AI provider response. Response snippet: {BuildResponseSnippet(responseContent)}", ex);
        }
    }

    private static string? ExtractAnswer(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("choices", out var choicesElement) ||
            choicesElement.ValueKind != JsonValueKind.Array ||
            choicesElement.GetArrayLength() == 0)
        {
            return null;
        }

        var firstChoice = choicesElement[0];

        if (firstChoice.TryGetProperty("message", out var messageElement) &&
            messageElement.ValueKind == JsonValueKind.Object &&
            messageElement.TryGetProperty("content", out var contentElement))
        {
            return ExtractContent(contentElement);
        }

        if (firstChoice.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
        {
            return textElement.GetString();
        }

        return null;
    }

    private static string? ExtractContent(JsonElement contentElement)
    {
        if (contentElement.ValueKind == JsonValueKind.String)
        {
            return contentElement.GetString();
        }

        if (contentElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();

        foreach (var item in contentElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                builder.Append(item.GetString());
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
            {
                builder.Append(textElement.GetString());
            }
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string BuildResponseSnippet(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return "<empty>";
        }

        const int maxLength = 500;
        var normalized = responseContent.Replace('\r', ' ').Replace('\n', ' ');
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength]}...";
    }
}
