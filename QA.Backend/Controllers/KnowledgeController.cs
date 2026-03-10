using Microsoft.AspNetCore.Mvc;
using QA.Backend.Models;
using QA.Backend.Services;

namespace QA.Backend.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController(KnowledgeBaseService knowledgeBaseService, ILogger<KnowledgeController> logger) : ControllerBase
{
    private readonly KnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
    private readonly ILogger<KnowledgeController> _logger = logger;

    [HttpGet("status")]
    [ProducesResponseType(typeof(KnowledgeStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<KnowledgeStatusResponse> GetStatus()
    {
        return Ok(_knowledgeBaseService.GetStatus());
    }

    [HttpPost("reload")]
    [ProducesResponseType(typeof(KnowledgeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reload(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _knowledgeBaseService.ReloadAsync(cancellationToken);
            return Ok(response);
        }
        catch (KnowledgeBaseException ex)
        {
            _logger.LogError(ex, "Knowledge base reload failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Message = "Knowledge base reload failed.",
                Details = ex.Message
            });
        }
    }
}
