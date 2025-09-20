using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using User.Core.DTOs;
using User.Core.DTOs.Requests;
using User.Core.Interfaces;

namespace User.API.Controllers;

/// <summary>
/// User profile management endpoints
/// </summary>
[ApiController]
[Route("api/v1/user-profiles")]
[Authorize]
[Produces("application/json")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<UserProfileController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public UserProfileController(IUserProfileService userProfileService, ILogger<UserProfileController> logger)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        
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
    /// Get user profile by user ID (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile</returns>
    [HttpGet("{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserProfileDto>> GetUserProfile(string userId, CancellationToken cancellationToken = default)
    {
        var result = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        
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
    /// Create user profile (typically called during registration)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> CreateProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userProfileService.CreateAsync(userId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("already exists"))
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetMyProfile), result.Value);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(
        [FromBody] UpdateUserProfileRequest request, 
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userProfileService.UpdateAsync(userId, request, cancellationToken);
        
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
    /// Update user profile by user ID (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserProfileDto>> UpdateUserProfile(
        string userId,
        [FromBody] UpdateUserProfileRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await _userProfileService.UpdateAsync(userId, request, cancellationToken);
        
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
    /// Delete current user's profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteMyProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var result = await _userProfileService.DeleteAsync(userId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Delete user profile by user ID (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUserProfile(string userId, CancellationToken cancellationToken = default)
    {
        var result = await _userProfileService.DeleteAsync(userId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return NoContent();
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