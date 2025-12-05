using GidroAtlas.Api.Infrastructure.Auth;
using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GidroAtlas.Api.Controllers;

/// <summary>
/// Authentication controller for user login/logout operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(AppConstants.ContentTypes.ApplicationJson)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user and returns JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token with user information</returns>
    /// <response code="200">Returns JWT token and user info</response>
    /// <response code="400">If login or password is missing</response>
    /// <response code="401">If credentials are invalid</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = AppConstants.ErrorMessages.LoginPasswordRequired });
        }

        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Login}", request.Login);
            return Unauthorized(new { message = AppConstants.ErrorMessages.InvalidLoginOrPassword });
        }

        _logger.LogInformation("User {Login} successfully authenticated", request.Login);
        return Ok(result);
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    /// <returns>Logout confirmation message</returns>
    /// <response code="200">User successfully logged out</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        var userName = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("User {User} logged out", userName);
        
        return Ok(new { message = "Successfully logged out" });
    }

    /// <summary>
    /// Gets current authenticated user information
    /// </summary>
    /// <returns>Current user details</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity?.Name;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            Id = userId,
            Login = userName,
            Role = role
        });
    }
}