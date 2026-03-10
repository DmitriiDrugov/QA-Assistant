using Microsoft.AspNetCore.Mvc;
using QA.Backend.Models;

namespace QA.Backend.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(IWebHostEnvironment environment) : ControllerBase
{
    private readonly IWebHostEnvironment _environment = environment;

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        var response = new HealthResponse
        {
            Environment = _environment.EnvironmentName,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        return Ok(response);
    }
}
