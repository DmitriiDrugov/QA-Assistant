using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QA.Backend.Data;
using QA.Backend.Data.Entities;
using QA.Backend.Extensions;
using QA.Backend.Models;
using QA.Backend.Models.Aura;
using QA.Backend.Services;

namespace QA.Backend.Controllers;

[ApiController]
[Authorize]
[Route("conversations")]
public sealed class ConversationsController(AppDbContext dbContext, AuraChatService auraChatService) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AuraChatService _auraChatService = auraChatService;

    [HttpGet]
    [ProducesResponseType(typeof(List<AuraConversationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuraConversationResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        var conversations = await _dbContext.Conversations
            .Where(item => item.UserId == userId)
            .Select(item => new AuraConversationResponse
            {
                Id = item.Id,
                Title = item.Title,
                CreatedAt = item.CreatedAtUtc,
                UpdatedAt = item.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(conversations.OrderByDescending(item => item.UpdatedAt).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(AuraConversationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuraConversationResponse>> Create([FromBody] ConversationCreateRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var now = DateTimeOffset.UtcNow;

        var conversation = new ConversationEntity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request?.Title) ? "New conversation" : request.Title.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AuraConversationResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAtUtc,
            UpdatedAt = conversation.UpdatedAtUtc
        });
    }

    [HttpDelete("{conversationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var conversation = await _dbContext.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId && item.UserId == userId, cancellationToken);

        if (conversation is null)
        {
            return NotFound(new ApiErrorResponse { Message = "Conversation not found." });
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { ok = true });
    }

    [HttpGet("{conversationId}/messages")]
    [ProducesResponseType(typeof(List<AuraMessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Messages(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var conversationExists = await _dbContext.Conversations
            .AnyAsync(item => item.Id == conversationId && item.UserId == userId, cancellationToken);

        if (!conversationExists)
        {
            return NotFound(new ApiErrorResponse { Message = "Conversation not found." });
        }

        var messages = await _dbContext.Messages
            .Where(item => item.ConversationId == conversationId)
            .Select(item => new AuraMessageResponse
            {
                Id = item.Id,
                Role = item.Role,
                Content = item.Content,
                CreatedAt = item.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(messages.OrderBy(item => item.CreatedAt).ToList());
    }

    [HttpPost("{conversationId}/chat")]
    [ProducesResponseType(typeof(AuraMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Chat(
        string conversationId,
        [FromBody] MessageCreateRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        try
        {
            var response = await _auraChatService.SendMessageAsync(
                userId,
                conversationId,
                request?.Content ?? string.Empty,
                cancellationToken);

            return Ok(response);
        }
        catch (QaValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (ConversationNotFoundException)
        {
            return NotFound(new ApiErrorResponse { Message = "Conversation not found." });
        }
        catch (KnowledgeBaseException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Message = "Knowledge base is unavailable.",
                Details = ex.Message
            });
        }
        catch (SearchException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Message = "No relevant knowledge base chunk found.",
                Details = ex.Message
            });
        }
        catch (AiProviderException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse
            {
                Message = "AI model request failed.",
                Details = ex.Message
            });
        }
    }
}
