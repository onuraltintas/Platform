using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Services.FeatureFlagService.Models.DTOs;
using EgitimPlatform.Services.FeatureFlagService.Services;
using EgitimPlatform.Shared.Security.Models;
using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Services.FeatureFlagService.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<FeatureFlagsController> _logger;

    public FeatureFlagsController(IFeatureFlagService featureFlagService, ILogger<FeatureFlagsController> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new feature flag
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<FeatureFlagResponse>>> CreateFeatureFlag([FromBody] CreateFeatureFlagRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "unknown";
            var result = await _featureFlagService.CreateFeatureFlagAsync(request, userId);
            
            return Ok(ApiResponse<FeatureFlagResponse>.Ok(result, "Feature flag created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature flag");
            return BadRequest(ApiResponse<FeatureFlagResponse>.Fail("FEATURE_FLAG_CREATE_ERROR", $"Failed to create feature flag: {ex.Message}"));
        }
    }

    /// <summary>
    /// Updates an existing feature flag
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<FeatureFlagResponse>>> UpdateFeatureFlag(string id, [FromBody] UpdateFeatureFlagRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "unknown";
            var result = await _featureFlagService.UpdateFeatureFlagAsync(id, request, userId);
            
            return Ok(ApiResponse<FeatureFlagResponse>.Ok(result, "Feature flag updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature flag {Id}", id);
            return BadRequest(ApiResponse<FeatureFlagResponse>.Fail("Failed to update feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Gets a feature flag by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FeatureFlagResponse>>> GetFeatureFlag(string id)
    {
        try
        {
            var result = await _featureFlagService.GetFeatureFlagAsync(id);
            
            if (result == null)
            {
                return NotFound(ApiResponse<FeatureFlagResponse>.Fail("FEATURE_FLAG_NOT_FOUND", "Feature flag not found"));
            }
            
            return Ok(ApiResponse<FeatureFlagResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag {Id}", id);
            return BadRequest(ApiResponse<FeatureFlagResponse>.Fail("Failed to get feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Gets a feature flag by key, environment and application ID
    /// </summary>
    [HttpGet("by-key/{key}")]
    public async Task<ActionResult<ApiResponse<FeatureFlagResponse>>> GetFeatureFlagByKey(
        string key, 
        [FromQuery] string environment = "production", 
        [FromQuery] string applicationId = "")
    {
        try
        {
            var result = await _featureFlagService.GetFeatureFlagByKeyAsync(key, environment, applicationId);
            
            if (result == null)
            {
                return NotFound(ApiResponse<FeatureFlagResponse>.Fail("FEATURE_FLAG_NOT_FOUND", "Feature flag not found"));
            }
            
            return Ok(ApiResponse<FeatureFlagResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag by key {Key}", key);
            return BadRequest(ApiResponse<FeatureFlagResponse>.Fail("Failed to get feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Gets paginated list of feature flags
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedFeatureFlagResponse>>> GetFeatureFlags([FromQuery] FeatureFlagListRequest request)
    {
        try
        {
            var result = await _featureFlagService.GetFeatureFlagsAsync(request);
            return Ok(ApiResponse<PagedFeatureFlagResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flags");
            return BadRequest(ApiResponse<PagedFeatureFlagResponse>.Fail("Failed to get feature flags", ex.Message));
        }
    }

    /// <summary>
    /// Deletes a feature flag
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFeatureFlag(string id)
    {
        try
        {
            var result = await _featureFlagService.DeleteFeatureFlagAsync(id);
            
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Fail("FEATURE_FLAG_NOT_FOUND", "Feature flag not found"));
            }
            
            return Ok(ApiResponse<bool>.Ok(true, "Feature flag deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag {Id}", id);
            return BadRequest(ApiResponse<bool>.Fail("Failed to delete feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Evaluates a feature flag for a specific user
    /// </summary>
    [HttpPost("evaluate")]
    [AllowAnonymous] // Allow anonymous for SDK usage
    public async Task<ActionResult<ApiResponse<FeatureFlagEvaluationResponse>>> EvaluateFeatureFlag([FromBody] EvaluateFeatureFlagRequest request)
    {
        try
        {
            var result = await _featureFlagService.EvaluateFeatureFlagAsync(request);
            return Ok(ApiResponse<FeatureFlagEvaluationResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag {Key} for user {UserId}", request.FeatureFlagKey, request.UserId);
            return BadRequest(ApiResponse<FeatureFlagEvaluationResponse>.Fail("Failed to evaluate feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Evaluates multiple feature flags for a user in a single request
    /// </summary>
    [HttpPost("batch-evaluate")]
    [AllowAnonymous] // Allow anonymous for SDK usage
    public async Task<ActionResult<ApiResponse<BatchEvaluationResponse>>> BatchEvaluate([FromBody] BatchEvaluateRequest request)
    {
        try
        {
            var result = await _featureFlagService.BatchEvaluateAsync(request);
            return Ok(ApiResponse<BatchEvaluationResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch evaluating feature flags for user {UserId}", request.UserId);
            return BadRequest(ApiResponse<BatchEvaluationResponse>.Fail("Failed to evaluate feature flags", ex.Message));
        }
    }

    /// <summary>
    /// Simple check if a feature is enabled for a user
    /// </summary>
    [HttpGet("enabled/{key}")]
    [AllowAnonymous] // Allow anonymous for SDK usage
    public async Task<ActionResult<ApiResponse<bool>>> IsFeatureEnabled(
        string key, 
        [FromQuery] string userId, 
        [FromQuery] string environment = "production", 
        [FromQuery] string applicationId = "")
    {
        try
        {
            var result = await _featureFlagService.IsFeatureEnabledAsync(userId, key, null, environment, applicationId);
            return Ok(ApiResponse<bool>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature {Key} is enabled for user {UserId}", key, userId);
            return BadRequest(ApiResponse<bool>.Fail("Failed to check feature flag", ex.Message));
        }
    }

    /// <summary>
    /// Assigns a user to a specific variation
    /// </summary>
    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<FeatureFlagAssignmentResponse>>> AssignUserToVariation(
        string id, 
        [FromBody] AssignUserRequest request)
    {
        try
        {
            var result = await _featureFlagService.AssignUserToVariationAsync(
                id, 
                request.UserId, 
                request.Variation, 
                request.Reason, 
                request.Context);
                
            return Ok(ApiResponse<FeatureFlagAssignmentResponse>.Ok(result, "User assigned to variation successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user {UserId} to variation for feature flag {Id}", request.UserId, id);
            return BadRequest(ApiResponse<FeatureFlagAssignmentResponse>.Fail("Failed to assign user to variation", ex.Message));
        }
    }

    /// <summary>
    /// Gets user's assignment for a feature flag
    /// </summary>
    [HttpGet("{id}/assignment/{userId}")]
    public async Task<ActionResult<ApiResponse<FeatureFlagAssignmentResponse>>> GetUserAssignment(string id, string userId)
    {
        try
        {
            var result = await _featureFlagService.GetUserAssignmentAsync(userId, id);
            
            if (result == null)
            {
                return NotFound(ApiResponse<FeatureFlagAssignmentResponse>.Fail("ASSIGNMENT_NOT_FOUND", "Assignment not found"));
            }
            
            return Ok(ApiResponse<FeatureFlagAssignmentResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user assignment for feature flag {Id} and user {UserId}", id, userId);
            return BadRequest(ApiResponse<FeatureFlagAssignmentResponse>.Fail("Failed to get user assignment", ex.Message));
        }
    }

    /// <summary>
    /// Logs an event for tracking
    /// </summary>
    [HttpPost("events")]
    [AllowAnonymous] // Allow anonymous for SDK usage
    public async Task<ActionResult<ApiResponse<bool>>> LogEvent([FromBody] LogEventRequest request)
    {
        try
        {
            await _featureFlagService.LogEventAsync(request);
            return Ok(ApiResponse<bool>.Ok(true, "Event logged successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging event for feature flag {FeatureFlagId}", request.FeatureFlagId);
            return BadRequest(ApiResponse<bool>.Fail("Failed to log event", ex.Message));
        }
    }

    /// <summary>
    /// Gets statistics for a feature flag
    /// </summary>
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<ApiResponse<FeatureFlagStatsResponse>>> GetFeatureFlagStats(
        string id, 
        [FromQuery] int days = 30)
    {
        try
        {
            var result = await _featureFlagService.GetFeatureFlagStatsAsync(id, TimeSpan.FromDays(days));
            return Ok(ApiResponse<FeatureFlagStatsResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for feature flag {Id}", id);
            return BadRequest(ApiResponse<FeatureFlagStatsResponse>.Fail("Failed to get feature flag stats", ex.Message));
        }
    }

    /// <summary>
    /// Bulk update status of multiple feature flags
    /// </summary>
    [HttpPatch("bulk/status")]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<List<FeatureFlagResponse>>>> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "unknown";
            var result = await _featureFlagService.BulkUpdateStatusAsync(request.FeatureFlagIds, request.Status, userId);
            
            return Ok(ApiResponse<List<FeatureFlagResponse>>.Ok(result, "Feature flags updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating feature flag status");
            return BadRequest(ApiResponse<List<FeatureFlagResponse>>.Fail("Failed to update feature flags", ex.Message));
        }
    }

    /// <summary>
    /// Bulk toggle multiple feature flags
    /// </summary>
    [HttpPatch("bulk/toggle")]
    [Authorize(Roles = "Admin,FeatureManager")]
    public async Task<ActionResult<ApiResponse<List<FeatureFlagResponse>>>> BulkToggle([FromBody] BulkToggleRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "unknown";
            var result = await _featureFlagService.BulkToggleAsync(request.FeatureFlagIds, request.IsEnabled, userId);
            
            return Ok(ApiResponse<List<FeatureFlagResponse>>.Ok(result, "Feature flags toggled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk toggling feature flags");
            return BadRequest(ApiResponse<List<FeatureFlagResponse>>.Fail("Failed to toggle feature flags", ex.Message));
        }
    }
}

// Additional request DTOs for controller actions
public record AssignUserRequest(
    string UserId,
    string Variation,
    string Reason,
    Dictionary<string, object>? Context = null
);

public record BulkUpdateStatusRequest(
    List<string> FeatureFlagIds,
    Models.Entities.FeatureFlagStatus Status
);

public record BulkToggleRequest(
    List<string> FeatureFlagIds,
    bool IsEnabled
);