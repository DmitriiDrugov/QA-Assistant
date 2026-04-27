using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QA.Backend.Data.Entities;
using QA.Backend.Options;

namespace QA.Backend.Services;

public sealed class AuraModelService(
    HttpClient httpClient,
    IOptions<AiOptions> aiOptions,
    ILogger<AuraModelService> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly ILogger<AuraModelService> _logger = logger;

    public async Task<string> GenerateReplyAsync(
        string userMessage,
        IReadOnlyList<MessageEntity> history,
        AiModelSettingsEntity? settings,
        string? knowledgeContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_aiOptions.ApiKey))
        {
            throw new AiProviderException("AI API key is not configured. Set Ai:ApiKey.");
        }

        if (!Uri.TryCreate(_aiOptions.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new AiProviderException("AI base URL is invalid. Check Ai:BaseUrl configuration.");
        }

        var normalizedBaseUrl = baseUri.AbsoluteUri.TrimEnd('/');
        var normalizedEndpointPath = _aiOptions.ChatCompletionsPath.Trim().TrimStart('/');
        if (!Uri.TryCreate($"{normalizedBaseUrl}/{normalizedEndpointPath}", UriKind.Absolute, out var endpointUri))
        {
            throw new AiProviderException("AI chat completions URL is invalid. Check Ai:BaseUrl and Ai:ChatCompletionsPath.");
        }

        var baseSystemPrompt = string.IsNullOrWhiteSpace(settings?.SystemPrompt)
            ? "You are a helpful AI assistant."
            : settings!.SystemPrompt;

        var model = ResolveModel(settings);

        var groundedSystemPrompt = BuildGroundedSystemPrompt(baseSystemPrompt, knowledgeContext);
        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = groundedSystemPrompt }
            }
            .Concat(history.TakeLast(20).Select(message => (object)new { role = message.Role, content = message.Content }))
            .Concat([new { role = "user", content = userMessage }]),
            temperature = settings?.Temperature ?? 0.7d,
            max_tokens = settings?.MaxTokens ?? 1024
        };

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiOptions.ApiKey);

            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AURA provider request failed before receiving a response.");
            throw new AiProviderException("AURA provider request failed before receiving a response.", ex);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AiProviderException($"AI provider returned {(int)response.StatusCode}. Response: {BuildResponseSnippet(content)}");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var reply = ExtractReply(document.RootElement);

            if (string.IsNullOrWhiteSpace(reply))
            {
                throw new AiProviderException("AURA model response did not contain reply text.");
            }

            return reply.Trim();
        }
        catch (AiProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AiProviderException($"Failed to parse AURA model response. Response snippet: {BuildResponseSnippet(content)}", ex);
        }
    }

    private static string? ExtractReply(JsonElement rootElement)
    {
        if (TryGetString(rootElement, "reply", out var reply))
        {
            return reply;
        }

        if (TryGetString(rootElement, "text", out var text))
        {
            return text;
        }

        if (TryGetString(rootElement, "output", out var output))
        {
            return output;
        }

        if (rootElement.TryGetProperty("choices", out var choicesElement) &&
            choicesElement.ValueKind == JsonValueKind.Array &&
            choicesElement.GetArrayLength() > 0)
        {
            var firstChoice = choicesElement[0];
            if (firstChoice.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.Object &&
                TryGetString(messageElement, "content", out var content))
            {
                return content;
            }
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return true;
        }

        return false;
    }

    private static string BuildResponseSnippet(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "<empty>";
        }

        const int maxLength = 500;
        var normalized = content.Replace('\r', ' ').Replace('\n', ' ');
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength]}...";
    }

    private string ResolveModel(AiModelSettingsEntity? settings)
    {
        var configuredValue = settings?.ModelEndpoint?.Trim();

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return _aiOptions.Model;
        }

        // Backward compatibility for users who may still have an old endpoint saved.
        if (Uri.TryCreate(configuredValue, UriKind.Absolute, out _))
        {
            return _aiOptions.Model;
        }

        return configuredValue;
    }

    private const string PlainTextFormattingPolicy =
        "Formatting rules (must follow strictly):\n" +
        "- Reply in clean plain text. Do not use Markdown.\n" +
        "- Do not use tables or pipe characters (|).\n" +
        "- Do not use asterisks (*), underscores (_), backticks (`), or hash headers (#) for emphasis or structure.\n" +
        "- Do not use bullet markers such as '-', '*', or '•'. If you need to list steps, write them as numbered sentences like '1. ...', '2. ...' on separate lines.\n" +
        "- Do not add decorative separators, emojis, or ASCII art.\n" +
        "- Write natural sentences and paragraphs only.";

    private static string BuildGroundedSystemPrompt(string baseSystemPrompt, string? knowledgeContext)
    {
        if (string.IsNullOrWhiteSpace(knowledgeContext))
        {
            return
                $"{baseSystemPrompt}\n\n" +
                "No knowledge base entry matched the user's message. " +
                "If the user is greeting you, making small talk, or asking what you can do, respond naturally and briefly, and invite them to ask a specific question. " +
                "If the user is asking a factual question about the product or service, say that you do not have information about that in the knowledge base, and suggest they rephrase or ask about a related topic. " +
                "Do not invent facts.\n\n" +
                $"{PlainTextFormattingPolicy}";
        }

        return
            $"{baseSystemPrompt}\n\n" +
            "You must answer using the provided knowledge base context as the primary source of truth. " +
            "If the context does not contain enough information, say that the knowledge base does not contain the answer. " +
            "Do not invent facts outside the provided context.\n\n" +
            $"{PlainTextFormattingPolicy}\n\n" +
            $"Knowledge base context:\n{knowledgeContext}";
    }
}
