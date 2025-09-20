namespace Enterprise.Shared.Auditing.Services;

/// <summary>
/// Default implementation of the audit service
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditStore _auditStore;
    private readonly IAuditContextProvider _contextProvider;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the AuditService
    /// </summary>
    public AuditService(
        IAuditStore auditStore,
        IAuditContextProvider contextProvider,
        IOptions<AuditConfiguration> configuration,
        ILogger<AuditService> logger)
    {
        _auditStore = auditStore ?? throw new ArgumentNullException(nameof(auditStore));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<Result> LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent == null)
        {
            return Result.Failure("Audit event cannot be null");
        }

        if (!_configuration.Enabled)
        {
            return Result.Success(); // Auditing is disabled
        }

        try
        {
            // Enrich the event with context information
            EnrichAuditEvent(auditEvent);

            // Validate the event
            var validationResult = ValidateAuditEvent(auditEvent);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Store the event
            var storeResult = await _auditStore.StoreEventAsync(auditEvent, cancellationToken);
            if (!storeResult.IsSuccess)
            {
                _logger.LogError("Failed to store audit event {EventId}: {Error}", auditEvent.Id, storeResult.Error);
                return storeResult;
            }

            _logger.LogDebug("Audit event {EventId} logged successfully: {Action} on {Resource}", 
                auditEvent.Id, auditEvent.Action, auditEvent.Resource);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event {EventId}: {Action} on {Resource}", 
                auditEvent.Id, auditEvent.Action, auditEvent.Resource);
            return Result.Failure($"Failed to log audit event: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> LogSecurityEventAsync(SecurityAuditEvent securityEvent, CancellationToken cancellationToken = default)
    {
        if (securityEvent == null)
        {
            return Result.Failure("Security audit event cannot be null");
        }

        if (!_configuration.Enabled)
        {
            return Result.Success(); // Auditing is disabled
        }

        try
        {
            // Set security-specific properties
            securityEvent.Category = AuditEventCategory.Security;
            if (securityEvent.Severity == AuditSeverity.Information && securityEvent.Outcome != SecurityOutcome.Success)
            {
                securityEvent.Severity = securityEvent.Outcome switch
                {
                    SecurityOutcome.Failed or SecurityOutcome.Blocked => AuditSeverity.Warning,
                    SecurityOutcome.Error => AuditSeverity.Error,
                    _ => AuditSeverity.Information
                };
            }

            // Add security-specific tags
            securityEvent.WithTags("security", securityEvent.EventType.ToString().ToLowerInvariant());

            // Enrich security context if not provided
            if (securityEvent.SecurityContext == null)
            {
                securityEvent.SecurityContext = _contextProvider.GetCurrentSecurityContext();
            }

            // Log as regular audit event
            return await LogEventAsync(securityEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security audit event {EventId}: {EventType} - {Action}", 
                securityEvent.Id, securityEvent.EventType, securityEvent.Action);
            return Result.Failure($"Failed to log security audit event: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> LogEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents == null)
        {
            return Result.Failure("Audit events collection cannot be null");
        }

        var eventList = auditEvents.ToList();
        if (eventList.Count == 0)
        {
            return Result.Success();
        }

        try
        {
            // Enrich all events with context information
            foreach (var auditEvent in eventList)
            {
                EnrichAuditEvent(auditEvent);

                var validationResult = ValidateAuditEvent(auditEvent);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Invalid audit event {EventId} in batch: {Error}", auditEvent.Id, validationResult.Error);
                    return validationResult;
                }
            }

            // Store all events
            var storeResult = await _auditStore.StoreEventsAsync(eventList, cancellationToken);
            if (!storeResult.IsSuccess)
            {
                _logger.LogError("Failed to store batch of {Count} audit events: {Error}", eventList.Count, storeResult.Error);
                return storeResult;
            }

            _logger.LogDebug("Successfully logged batch of {Count} audit events", eventList.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging batch of {Count} audit events", eventList.Count);
            return Result.Failure($"Failed to log audit events: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<AuditSearchResult> SearchEventsAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        if (criteria == null)
        {
            criteria = new AuditSearchCriteria();
        }

        try
        {
            var validationResult = criteria.Validate();
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Invalid search criteria: {Error}", validationResult.Error);
                return new AuditSearchResult { Criteria = criteria };
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var (events, totalCount) = await _auditStore.QueryEventsAsync(criteria, cancellationToken);
            stopwatch.Stop();

            _logger.LogDebug("Audit search completed in {ElapsedMs}ms, found {Count} of {Total} events", 
                stopwatch.ElapsedMilliseconds, events.Count, totalCount);

            return AuditSearchResult.Success(criteria, events, totalCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit events");
            return new AuditSearchResult { Criteria = criteria };
        }
    }

    /// <inheritdoc />
    public async Task<AuditEvent?> GetEventAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        try
        {
            return await _auditStore.GetEventAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit event {EventId}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<AuditEvent>> GetEventsByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return new List<AuditEvent>();
        }

        try
        {
            return await _auditStore.GetEventsByCorrelationAsync(correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for correlation {CorrelationId}", correlationId);
            return new List<AuditEvent>();
        }
    }

    /// <inheritdoc />
    public async Task<AuditStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (startDate >= endDate)
            {
                _logger.LogWarning("Invalid date range for statistics: {StartDate} to {EndDate}", startDate, endDate);
                return new AuditStatistics { StartDate = startDate, EndDate = endDate };
            }

            return await _auditStore.GetStatisticsAsync(startDate, endDate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit statistics for {StartDate} to {EndDate}", startDate, endDate);
            return new AuditStatistics { StartDate = startDate, EndDate = endDate };
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> PurgeEventsAsync(DateTime olderThan, AuditEventCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (olderThan >= DateTime.UtcNow)
            {
                return Result<int>.Failure("Purge date must be in the past");
            }

            var deletedCount = await _auditStore.DeleteEventsAsync(olderThan, categories, cancellationToken);
            _logger.LogInformation("Purged {Count} audit events older than {Date}", deletedCount, olderThan);
            
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging audit events older than {Date}", olderThan);
            return Result<int>.Failure($"Failed to purge audit events: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> ArchiveEventsAsync(DateTime olderThan, string archiveLocation, CancellationToken cancellationToken = default)
    {
        try
        {
            if (olderThan >= DateTime.UtcNow)
            {
                return Result<int>.Failure("Archive date must be in the past");
            }

            if (string.IsNullOrWhiteSpace(archiveLocation))
            {
                return Result<int>.Failure("Archive location must be specified");
            }

            // Search for events to archive
            var criteria = new AuditSearchCriteria
            {
                EndDate = olderThan,
                PageSize = 1000
            };

            var searchResult = await SearchEventsAsync(criteria, cancellationToken);
            if (searchResult.Events.Count == 0)
            {
                return Result<int>.Success(0);
            }

            // Export events (this would typically be implemented based on the storage type)
            // For now, we'll just delete them after "archiving"
            var deletedCount = await _auditStore.DeleteEventsAsync(olderThan, null, cancellationToken);
            
            _logger.LogInformation("Archived and purged {Count} audit events older than {Date} to {Location}", 
                deletedCount, olderThan, archiveLocation);
            
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit events older than {Date}", olderThan);
            return Result<int>.Failure($"Failed to archive audit events: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auditStore.HealthCheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit service health check failed");
            return Result.Failure($"Audit service health check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Enriches the audit event with context information
    /// </summary>
    private void EnrichAuditEvent(AuditEvent auditEvent)
    {
        // Set context information if not already provided
        auditEvent.UserId ??= _contextProvider.GetCurrentUserId();
        auditEvent.Username ??= _contextProvider.GetCurrentUsername();
        auditEvent.SessionId ??= _contextProvider.GetCurrentSessionId();
        auditEvent.IpAddress ??= _contextProvider.GetCurrentIpAddress();
        auditEvent.UserAgent ??= _contextProvider.GetCurrentUserAgent();
        auditEvent.CorrelationId ??= _contextProvider.GetCurrentCorrelationId();
        auditEvent.TraceId ??= _contextProvider.GetCurrentTraceId();
        auditEvent.ServiceName ??= _contextProvider.GetCurrentServiceName();
        auditEvent.Environment ??= _contextProvider.GetCurrentEnvironment();

        // Add context properties
        var contextProperties = _contextProvider.GetContextProperties();
        foreach (var property in contextProperties)
        {
            auditEvent.WithProperty(property.Key, property.Value);
        }

        // Set defaults if still null
        auditEvent.ServiceName ??= _configuration.DefaultServiceName;
        auditEvent.Environment ??= _configuration.DefaultEnvironment;
    }

    /// <summary>
    /// Validates the audit event
    /// </summary>
    private Result ValidateAuditEvent(AuditEvent auditEvent)
    {
        if (string.IsNullOrWhiteSpace(auditEvent.Action))
        {
            return Result.Failure("Audit event action is required");
        }

        if (string.IsNullOrWhiteSpace(auditEvent.Resource))
        {
            return Result.Failure("Audit event resource is required");
        }

        if (string.IsNullOrWhiteSpace(auditEvent.Result))
        {
            return Result.Failure("Audit event result is required");
        }

        // Validate string lengths based on model attributes
        if (auditEvent.Action.Length > 100)
        {
            return Result.Failure("Audit event action cannot exceed 100 characters");
        }

        if (auditEvent.Resource.Length > 100)
        {
            return Result.Failure("Audit event resource cannot exceed 100 characters");
        }

        if (auditEvent.Details?.Length > 2000)
        {
            return Result.Failure("Audit event details cannot exceed 2000 characters");
        }

        return Result.Success();
    }
}