namespace Enterprise.Shared.Auditing.Services;

/// <summary>
/// In-memory implementation of audit store (for testing and development)
/// </summary>
public class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentDictionary<string, AuditEvent> _events = new();
    private readonly ILogger<InMemoryAuditStore> _logger;
    private readonly AuditConfiguration _configuration;
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Initializes a new instance of the InMemoryAuditStore
    /// </summary>
    public InMemoryAuditStore(
        IOptions<AuditConfiguration> configuration,
        ILogger<InMemoryAuditStore> logger)
    {
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result> StoreEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent == null)
        {
            return Task.FromResult(Result.Failure("Audit event cannot be null"));
        }

        try
        {
            _lock.EnterWriteLock();
            try
            {
                _events[auditEvent.Id] = auditEvent;
                _logger.LogTrace("Stored audit event {EventId} in memory", auditEvent.Id);
                return Task.FromResult(Result.Success());
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing audit event {EventId} in memory", auditEvent.Id);
            return Task.FromResult(Result.Failure($"Failed to store audit event: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> StoreEventsAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents == null)
        {
            return Task.FromResult(Result.Failure("Audit events collection cannot be null"));
        }

        var eventList = auditEvents.ToList();
        if (eventList.Count == 0)
        {
            return Task.FromResult(Result.Success());
        }

        try
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var auditEvent in eventList)
                {
                    _events[auditEvent.Id] = auditEvent;
                }
                
                _logger.LogTrace("Stored {Count} audit events in memory", eventList.Count);
                return Task.FromResult(Result.Success());
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing {Count} audit events in memory", eventList.Count);
            return Task.FromResult(Result.Failure($"Failed to store audit events: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<(List<AuditEvent> Events, int TotalCount)> QueryEventsAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        try
        {
            _lock.EnterReadLock();
            try
            {
                var query = _events.Values.AsEnumerable();

                // Apply filters
                query = ApplyFilters(query, criteria);

                // Get total count before pagination
                var totalCount = query.Count();

                // Apply sorting
                query = ApplySorting(query, criteria);

                // Apply pagination
                var events = query
                    .Skip(criteria.Skip)
                    .Take(criteria.Take)
                    .ToList();

                _logger.LogTrace("Queried {Count} of {Total} audit events from memory", events.Count, totalCount);

                return Task.FromResult((events, totalCount));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying audit events from memory");
            return Task.FromResult((new List<AuditEvent>(), 0));
        }
    }

    /// <inheritdoc />
    public Task<AuditEvent?> GetEventAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<AuditEvent?>(null);
        }

        try
        {
            _lock.EnterReadLock();
            try
            {
                _events.TryGetValue(id, out var auditEvent);
                return Task.FromResult(auditEvent);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit event {EventId} from memory", id);
            return Task.FromResult<AuditEvent?>(null);
        }
    }

    /// <inheritdoc />
    public Task<List<AuditEvent>> GetEventsByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Task.FromResult(new List<AuditEvent>());
        }

        try
        {
            _lock.EnterReadLock();
            try
            {
                var events = _events.Values
                    .Where(e => e.CorrelationId == correlationId)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                return Task.FromResult(events);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events by correlation {CorrelationId} from memory", correlationId);
            return Task.FromResult(new List<AuditEvent>());
        }
    }

    /// <inheritdoc />
    public Task<int> DeleteEventsAsync(DateTime olderThan, AuditEventCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _lock.EnterWriteLock();
            try
            {
                var eventsToDelete = _events.Values
                    .Where(e => e.Timestamp < olderThan)
                    .Where(e => categories == null || categories.Contains(e.Category))
                    .ToList();

                foreach (var eventToDelete in eventsToDelete)
                {
                    _events.TryRemove(eventToDelete.Id, out _);
                }

                _logger.LogInformation("Deleted {Count} audit events older than {Date} from memory", 
                    eventsToDelete.Count, olderThan);

                return Task.FromResult(eventsToDelete.Count);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audit events older than {Date} from memory", olderThan);
            return Task.FromResult(0);
        }
    }

    /// <inheritdoc />
    public Task<AuditStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _lock.EnterReadLock();
            try
            {
                var events = _events.Values
                    .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                    .ToList();

                var statistics = new AuditStatistics
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalEvents = events.Count,
                    UniqueUsers = events.Where(e => !string.IsNullOrEmpty(e.UserId))
                                       .Select(e => e.UserId!)
                                       .Distinct()
                                       .Count(),
                    SecurityAlerts = events.Count(e => e is SecurityAuditEvent se && se.IsAlert),
                    FailedOperations = events.Count(e => e.Result == "Failed"),
                    EventsByCategory = events.GroupBy(e => e.Category)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                    EventsBySeverity = events.GroupBy(e => e.Severity)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                    EventsByResult = events.GroupBy(e => e.Result)
                                         .ToDictionary(g => g.Key, g => g.Count()),
                    CommonActions = events.GroupBy(e => e.Action)
                                        .OrderByDescending(g => g.Count())
                                        .Take(10)
                                        .ToDictionary(g => g.Key, g => g.Count()),
                    ActiveUsers = events.Where(e => !string.IsNullOrEmpty(e.UserId))
                                       .GroupBy(e => e.UserId!)
                                       .OrderByDescending(g => g.Count())
                                       .Take(10)
                                       .ToDictionary(g => g.Key, g => g.Count()),
                    AccessedResources = events.GroupBy(e => e.Resource)
                                            .OrderByDescending(g => g.Count())
                                            .Take(10)
                                            .ToDictionary(g => g.Key, g => g.Count()),
                    EventsByDay = events.GroupBy(e => e.Timestamp.Date)
                                       .ToDictionary(g => g.Key, g => g.Count()),
                    EventsByHour = events.GroupBy(e => e.Timestamp.Hour)
                                        .ToDictionary(g => g.Key, g => g.Count()),
                    TopIpAddresses = events.Where(e => !string.IsNullOrEmpty(e.IpAddress))
                                          .GroupBy(e => e.IpAddress!)
                                          .OrderByDescending(g => g.Count())
                                          .Take(10)
                                          .ToDictionary(g => g.Key, g => g.Count()),
                    AverageResponseTime = events.Where(e => e.DurationMs.HasValue)
                                               .Select(e => e.DurationMs!.Value)
                                               .DefaultIfEmpty(0)
                                               .Average()
                };

                statistics.SuccessRate = statistics.TotalEvents > 0 
                    ? (double)(statistics.TotalEvents - statistics.FailedOperations) / statistics.TotalEvents * 100
                    : 100;

                return Task.FromResult(statistics);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating audit statistics from memory");
            return Task.FromResult(new AuditStatistics { StartDate = startDate, EndDate = endDate });
        }
    }

    /// <inheritdoc />
    public Task<Result> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var eventCount = _events.Count;
            _logger.LogTrace("In-memory audit store health check: {EventCount} events stored", eventCount);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "In-memory audit store health check failed");
            return Task.FromResult(Result.Failure($"Health check failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<int> GetEventCountAsync(DateTime startDate, DateTime endDate, AuditEventCategory? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _lock.EnterReadLock();
            try
            {
                var query = _events.Values
                    .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate);

                if (category.HasValue)
                {
                    query = query.Where(e => e.Category == category.Value);
                }

                return Task.FromResult(query.Count());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event count from memory");
            return Task.FromResult(0);
        }
    }

    /// <inheritdoc />
    public Task<byte[]> ExportEventsAsync(AuditSearchCriteria criteria, AuditExportFormat format, CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation - in real scenarios, you'd implement proper export logic
        try
        {
            var (events, _) = QueryEventsAsync(criteria, cancellationToken).Result;
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(json));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting events from memory");
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    /// <summary>
    /// Applies search filters to the query
    /// </summary>
    private static IEnumerable<AuditEvent> ApplyFilters(IEnumerable<AuditEvent> query, AuditSearchCriteria criteria)
    {
        if (criteria.StartDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= criteria.StartDate.Value);
        }

        if (criteria.EndDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= criteria.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.UserId))
        {
            query = query.Where(e => e.UserId == criteria.UserId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Username))
        {
            query = query.Where(e => e.Username == criteria.Username);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(e => e.Action.Contains(criteria.Action, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Resource))
        {
            query = query.Where(e => e.Resource.Contains(criteria.Resource, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.ResourceId))
        {
            query = query.Where(e => e.ResourceId == criteria.ResourceId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Result))
        {
            query = query.Where(e => e.Result == criteria.Result);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ServiceName))
        {
            query = query.Where(e => e.ServiceName == criteria.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CorrelationId))
        {
            query = query.Where(e => e.CorrelationId == criteria.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.IpAddress))
        {
            query = query.Where(e => e.IpAddress == criteria.IpAddress);
        }

        if (criteria.Category.HasValue)
        {
            query = query.Where(e => e.Category == criteria.Category.Value);
        }

        if (criteria.MinSeverity.HasValue)
        {
            query = query.Where(e => e.Severity >= criteria.MinSeverity.Value);
        }

        if (criteria.Tags.Count > 0)
        {
            query = query.Where(e => criteria.Tags.All(tag => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            query = query.Where(e => 
                (e.Details != null && e.Details.Contains(criteria.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (e.Metadata != null && e.Metadata.Contains(criteria.SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Environment))
        {
            query = query.Where(e => e.Environment == criteria.Environment);
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the query
    /// </summary>
    private static IEnumerable<AuditEvent> ApplySorting(IEnumerable<AuditEvent> query, AuditSearchCriteria criteria)
    {
        return criteria.SortBy?.ToLowerInvariant() switch
        {
            "timestamp" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Timestamp) 
                : query.OrderByDescending(e => e.Timestamp),
            "action" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Action) 
                : query.OrderByDescending(e => e.Action),
            "resource" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Resource) 
                : query.OrderByDescending(e => e.Resource),
            "userid" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.UserId) 
                : query.OrderByDescending(e => e.UserId),
            "severity" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Severity) 
                : query.OrderByDescending(e => e.Severity),
            "category" => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Category) 
                : query.OrderByDescending(e => e.Category),
            _ => criteria.SortDirection == Models.SortDirection.Ascending 
                ? query.OrderBy(e => e.Timestamp) 
                : query.OrderByDescending(e => e.Timestamp)
        };
    }

    /// <summary>
    /// Disposes the store
    /// </summary>
    public void Dispose()
    {
        _lock?.Dispose();
    }
}