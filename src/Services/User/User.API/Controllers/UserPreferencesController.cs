using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using User.Core.DTOs;
using User.Core.Interfaces;
using Enterprise.Shared.Common.Models;

namespace User.API.Controllers;

/// <summary>
/// User preferences management endpoints
/// </summary>
[ApiController]
[Route("api/v1/user-preferences")]
[Authorize]
[Produces("application/json")]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly ILogger<UserPreferencesController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserPreferencesController(IUserPreferencesService userPreferencesService, ILogger<UserPreferencesController> logger)
    {
        _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current user's preferences
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserPreferencesDto>> GetMyPreferences(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get user preferences by user ID (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences</returns>
    [HttpGet("{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPreferencesDto>> GetUserPreferences(string userId, CancellationToken cancellationToken = default)
    {
        var result = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create user preferences
    /// </summary>
    /// <param name="preferencesDto">Preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user preferences</returns>
    [HttpPost("me")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserPreferencesDto>> CreateMyPreferences(
        [FromBody] UserPreferencesDto preferencesDto, 
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userPreferencesService.UpdateAsync(userId, new User.Core.DTOs.Requests.UpdateUserPreferencesRequest
        {
            EmailNotifications = preferencesDto.EmailNotifications,
            SmsNotifications = preferencesDto.SmsNotifications,
            PushNotifications = preferencesDto.PushNotifications
        }, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("already exists"))
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetMyPreferences), result.Value);
    }

    /// <summary>
    /// Update current user's preferences
    /// </summary>
    /// <param name="preferencesDto">Updated preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user preferences</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPreferencesDto>> UpdateMyPreferences(
        [FromBody] UserPreferencesDto preferencesDto, 
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userPreferencesService.UpdateAsync(userId, new User.Core.DTOs.Requests.UpdateUserPreferencesRequest
        {
            EmailNotifications = preferencesDto.EmailNotifications,
            SmsNotifications = preferencesDto.SmsNotifications,
            PushNotifications = preferencesDto.PushNotifications
        }, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update user preferences by user ID (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferencesDto">Updated preferences data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user preferences</returns>
    [HttpPut("{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPreferencesDto>> UpdateUserPreferences(
        string userId,
        [FromBody] UserPreferencesDto preferencesDto, 
        CancellationToken cancellationToken = default)
    {
        var result = await _userPreferencesService.UpdateAsync(userId, new User.Core.DTOs.Requests.UpdateUserPreferencesRequest
        {
            EmailNotifications = preferencesDto.EmailNotifications,
            SmsNotifications = preferencesDto.SmsNotifications,
            PushNotifications = preferencesDto.PushNotifications
        }, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }





    /// <summary>
    /// Get current user ID from JWT token
    /// </summary>
    /// <returns>User ID or null</returns>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ??
               User.FindFirst("userId")?.Value;
    }
}