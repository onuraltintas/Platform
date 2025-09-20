namespace Enterprise.Shared.Auditing.HealthChecks;

/// <summary>
/// Health check for audit services
/// </summary>
public class AuditHealthCheck : IHealthCheck
{
    private readonly IAuditStore _auditStore;
    private readonly IAuditService _auditService;
    private readonly AuditConfiguration _configuration;
    private readonly ILogger<AuditHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the AuditHealthCheck
    /// </summary>
    public AuditHealthCheck(
        IAuditStore auditStore,
        IAuditService auditService,
        IOptions<AuditConfiguration> configuration,
        ILogger<AuditHealthCheck> logger)
    {
        _auditStore = auditStore ?? throw new ArgumentNullException(nameof(auditStore));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs the health check
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["enabled"] = _configuration.Enabled,
            ["service_name"] = _configuration.DefaultServiceName,
            ["environment"] = _configuration.DefaultEnvironment
        };

        try
        {
            // Check if auditing is enabled
            if (!_configuration.Enabled)
            {
                return HealthCheckResult.Healthy("Audit system is disabled", data);
            }

            // Test audit store connectivity
            var storeResult = await _auditStore.HealthCheckAsync(cancellationToken);
            if (!storeResult.IsSuccess)
            {
                data["store_error"] = storeResult.Error ?? "Unknown store error";
                return HealthCheckResult.Degraded("Audit store health check failed", null, data);
            }

            // Test basic audit functionality
            var testEvent = AuditEvent.Create("HealthCheck", "AuditSystem", "Success")
                .WithMetadata(new Dictionary<string, object>
                {
                    ["HealthCheckId"] = Guid.NewGuid().ToString(),
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Source"] = "AuditHealthCheck"
                });

            var serviceResult = await _auditService.LogEventAsync(testEvent, cancellationToken);
            if (!serviceResult.IsSuccess)
            {
                data["service_error"] = serviceResult.Error ?? "Unknown service error";
                return HealthCheckResult.Unhealthy("Audit service test failed", null, data);
            }

            // Get basic statistics for additional health information
            try
            {
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddHours(-1); // Last hour
                var eventCount = await _auditStore.GetEventCountAsync(startDate, endDate, cancellationToken: cancellationToken);
                data["events_last_hour"] = eventCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get event count for health check");
                data["stats_warning"] = "Could not retrieve event statistics";
            }

            // Check configuration validity
            var configValidation = _configuration.Validate();
            if (!configValidation.IsSuccess)
            {
                data["config_error"] = configValidation.Error ?? "Unknown configuration error";
                return HealthCheckResult.Degraded("Configuration validation failed", null, data);
            }

            data["status"] = "All audit system components are healthy";
            return HealthCheckResult.Healthy("Audit system is healthy", data);
        }
        catch (OperationCanceledException)
        {
            data["error"] = "Health check timed out";
            return HealthCheckResult.Unhealthy("Audit health check timed out", null, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit health check failed with unexpected error");
            data["error"] = ex.Message;
            data["exception_type"] = ex.GetType().Name;
            
            return HealthCheckResult.Unhealthy("Audit health check failed", ex, data);
        }
    }
}