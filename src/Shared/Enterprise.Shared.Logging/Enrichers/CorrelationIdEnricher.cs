using Enterprise.Shared.Logging.Interfaces;
using Serilog.Core;
using Serilog.Events;

namespace Enterprise.Shared.Logging.Enrichers;

/// <summary>
/// Serilog enricher that adds correlation ID to log events
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly ICorrelationContextAccessor? _correlationContextAccessor;

    public CorrelationIdEnricher(ICorrelationContextAccessor? correlationContextAccessor = null)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationContext = _correlationContextAccessor?.CorrelationContext;
        if (correlationContext == null) return;

        if (!string.IsNullOrEmpty(correlationContext.CorrelationId))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CorrelationId", correlationContext.CorrelationId));
        }

        if (!string.IsNullOrEmpty(correlationContext.ParentCorrelationId))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ParentCorrelationId", correlationContext.ParentCorrelationId));
        }

        if (!string.IsNullOrEmpty(correlationContext.RequestId))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("RequestId", correlationContext.RequestId));
        }

        if (!string.IsNullOrEmpty(correlationContext.SessionId))
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SessionId", correlationContext.SessionId));
        }

        // Add custom correlation properties
        foreach (var property in correlationContext.Properties)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, property.Value, true));
        }
    }
}