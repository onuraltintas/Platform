using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Observability.Configuration;

namespace EgitimPlatform.Shared.Observability.Metrics;

public class ApplicationMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly ObservabilityOptions _options;

    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _authenticationAttemptCounter;
    private readonly Counter<long> _authenticationSuccessCounter;
    private readonly Counter<long> _authenticationFailureCounter;
    private readonly Counter<long> _userRegistrationCounter;
    private readonly Counter<long> _courseEnrollmentCounter;
    private readonly Counter<long> _lessonCompletionCounter;
    private readonly Counter<long> _messagePublishedCounter;
    private readonly Counter<long> _messageConsumedCounter;
    private readonly Counter<long> _databaseQueryCounter;

    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _messageProcessingDuration;
    private readonly Histogram<long> _requestSize;
    private readonly Histogram<long> _responseSize;

    // Gauges (UpDownCounters)
    private readonly UpDownCounter<long> _activeConnections;
    private readonly UpDownCounter<long> _activeUsers;
    private readonly UpDownCounter<long> _memoryUsage;
    private readonly UpDownCounter<long> _cpuUsage;

    public ApplicationMetrics(IOptions<ObservabilityOptions> options)
    {
        _options = options.Value;
        _meter = new Meter(_options.ServiceName, _options.ServiceVersion);

        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>(
            "http_requests_total",
            "requests",
            "Total number of HTTP requests");

        _errorCounter = _meter.CreateCounter<long>(
            "http_errors_total",
            "errors",
            "Total number of HTTP errors");

        _authenticationAttemptCounter = _meter.CreateCounter<long>(
            "authentication_attempts_total",
            "attempts",
            "Total number of authentication attempts");

        _authenticationSuccessCounter = _meter.CreateCounter<long>(
            "authentication_success_total",
            "successes",
            "Total number of successful authentications");

        _authenticationFailureCounter = _meter.CreateCounter<long>(
            "authentication_failures_total",
            "failures",
            "Total number of failed authentications");

        _userRegistrationCounter = _meter.CreateCounter<long>(
            "user_registrations_total",
            "registrations",
            "Total number of user registrations");

        _courseEnrollmentCounter = _meter.CreateCounter<long>(
            "course_enrollments_total",
            "enrollments",
            "Total number of course enrollments");

        _lessonCompletionCounter = _meter.CreateCounter<long>(
            "lesson_completions_total",
            "completions",
            "Total number of lesson completions");

        _messagePublishedCounter = _meter.CreateCounter<long>(
            "messages_published_total",
            "messages",
            "Total number of messages published");

        _messageConsumedCounter = _meter.CreateCounter<long>(
            "messages_consumed_total",
            "messages",
            "Total number of messages consumed");

        _databaseQueryCounter = _meter.CreateCounter<long>(
            "database_queries_total",
            "queries",
            "Total number of database queries");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>(
            "http_request_duration_seconds",
            "seconds",
            "HTTP request duration in seconds");

        _databaseQueryDuration = _meter.CreateHistogram<double>(
            "database_query_duration_seconds",
            "seconds",
            "Database query duration in seconds");

        _messageProcessingDuration = _meter.CreateHistogram<double>(
            "message_processing_duration_seconds",
            "seconds",
            "Message processing duration in seconds");

        _requestSize = _meter.CreateHistogram<long>(
            "http_request_size_bytes",
            "bytes",
            "HTTP request size in bytes");

        _responseSize = _meter.CreateHistogram<long>(
            "http_response_size_bytes",
            "bytes",
            "HTTP response size in bytes");

        // Initialize gauges
        _activeConnections = _meter.CreateUpDownCounter<long>(
            "active_connections",
            "connections",
            "Number of active connections");

        _activeUsers = _meter.CreateUpDownCounter<long>(
            "active_users",
            "users",
            "Number of active users");

        _memoryUsage = _meter.CreateUpDownCounter<long>(
            "memory_usage_bytes",
            "bytes",
            "Memory usage in bytes");

        _cpuUsage = _meter.CreateUpDownCounter<long>(
            "cpu_usage_percent",
            "percent",
            "CPU usage percentage");
    }

    // Counter methods
    public void IncrementHttpRequests(string method, string endpoint, int statusCode)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                               new KeyValuePair<string, object?>("endpoint", endpoint),
                               new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void IncrementHttpErrors(string method, string endpoint, int statusCode, string errorType)
    {
        _errorCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                             new KeyValuePair<string, object?>("endpoint", endpoint),
                             new KeyValuePair<string, object?>("status_code", statusCode),
                             new KeyValuePair<string, object?>("error_type", errorType));
    }

    public void IncrementAuthenticationAttempts(string provider = "local")
    {
        _authenticationAttemptCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
    }

    public void IncrementAuthenticationSuccess(string provider = "local")
    {
        _authenticationSuccessCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
    }

    public void IncrementAuthenticationFailures(string provider = "local", string reason = "invalid_credentials")
    {
        _authenticationFailureCounter.Add(1, new KeyValuePair<string, object?>("provider", provider),
                                              new KeyValuePair<string, object?>("reason", reason));
    }

    public void IncrementUserRegistrations(string provider = "local")
    {
        _userRegistrationCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
    }

    public void IncrementCourseEnrollments(string courseId, string category)
    {
        _courseEnrollmentCounter.Add(1, new KeyValuePair<string, object?>("course_id", courseId),
                                        new KeyValuePair<string, object?>("category", category));
    }

    public void IncrementLessonCompletions(string courseId, string lessonId)
    {
        _lessonCompletionCounter.Add(1, new KeyValuePair<string, object?>("course_id", courseId),
                                        new KeyValuePair<string, object?>("lesson_id", lessonId));
    }

    public void IncrementMessagesPublished(string eventType, string exchange)
    {
        _messagePublishedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType),
                                        new KeyValuePair<string, object?>("exchange", exchange));
    }

    public void IncrementMessagesConsumed(string eventType, string queue, bool success)
    {
        _messageConsumedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType),
                                       new KeyValuePair<string, object?>("queue", queue),
                                       new KeyValuePair<string, object?>("success", success));
    }

    public void IncrementDatabaseQueries(string operation, string table, bool success)
    {
        _databaseQueryCounter.Add(1, new KeyValuePair<string, object?>("operation", operation),
                                     new KeyValuePair<string, object?>("table", table),
                                     new KeyValuePair<string, object?>("success", success));
    }

    // Histogram methods
    public void RecordHttpRequestDuration(double duration, string method, string endpoint, int statusCode)
    {
        _requestDuration.Record(duration, new KeyValuePair<string, object?>("method", method),
                                         new KeyValuePair<string, object?>("endpoint", endpoint),
                                         new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void RecordDatabaseQueryDuration(double duration, string operation, string table)
    {
        _databaseQueryDuration.Record(duration, new KeyValuePair<string, object?>("operation", operation),
                                                new KeyValuePair<string, object?>("table", table));
    }

    public void RecordMessageProcessingDuration(double duration, string eventType, string handler)
    {
        _messageProcessingDuration.Record(duration, new KeyValuePair<string, object?>("event_type", eventType),
                                                   new KeyValuePair<string, object?>("handler", handler));
    }

    public void RecordHttpRequestSize(long size, string method, string endpoint)
    {
        _requestSize.Record(size, new KeyValuePair<string, object?>("method", method),
                                 new KeyValuePair<string, object?>("endpoint", endpoint));
    }

    public void RecordHttpResponseSize(long size, string method, string endpoint, int statusCode)
    {
        _responseSize.Record(size, new KeyValuePair<string, object?>("method", method),
                                  new KeyValuePair<string, object?>("endpoint", endpoint),
                                  new KeyValuePair<string, object?>("status_code", statusCode));
    }

    // Gauge methods
    public void SetActiveConnections(long count)
    {
        _activeConnections.Add(count);
    }

    public void IncrementActiveConnections()
    {
        _activeConnections.Add(1);
    }

    public void DecrementActiveConnections()
    {
        _activeConnections.Add(-1);
    }

    public void SetActiveUsers(long count)
    {
        _activeUsers.Add(count);
    }

    public void IncrementActiveUsers()
    {
        _activeUsers.Add(1);
    }

    public void DecrementActiveUsers()
    {
        _activeUsers.Add(-1);
    }

    public void SetMemoryUsage(long bytes)
    {
        _memoryUsage.Add(bytes);
    }

    public void SetCpuUsage(long percentage)
    {
        _cpuUsage.Add(percentage);
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}