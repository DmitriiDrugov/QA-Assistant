using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QA.Backend.Data;
using QA.Backend.Data.Entities;
using QA.Backend.Extensions;
using QA.Backend.Models;
using QA.Backend.Models.Aura;
using QA.Backend.Options;

namespace QA.Backend.Controllers;

[ApiController]
[Authorize]
[Route("settings/ai")]
public sealed class AiSettingsController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AiOptions _aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

    [HttpGet]
    [ProducesResponseType(typeof(AiSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var settings = await _dbContext.AiModelSettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return Ok(ToResponse(settings, _aiOptions));
    }

    [HttpPut]
    [ProducesResponseType(typeof(AiSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromBody] AiSettingsUpdateRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse { Message = "Settings payload is required." });
        }

        if (request.Temperature is < 0 or > 2)
        {
            return BadRequest(new ApiErrorResponse { Message = "Temperature must be between 0 and 2." });
        }

        if (request.MaxTokens is <= 0)
        {
            return BadRequest(new ApiErrorResponse { Message = "Max tokens must be greater than 0." });
        }

        var userId = User.GetRequiredUserId();
        var settings = await _dbContext.AiModelSettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new AiModelSettingsEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId
            };
            _dbContext.AiModelSettings.Add(settings);
        }

        if (request.Model is not null)
        {
            settings.ModelEndpoint = request.Model.Trim();
        }

        if (request.Temperature.HasValue)
        {
            settings.Temperature = request.Temperature.Value;
        }

        if (request.MaxTokens.HasValue)
        {
            settings.MaxTokens = request.MaxTokens.Value;
        }

        if (request.SystemPrompt is not null)
        {
            settings.SystemPrompt = string.IsNullOrWhiteSpace(request.SystemPrompt)
                ? "You are a helpful AI assistant."
                : request.SystemPrompt.Trim();
        }

        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(settings, _aiOptions));
    }

    private static AiSettingsResponse ToResponse(AiModelSettingsEntity? settings, AiOptions aiOptions)
    {
        return new AiSettingsResponse
        {
            Model = string.IsNullOrWhiteSpace(settings?.ModelEndpoint) ? aiOptions.Model : settings!.ModelEndpoint,
            Temperature = settings?.Temperature ?? 0.7d,
            MaxTokens = settings?.MaxTokens ?? 1024,
            SystemPrompt = settings?.SystemPrompt ?? "You are a helpful AI assistant."
        };
    }
}
