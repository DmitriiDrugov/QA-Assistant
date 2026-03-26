namespace QA.Backend.Data.Entities;

public sealed class ConversationEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "New conversation";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public UserEntity? User { get; set; }
    public List<MessageEntity> Messages { get; set; } = [];
}
