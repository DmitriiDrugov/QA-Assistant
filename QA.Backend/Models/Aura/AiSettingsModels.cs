using System.Text.Json.Serialization;

namespace QA.Backend.Models.Aura;

public sealed class AiSettingsUpdateRequest
{
    public string? Model { get; set; }

    public double? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("system_prompt")]
    public string? SystemPrompt { get; set; }
}

public sealed class AiSettingsResponse
{
    public string Model { get; init; } = string.Empty;

    public double Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; }

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; init; } = string.Empty;
}
