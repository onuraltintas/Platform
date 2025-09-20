using Enterprise.Shared.Configuration.Interfaces;
using Enterprise.Shared.Configuration.Models;

namespace Enterprise.Shared.Configuration.Services;

/// <summary>
/// In-memory implementation of configuration change tracking
/// In production, this should be replaced with a persistent storage implementation
/// </summary>
public sealed class ConfigurationChangeTracker : IConfigurationChangeTracker, IDisposable
{
    private readonly ILogger<ConfigurationChangeTracker> _logger;
    private readonly List<ConfigurationChangeRecord> _changeHistory;
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationChangeTracker(ILogger<ConfigurationChangeTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _changeHistory = new List<ConfigurationChangeRecord>();
    }

    /// <inheritdoc/>
    public async Task TrackChangeAsync(string key, object? oldValue, object? newValue, 
        string? changedBy = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var changeRecord = new ConfigurationChangeRecord
            {
                Id = Guid.NewGuid(),
                Key = key,
                OldValue = oldValue?.ToString(),
                NewValue = newValue?.ToString(),
                ChangedBy = changedBy ?? "System",
                ChangedAt = DateTime.UtcNow,
                Reason = reason,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Source = "ConfigurationService"
            };

            lock (_lockObject)
            {
                _changeHistory.Add(changeRecord);
                
                // Keep only last 1000 changes to prevent memory issues
                if (_changeHistory.Count > 1000)
                {
                    _changeHistory.RemoveAt(0);
                }
            }

            // Fire event
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = changeRecord.ChangedAt,
                ChangedBy = changedBy
            });

            _logger.LogInformation("Configuration change tracked: {Key} changed from {OldValue} to {NewValue} by {ChangedBy} - {Reason}",
                key, oldValue, newValue, changedBy ?? "System", reason ?? "No reason provided");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking configuration change for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ConfigurationChangeRecord>> GetChangeHistoryAsync(string key, 
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            IEnumerable<ConfigurationChangeRecord> query;
            
            lock (_lockObject)
            {
                query = _changeHistory.Where(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (from.HasValue)
            {
                query = query.Where(r => r.ChangedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.ChangedAt <= to.Value);
            }

            var result = query.OrderByDescending(r => r.ChangedAt).ToList();
            
            
            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving change history for key: {Key}", key);
            return Enumerable.Empty<ConfigurationChangeRecord>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ConfigurationChangeRecord>> GetAllChangesAsync(
        DateTime? from = null, DateTime? to = null, string? changedBy = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<ConfigurationChangeRecord> query;
            
            lock (_lockObject)
            {
                query = _changeHistory.ToList();
            }

            if (from.HasValue)
            {
                query = query.Where(r => r.ChangedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.ChangedAt <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(changedBy))
            {
                query = query.Where(r => r.ChangedBy.Equals(changedBy, StringComparison.OrdinalIgnoreCase));
            }

            var result = query.OrderByDescending(r => r.ChangedAt).ToList();
            
            
            await Task.CompletedTask;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all change history");
            return Enumerable.Empty<ConfigurationChangeRecord>();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetChangeStatisticsAsync(DateTime from, DateTime to, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<ConfigurationChangeRecord> query;
            
            lock (_lockObject)
            {
                query = _changeHistory.Where(r => r.ChangedAt >= from && r.ChangedAt <= to).ToList();
            }

            var statistics = new Dictionary<string, object>
            {
                ["TotalChanges"] = query.Count(),
                ["UniqueKeys"] = query.Select(r => r.Key).Distinct().Count(),
                ["UniqueUsers"] = query.Select(r => r.ChangedBy).Distinct().Count(),
                ["ChangesByUser"] = query.GroupBy(r => r.ChangedBy)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["ChangesByKey"] = query.GroupBy(r => r.Key)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["ChangesByDay"] = query.GroupBy(r => r.ChangedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                ["Period"] = new { From = from, To = to }
            };

            
            await Task.CompletedTask;
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating change statistics for period {From} to {To}", from, to);
            return new Dictionary<string, object>();
        }
    }

    /// <inheritdoc/>
    public async Task<int> CleanupHistoryAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            int removedCount;
            
            lock (_lockObject)
            {
                var itemsToRemove = _changeHistory.Where(r => r.ChangedAt < olderThan).ToList();
                removedCount = itemsToRemove.Count;
                
                foreach (var item in itemsToRemove)
                {
                    _changeHistory.Remove(item);
                }
            }

            _logger.LogInformation("Cleaned up {Count} configuration change records older than {OlderThan}", 
                removedCount, olderThan);
            
            await Task.CompletedTask;
            return removedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up configuration change history");
            return 0;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lockObject)
            {
                _changeHistory.Clear();
            }
            
            ConfigurationChanged = null;
            _disposed = true;
        }
    }
}