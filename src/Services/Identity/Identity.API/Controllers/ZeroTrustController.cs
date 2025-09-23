using Identity.Core.ZeroTrust;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Zero Trust security architecture controller
/// </summary>
[ApiController]
[Route("api/zero-trust")]
[Authorize]
public class ZeroTrustController : ControllerBase
{
    private readonly IZeroTrustService _zeroTrustService;
    private readonly ILogger<ZeroTrustController> _logger;

    public ZeroTrustController(
        IZeroTrustService zeroTrustService,
        ILogger<ZeroTrustController> logger)
    {
        _zeroTrustService = zeroTrustService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluate trust score for current user context
    /// </summary>
    [HttpPost("evaluate-trust")]
    public async Task<ActionResult<TrustScore>> EvaluateTrustScore([FromBody] ZeroTrustEvaluationRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var context = CreateZeroTrustContext(userId, request);
            var trustScore = await _zeroTrustService.EvaluateTrustScoreAsync(context);

            return Ok(trustScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating trust score");
            return StatusCode(500, new { error = "Failed to evaluate trust score" });
        }
    }

    /// <summary>
    /// Validate device compliance
    /// </summary>
    [HttpPost("validate-device")]
    public async Task<ActionResult<DeviceComplianceResult>> ValidateDevice([FromBody] DeviceInfo deviceInfo)
    {
        try
        {
            var result = await _zeroTrustService.ValidateDeviceAsync(deviceInfo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device {DeviceId}", deviceInfo.DeviceId);
            return StatusCode(500, new { error = "Failed to validate device" });
        }
    }

    /// <summary>
    /// Assess network security
    /// </summary>
    [HttpPost("assess-network")]
    public async Task<ActionResult<NetworkSecurityAssessment>> AssessNetwork([FromBody] NetworkContext networkContext)
    {
        try
        {
            var assessment = await _zeroTrustService.AssessNetworkSecurityAsync(networkContext);
            return Ok(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing network security for IP {IpAddress}", networkContext.IpAddress);
            return StatusCode(500, new { error = "Failed to assess network security" });
        }
    }

    /// <summary>
    /// Analyze user behavior
    /// </summary>
    [HttpPost("analyze-behavior")]
    public async Task<ActionResult<BehaviorAnalysisResult>> AnalyzeBehavior([FromBody] UserBehaviorContext behaviorContext)
    {
        try
        {
            var result = await _zeroTrustService.AnalyzeBehaviorAsync(behaviorContext);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing user behavior");
            return StatusCode(500, new { error = "Failed to analyze behavior" });
        }
    }

    /// <summary>
    /// Get authentication requirements based on current context
    /// </summary>
    [HttpPost("authentication-requirements")]
    public async Task<ActionResult<AuthenticationRequirement>> GetAuthenticationRequirements([FromBody] ZeroTrustEvaluationRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var context = CreateZeroTrustContext(userId, request);
            var requirements = await _zeroTrustService.GetAuthenticationRequirementAsync(userId, context);

            return Ok(requirements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication requirements");
            return StatusCode(500, new { error = "Failed to get authentication requirements" });
        }
    }

    /// <summary>
    /// Monitor session security
    /// </summary>
    [HttpGet("monitor-session/{sessionId}")]
    public async Task<ActionResult<SecurityMonitoringResult>> MonitorSession(string sessionId)
    {
        try
        {
            var result = await _zeroTrustService.MonitorSecurityAsync(sessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to monitor session" });
        }
    }

    /// <summary>
    /// Evaluate access request
    /// </summary>
    [HttpPost("evaluate-access")]
    public async Task<ActionResult<AccessDecision>> EvaluateAccess([FromBody] AccessRequestDto accessRequestDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var accessRequest = new AccessRequest
            {
                UserId = userId,
                ResourceId = accessRequestDto.ResourceId,
                Action = accessRequestDto.Action,
                Context = CreateZeroTrustContext(userId, accessRequestDto.Context),
                RequiredPermissions = accessRequestDto.RequiredPermissions
            };

            var decision = await _zeroTrustService.EvaluateAccessAsync(accessRequest);
            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating access request");
            return StatusCode(500, new { error = "Failed to evaluate access" });
        }
    }

    /// <summary>
    /// Validate session integrity
    /// </summary>
    [HttpGet("validate-session/{sessionId}")]
    public async Task<ActionResult<SessionValidationResult>> ValidateSession(string sessionId)
    {
        try
        {
            var result = await _zeroTrustService.ValidateSessionAsync(sessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to validate session" });
        }
    }

    /// <summary>
    /// Update trust score based on events
    /// </summary>
    [HttpPost("update-trust-score")]
    public async Task<IActionResult> UpdateTrustScore([FromBody] TrustScoreUpdate update)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            await _zeroTrustService.UpdateTrustScoreAsync(userId, update);
            return Ok(new { message = "Trust score updated", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trust score");
            return StatusCode(500, new { error = "Failed to update trust score" });
        }
    }

    /// <summary>
    /// Get security recommendations
    /// </summary>
    [HttpGet("security-recommendations")]
    public async Task<ActionResult<List<SecurityRecommendation>>> GetSecurityRecommendations()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var recommendations = await _zeroTrustService.GetSecurityRecommendationsAsync(userId);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security recommendations");
            return StatusCode(500, new { error = "Failed to get security recommendations" });
        }
    }

    /// <summary>
    /// Get real-time security status
    /// </summary>
    [HttpGet("security-status")]
    public async Task<ActionResult<SecurityStatusResponse>> GetSecurityStatus()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Get basic context from request
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers.UserAgent.ToString();

            var context = new ZeroTrustContext
            {
                UserId = userId,
                Network = new NetworkContext { IpAddress = ipAddress },
                Device = new DeviceInfo { UserAgent = userAgent }
            };

            var trustScore = await _zeroTrustService.EvaluateTrustScoreAsync(context);
            var recommendations = await _zeroTrustService.GetSecurityRecommendationsAsync(userId);

            var status = new SecurityStatusResponse
            {
                UserId = userId,
                TrustScore = trustScore,
                SecurityLevel = trustScore.Level.ToString(),
                ActiveThreats = trustScore.Risks.Count,
                Recommendations = recommendations.Take(3).ToList(), // Top 3 recommendations
                LastEvaluated = DateTime.UtcNow
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status");
            return StatusCode(500, new { error = "Failed to get security status" });
        }
    }

    #region Private Helper Methods

    private ZeroTrustContext CreateZeroTrustContext(string userId, ZeroTrustEvaluationRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        return new ZeroTrustContext
        {
            UserId = userId,
            SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
            Device = request.Device ?? new DeviceInfo
            {
                DeviceId = request.DeviceId ?? "Unknown",
                UserAgent = userAgent,
                DeviceType = "Unknown"
            },
            Network = request.Network ?? new NetworkContext
            {
                IpAddress = ipAddress,
                NetworkType = "Unknown"
            },
            Behavior = request.Behavior ?? new UserBehaviorContext(),
            RequestedPermissions = request.RequestedPermissions ?? new List<string>(),
            ResourceId = request.ResourceId ?? "Unknown"
        };
    }

    #endregion
}

/// <summary>
/// Zero Trust evaluation request model
/// </summary>
public class ZeroTrustEvaluationRequest
{
    public string? SessionId { get; set; }
    public string? DeviceId { get; set; }
    public DeviceInfo? Device { get; set; }
    public NetworkContext? Network { get; set; }
    public UserBehaviorContext? Behavior { get; set; }
    public List<string>? RequestedPermissions { get; set; }
    public string? ResourceId { get; set; }
}

/// <summary>
/// Access request DTO
/// </summary>
public class AccessRequestDto
{
    public string ResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ZeroTrustEvaluationRequest Context { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
}

/// <summary>
/// Security status response model
/// </summary>
public class SecurityStatusResponse
{
    public string UserId { get; set; } = string.Empty;
    public TrustScore TrustScore { get; set; } = new();
    public string SecurityLevel { get; set; } = string.Empty;
    public int ActiveThreats { get; set; }
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
    public DateTime LastEvaluated { get; set; }
}