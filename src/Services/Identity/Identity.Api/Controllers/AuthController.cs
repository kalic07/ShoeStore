using Identity.Api.Dtos;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Exceptions;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Creates a new customer account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new ConflictException($"An account with email '{request.Email}' already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationAppException(errors);
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        _logger.LogInformation("New user registered: {UserId}", user.Id);

        var response = await BuildAuthResponseAsync(user);
        return CreatedAtAction(nameof(Me), new { }, response);
    }

    /// <summary>Authenticates a user and returns a signed JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["credentials"] = new[] { "Invalid email or password." },
            });
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["credentials"] = new[] { "Invalid email or password." },
            });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(await BuildAuthResponseAsync(user));
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null || !Guid.TryParse(userId, out var id))
        {
            throw new NotFoundException("User", "current");
        }

        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException("User", id);

        return Ok(new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
        });
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _tokenService.CreateAccessToken(user, roles);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
        };
    }
}
