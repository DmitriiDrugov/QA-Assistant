using System.Text.Json.Serialization;

namespace QA.Backend.Models.Aura;

public sealed class ConversationCreateRequest
{
    public string? Title { get; set; }
}

public sealed class AuraConversationResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class MessageCreateRequest
{
    public string Content { get; set; } = string.Empty;
}

public sealed class AuraMessageResponse
{
    public string Id { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
