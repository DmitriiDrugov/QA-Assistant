using Microsoft.AspNetCore.Mvc;
using QA.Backend.Models;
using QA.Backend.Services;

namespace QA.Backend.Controllers;

[ApiController]
[Route("api/questions")]
public sealed class QuestionsController(QaService qaService, ILogger<QuestionsController> logger) : ControllerBase
{
    private readonly QaService _qaService = qaService;
    private readonly ILogger<QuestionsController> _logger = logger;

    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Ask([FromBody] AskQuestionRequest? request, CancellationToken cancellationToken)
    {
        // STEP 7: Main endpoint for frontend integration.
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new ApiErrorResponse { Message = "Question is required." });
        }

        try
        {
            var response = await _qaService.AskQuestionAsync(request.Question, cancellationToken);
            return Ok(response);
        }
        catch (QaValidationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (AiProviderException ex)
        {
            _logger.LogError(ex, "AI provider failed while answering question.");
            return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse { Message = "AI provider request failed.", Details = ex.Message });
        }
        catch (KnowledgeBaseException ex)
        {
            _logger.LogError(ex, "Knowledge base failure while answering question.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse { Message = "Knowledge base is unavailable.", Details = ex.Message });
        }
        catch (SearchException ex)
        {
            _logger.LogError(ex, "Search failed while answering question.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse { Message = "No relevant knowledge base chunk found.", Details = ex.Message });
        }
    }
}
