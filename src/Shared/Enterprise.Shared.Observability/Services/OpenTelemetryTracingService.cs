using System.Diagnostics;
using System.Reflection;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Observability.Services;

public class OpenTelemetryTracingService : ITracingService
{
    private static readonly ActivitySource ActivitySource = new("Enterprise.Platform", "1.0.0");
    private readonly ICorrelationContextAccessor _correlationContext;
    private readonly ILogger<OpenTelemetryTracingService> _logger;
    private readonly ObservabilitySettings _settings;

    public OpenTelemetryTracingService(
        ICorrelationContextAccessor correlationContext,
        ILogger<OpenTelemetryTracingService> logger,
        ObservabilitySettings settings)
    {
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Activity? StartActivity(string activityName, ActivityKind kind = ActivityKind.Internal)
    {
        if (!_settings.EnableTracing)
            return null;

        var activity = ActivitySource.StartActivity(activityName, kind);
        
        if (activity != null)
        {
            // Add correlation ID
            var correlationId = _correlationContext.CorrelationContext?.CorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }

            // Add service information
            activity.SetTag("service.name", _settings.ServiceName);
            activity.SetTag("service.version", _settings.ServiceVersion);
            activity.SetTag("service.environment", _settings.Environment);
            activity.SetTag("service.instance", Environment.MachineName);
            
            // Add user context if available
            var userId = _correlationContext.CorrelationContext?.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                activity.SetTag("user.id", userId);
            }
            
        }
        
        return activity;
    }

    public void AddTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    public void AddEvent(string name, Dictionary<string, object>? attributes = null)
    {
        if (Activity.Current == null)
            return;

        var tags = attributes?.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value));
        var tagsCollection = tags != null ? new ActivityTagsCollection(tags) : null;
        
        Activity.Current.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, tagsCollection));
    }

    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        Activity.Current?.SetStatus(status, description);
    }

    public string? GetTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }

    public string? GetSpanId()
    {
        return Activity.Current?.SpanId.ToString();
    }

    public void EnrichWithUserContext(string userId, string? email = null)
    {
        if (Activity.Current == null)
            return;

        Activity.Current.SetTag("user.id", userId);
        
        if (!string.IsNullOrEmpty(email))
        {
            Activity.Current.SetTag("user.email", email);
        }
        
    }

    public void EnrichWithBusinessContext(Dictionary<string, object> businessData)
    {
        if (Activity.Current == null)
            return;

        foreach (var kvp in businessData)
        {
            Activity.Current.SetTag($"business.{kvp.Key}", kvp.Value);
        }
        
    }

    public void RecordException(Exception exception, Dictionary<string, object>? attributes = null)
    {
        if (Activity.Current == null)
            return;

        var tags = new Dictionary<string, object?>
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.StackTrace
        };

        if (attributes != null)
        {
            foreach (var kvp in attributes)
            {
                tags[$"exception.{kvp.Key}"] = kvp.Value;
            }
        }

        // Record exception as event since RecordException is not available in current version
        var eventTags = new ActivityTagsCollection(tags);
        Activity.Current.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, eventTags));
        Activity.Current.SetStatus(ActivityStatusCode.Error, exception.Message);
        
    }

    public Activity? GetCurrentActivity()
    {
        return Activity.Current;
    }
}