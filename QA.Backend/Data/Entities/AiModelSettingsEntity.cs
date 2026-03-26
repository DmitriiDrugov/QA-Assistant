namespace QA.Backend.Data.Entities;

public sealed class AiModelSettingsEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ModelEndpoint { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7d;
    public int MaxTokens { get; set; } = 1024;
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public UserEntity? User { get; set; }
}
