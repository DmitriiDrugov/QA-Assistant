using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
[Route("auth")]
public sealed class AuthController(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher,
    JwtTokenService jwtTokenService) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<UserEntity> _passwordHasher = passwordHasher;
    private readonly JwtTokenService _jwtTokenService = jwtTokenService;

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuraUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest? request, CancellationToken cancellationToken)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiErrorResponse { Message = "Email, username, and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            return BadRequest(new ApiErrorResponse { Message = "Email already registered." });
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,
            Username = request.Username.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToUserResponse(user));
    }

    [HttpPost("login")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse { Message = "Incorrect email or password." });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ApiErrorResponse { Message = "Incorrect email or password." });
        }

        return Ok(new TokenResponse
        {
            AccessToken = _jwtTokenService.CreateAccessToken(user)
        });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuraUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuraUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var user = await _dbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        return Ok(ToUserResponse(user));
    }

    private static AuraUserResponse ToUserResponse(UserEntity user)
    {
        return new AuraUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            CreatedAt = user.CreatedAtUtc
        };
    }
}
