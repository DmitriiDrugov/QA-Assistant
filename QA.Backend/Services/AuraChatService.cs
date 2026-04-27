using Microsoft.EntityFrameworkCore;
using QA.Backend.Data;
using QA.Backend.Data.Entities;
using QA.Backend.Models.Aura;

namespace QA.Backend.Services;

public sealed class AuraChatService(
    AppDbContext dbContext,
    AuraModelService auraModelService,
    KnowledgeBaseService knowledgeBaseService,
    SearchService searchService)
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AuraModelService _auraModelService = auraModelService;
    private readonly KnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
    private readonly SearchService _searchService = searchService;

    public async Task<AuraMessageResponse> SendMessageAsync(
        string userId,
        string conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new QaValidationException("Message content is required.");
        }

        var conversation = await _dbContext.Conversations
            .SingleOrDefaultAsync(
                item => item.Id == conversationId && item.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            throw new ConversationNotFoundException(conversationId);
        }

        var history = await _dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
        history = history.OrderBy(message => message.CreatedAtUtc).ToList();

        var settings = await _dbContext.AiModelSettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var userMessageContent = content.Trim();
        var knowledgeChunks = await _knowledgeBaseService.GetChunksAsync(cancellationToken);
        var matchedKnowledgeChunk = _searchService.TryFindMostRelevantChunk(knowledgeChunks, userMessageContent);

        var aiReply = await _auraModelService.GenerateReplyAsync(
            userMessageContent,
            history,
            settings,
            matchedKnowledgeChunk,
            cancellationToken);

        var userMessage = new MessageEntity
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            Role = "user",
            Content = userMessageContent,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var assistantMessage = new MessageEntity
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = aiReply,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.AddRange(userMessage, assistantMessage);

        conversation.UpdatedAtUtc = assistantMessage.CreatedAtUtc;
        if (conversation.Title == "New conversation" && history.Count == 0)
        {
            conversation.Title = userMessageContent.Length <= 50
                ? userMessageContent
                : $"{userMessageContent[..50]}...";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuraMessageResponse
        {
            Id = assistantMessage.Id,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            CreatedAt = assistantMessage.CreatedAtUtc
        };
    }
}
