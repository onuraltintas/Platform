using Identity.Core.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

/// <summary>
/// Advanced audit and monitoring controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,Auditor")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit events with filtering and pagination
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<PagedAuditResult>> GetAuditEvents([FromQuery] AuditFilterRequest request)
    {
        try
        {
            var filter = new AuditFilter
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                UserId = request.UserId,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                EventType = request.EventType,
                Severity = request.Severity,
                Category = request.Category,
                IsSecurityEvent = request.IsSecurityEvent,
                IpAddress = request.IpAddress,
                SearchTerm = request.SearchTerm,
                Tags = request.Tags ?? new List<string>(),
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? "Timestamp",
                SortDescending = request.SortDescending
            };

            var result = await _auditService.GetAuditEventsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit events");
            return StatusCode(500, new { error = "Failed to get audit events" });
        }
    }

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    [HttpGet("trail/{entityType}/{entityId}")]
    public async Task<ActionResult<List<AuditEvent>>> GetAuditTrail(string entityType, string entityId)
    {
        try
        {
            var trail = await _auditService.GetAuditTrailAsync(entityType, entityId);
            return Ok(trail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail for {EntityType}:{EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to get audit trail" });
        }
    }

    /// <summary>
    /// Generate audit report
    /// </summary>
    [HttpPost("reports/generate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<AuditReport>> GenerateAuditReport([FromBody] AuditReportRequest request)
    {
        try
        {
            var report = await _auditService.GenerateAuditReportAsync(request);

            _logger.LogInformation("Generated audit report '{Title}' by user {UserId}",
                report.Title, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit report");
            return StatusCode(500, new { error = "Failed to generate audit report" });
        }
    }

    /// <summary>
    /// Get security analytics
    /// </summary>
    [HttpGet("security-analytics")]
    public async Task<ActionResult<SecurityAnalytics>> GetSecurityAnalytics(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var analytics = await _auditService.GetSecurityAnalyticsAsync(fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security analytics");
            return StatusCode(500, new { error = "Failed to get security analytics" });
        }
    }

    /// <summary>
    /// Detect anomalies in audit data
    /// </summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<List<AuditAnomaly>>> DetectAnomalies(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var anomalies = await _auditService.DetectAnomaliesAsync(fromDate, toDate);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
            return StatusCode(500, new { error = "Failed to detect anomalies" });
        }
    }

    /// <summary>
    /// Get compliance report
    /// </summary>
    [HttpPost("compliance/report")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ComplianceReport>> GetComplianceReport([FromBody] ComplianceReportRequest request)
    {
        try
        {
            var report = await _auditService.GetComplianceReportAsync(request);

            _logger.LogInformation("Generated compliance report for standard {Standard} by user {UserId}",
                request.Standard, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return StatusCode(500, new { error = "Failed to generate compliance report" });
        }
    }

    /// <summary>
    /// Export audit data
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportAuditData([FromBody] AuditExportRequest request)
    {
        try
        {
            var data = await _auditService.ExportAuditDataAsync(request);
            var fileName = request.FileName ?? $"audit_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.Format.ToLower()}";

            var contentType = request.Format.ToUpper() switch
            {
                "CSV" => "text/csv",
                "JSON" => "application/json",
                "XML" => "application/xml",
                _ => "application/octet-stream"
            };

            _logger.LogInformation("Exported audit data in {Format} format by user {UserId}",
                request.Format, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return File(data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit data");
            return StatusCode(500, new { error = "Failed to export audit data" });
        }
    }

    /// <summary>
    /// Get monitoring dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<MonitoringDashboard>> GetMonitoringDashboard()
    {
        try
        {
            var dashboard = await _auditService.GetMonitoringDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring dashboard");
            return StatusCode(500, new { error = "Failed to get monitoring dashboard" });
        }
    }

    /// <summary>
    /// Create alert rule
    /// </summary>
    [HttpPost("alerts/rules")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<AlertRule>> CreateAlertRule([FromBody] CreateAlertRuleRequest request)
    {
        try
        {
            var rule = await _auditService.CreateAlertRuleAsync(request);

            _logger.LogInformation("Created alert rule '{RuleName}' by user {UserId}",
                rule.Name, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return CreatedAtAction(nameof(GetAlertRule), new { ruleId = rule.Id }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert rule");
            return StatusCode(500, new { error = "Failed to create alert rule" });
        }
    }

    /// <summary>
    /// Get alert rule by ID
    /// </summary>
    [HttpGet("alerts/rules/{ruleId}")]
    public async Task<ActionResult<AlertRule>> GetAlertRule(string ruleId)
    {
        try
        {
            // This would need to be implemented in the service
            return Ok(new AlertRule { Id = ruleId, Name = "Sample Rule" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Failed to get alert rule" });
        }
    }

    /// <summary>
    /// Get active alerts
    /// </summary>
    [HttpGet("alerts/active")]
    public async Task<ActionResult<List<Alert>>> GetActiveAlerts()
    {
        try
        {
            var alerts = await _auditService.GetActiveAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return StatusCode(500, new { error = "Failed to get active alerts" });
        }
    }

    /// <summary>
    /// Acknowledge alert
    /// </summary>
    [HttpPost("alerts/{alertId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(string alertId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            // This would need to be implemented in the service
            _logger.LogInformation("Alert {AlertId} acknowledged by user {UserId}", alertId, userId);

            return Ok(new { message = "Alert acknowledged", alertId, acknowledgedBy = userName, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return StatusCode(500, new { error = "Failed to acknowledge alert" });
        }
    }

    /// <summary>
    /// Resolve alert
    /// </summary>
    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<IActionResult> ResolveAlert(string alertId, [FromBody] ResolveAlertRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            // This would need to be implemented in the service
            _logger.LogInformation("Alert {AlertId} resolved by user {UserId} with reason: {Reason}",
                alertId, userId, request.Reason);

            return Ok(new { message = "Alert resolved", alertId, resolvedBy = userName, reason = request.Reason, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", alertId);
            return StatusCode(500, new { error = "Failed to resolve alert" });
        }
    }

    /// <summary>
    /// Archive old audit data
    /// </summary>
    [HttpPost("archive")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ArchiveResult>> ArchiveAuditData([FromBody] ArchiveRequest request)
    {
        try
        {
            var result = await _auditService.ArchiveAuditDataAsync(request.OlderThan);

            _logger.LogInformation("Archived audit data older than {Date} by user {UserId}",
                request.OlderThan, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit data");
            return StatusCode(500, new { error = "Failed to archive audit data" });
        }
    }

    /// <summary>
    /// Log manual audit event
    /// </summary>
    [HttpPost("events/log")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> LogAuditEvent([FromBody] LogAuditEventRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            var auditEvent = new AuditEvent
            {
                EventType = request.EventType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                UserId = userId,
                UserName = userName,
                Action = request.Action,
                Description = request.Description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = Request.Headers.UserAgent.ToString(),
                Severity = request.Severity,
                Category = request.Category,
                IsSecurityEvent = request.IsSecurityEvent,
                Metadata = request.Metadata,
                Tags = request.Tags
            };

            await _auditService.LogEventAsync(auditEvent);

            return Ok(new { message = "Audit event logged", eventId = auditEvent.Id, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event");
            return StatusCode(500, new { error = "Failed to log audit event" });
        }
    }

    /// <summary>
    /// Get audit statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AuditStatisticsResponse>> GetAuditStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var filter = new AuditFilter
            {
                FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
                ToDate = toDate ?? DateTime.UtcNow,
                Page = 1,
                PageSize = 1000
            };

            var result = await _auditService.GetAuditEventsAsync(filter);

            var statistics = new AuditStatisticsResponse
            {
                TotalEvents = result.Summary.TotalEvents,
                SecurityEvents = result.Summary.SecurityEvents,
                CriticalEvents = result.Summary.CriticalEvents,
                WarningEvents = result.Summary.WarningEvents,
                EventsByCategory = result.Summary.EventsByCategory,
                EventsByType = result.Summary.EventsByType,
                TopUsers = result.Summary.TopUsers,
                TopIpAddresses = result.Summary.TopIpAddresses,
                FromDate = filter.FromDate.Value,
                ToDate = filter.ToDate.Value,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return StatusCode(500, new { error = "Failed to get audit statistics" });
        }
    }
}

/// <summary>
/// Audit filter request model
/// </summary>
public class AuditFilterRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EventType { get; set; }
    public AuditSeverity? Severity { get; set; }
    public AuditCategory? Category { get; set; }
    public bool? IsSecurityEvent { get; set; }
    public string? IpAddress { get; set; }
    public string? SearchTerm { get; set; }
    public List<string>? Tags { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Resolve alert request model
/// </summary>
public class ResolveAlertRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Resolution { get; set; }
}

/// <summary>
/// Archive request model
/// </summary>
public class ArchiveRequest
{
    public DateTime OlderThan { get; set; }
}

/// <summary>
/// Log audit event request model
/// </summary>
public class LogAuditEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
    public AuditCategory Category { get; set; } = AuditCategory.General;
    public bool IsSecurityEvent { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Audit statistics response model
/// </summary>
public class AuditStatisticsResponse
{
    public int TotalEvents { get; set; }
    public int SecurityEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public Dictionary<string, int> EventsByCategory { get; set; } = new();
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }
}