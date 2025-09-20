using System.Diagnostics;
using Enterprise.Shared.Observability.Models;

namespace Enterprise.Shared.Observability.Interfaces;

public interface ITracingService
{
    Activity? StartActivity(string activityName, ActivityKind kind = ActivityKind.Internal);
    void AddTag(string key, object? value);
    void AddEvent(string name, Dictionary<string, object>? attributes = null);
    void SetStatus(ActivityStatusCode status, string? description = null);
    string? GetTraceId();
    string? GetSpanId();
    void EnrichWithUserContext(string userId, string? email = null);
    void EnrichWithBusinessContext(Dictionary<string, object> businessData);
    void RecordException(Exception exception, Dictionary<string, object>? attributes = null);
    Activity? GetCurrentActivity();
}

public interface IMetricsService
{
    void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object>[] tags);
    void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags);
    void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags);
    void RecordBusinessMetric(string metricName, double value, Dictionary<string, object>? dimensions = null);
    void IncrementUserAction(string action, string userId);
    void RecordApiCall(string method, string endpoint, int statusCode, double durationMs);
    void RecordDatabaseQuery(string operation, string table, double durationMs, bool success);
    void RecordCacheOperation(string operation, string key, bool hit, double durationMs);
    Task<ApplicationMetrics> GetApplicationMetricsAsync();
}

public interface ICorrelationContextAccessor
{
    CorrelationContext? CorrelationContext { get; set; }
    string? CorrelationId { get; }
}

public interface IBusinessMetricsCollector
{
    Task RecordUserRegistrationAsync(string userId, string registrationSource, Dictionary<string, object>? metadata = null);
    Task RecordUserLoginAsync(string userId, bool success, string? failureReason = null);
    Task RecordOrderCreatedAsync(string orderId, decimal amount, string currency, string userId);
    Task RecordPaymentProcessedAsync(string paymentId, decimal amount, string paymentMethod, bool success);
    Task RecordFeatureUsageAsync(string featureName, string userId, Dictionary<string, object>? context = null);
    Task RecordCustomEventAsync(string eventName, Dictionary<string, object> properties);
    Task<BusinessMetricsReport> GenerateReportAsync(DateTime from, DateTime to);
}

public interface IBusinessMetricsRepository
{
    Task StoreMetricAsync(BusinessMetricData metric);
    Task<IEnumerable<BusinessMetricData>> GetMetricsAsync(string metricName, DateTime from, DateTime to);
    Task<IEnumerable<BusinessMetricData>> GetMetricsByUserAsync(string userId, DateTime from, DateTime to);
    Task CleanupExpiredMetricsAsync(DateTime cutoffDate);
}