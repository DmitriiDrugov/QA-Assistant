namespace QA.Backend.Data.Entities;

public sealed class UserEntity
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<ConversationEntity> Conversations { get; set; } = [];
    public AiModelSettingsEntity? AiSettings { get; set; }
}
