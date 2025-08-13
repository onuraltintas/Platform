using Prometheus;

namespace EgitimPlatform.Shared.Observability.Metrics;

public static class PrometheusMetrics
{
    // Counters
    public static readonly Counter HttpRequestsTotal = Prometheus.Metrics
        .CreateCounter("http_requests_total", "Total number of HTTP requests",
            new[] { "method", "endpoint", "status_code" });

    public static readonly Counter HttpErrorsTotal = Prometheus.Metrics
        .CreateCounter("http_errors_total", "Total number of HTTP errors",
            new[] { "method", "endpoint", "status_code", "error_type" });

    public static readonly Counter AuthenticationAttemptsTotal = Prometheus.Metrics
        .CreateCounter("authentication_attempts_total", "Total number of authentication attempts",
            new[] { "provider" });

    public static readonly Counter AuthenticationSuccessTotal = Prometheus.Metrics
        .CreateCounter("authentication_success_total", "Total number of successful authentications",
            new[] { "provider" });

    public static readonly Counter AuthenticationFailuresTotal = Prometheus.Metrics
        .CreateCounter("authentication_failures_total", "Total number of failed authentications",
            new[] { "provider", "reason" });

    public static readonly Counter UserRegistrationsTotal = Prometheus.Metrics
        .CreateCounter("user_registrations_total", "Total number of user registrations",
            new[] { "provider" });

    public static readonly Counter CourseEnrollmentsTotal = Prometheus.Metrics
        .CreateCounter("course_enrollments_total", "Total number of course enrollments",
            new[] { "course_id", "category" });

    public static readonly Counter LessonCompletionsTotal = Prometheus.Metrics
        .CreateCounter("lesson_completions_total", "Total number of lesson completions",
            new[] { "course_id", "lesson_id" });

    public static readonly Counter MessagesPublishedTotal = Prometheus.Metrics
        .CreateCounter("messages_published_total", "Total number of messages published",
            new[] { "event_type", "exchange" });

    public static readonly Counter MessagesConsumedTotal = Prometheus.Metrics
        .CreateCounter("messages_consumed_total", "Total number of messages consumed",
            new[] { "event_type", "queue", "success" });

    public static readonly Counter DatabaseQueriesTotal = Prometheus.Metrics
        .CreateCounter("database_queries_total", "Total number of database queries",
            new[] { "operation", "table", "success" });

    // Histograms
    public static readonly Histogram HttpRequestDuration = Prometheus.Metrics
        .CreateHistogram("http_request_duration_seconds", "HTTP request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status_code" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // 1ms to ~32s
            });

    public static readonly Histogram DatabaseQueryDuration = Prometheus.Metrics
        .CreateHistogram("database_query_duration_seconds", "Database query duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation", "table" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // 1ms to ~32s
            });

    public static readonly Histogram MessageProcessingDuration = Prometheus.Metrics
        .CreateHistogram("message_processing_duration_seconds", "Message processing duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "event_type", "handler" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // 1ms to ~32s
            });

    public static readonly Histogram HttpRequestSizeBytes = Prometheus.Metrics
        .CreateHistogram("http_request_size_bytes", "HTTP request size in bytes",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" },
                Buckets = Histogram.ExponentialBuckets(100, 10, 8) // 100B to ~100MB
            });

    public static readonly Histogram HttpResponseSizeBytes = Prometheus.Metrics
        .CreateHistogram("http_response_size_bytes", "HTTP response size in bytes",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status_code" },
                Buckets = Histogram.ExponentialBuckets(100, 10, 8) // 100B to ~100MB
            });

    // Gauges
    public static readonly Gauge ActiveConnections = Prometheus.Metrics
        .CreateGauge("active_connections", "Number of active connections");

    public static readonly Gauge ActiveUsers = Prometheus.Metrics
        .CreateGauge("active_users", "Number of active users");

    public static readonly Gauge MemoryUsageBytes = Prometheus.Metrics
        .CreateGauge("memory_usage_bytes", "Memory usage in bytes");

    public static readonly Gauge CpuUsagePercent = Prometheus.Metrics
        .CreateGauge("cpu_usage_percent", "CPU usage percentage");

    public static readonly Gauge ThreadPoolActiveThreads = Prometheus.Metrics
        .CreateGauge("threadpool_active_threads", "Number of active threads in thread pool");

    public static readonly Gauge ThreadPoolQueuedItems = Prometheus.Metrics
        .CreateGauge("threadpool_queued_items", "Number of queued items in thread pool");

    public static readonly Gauge GcCollectionsTotal = Prometheus.Metrics
        .CreateGauge("gc_collections_total", "Total number of garbage collections",
            new[] { "generation" });

    public static readonly Gauge GcMemoryBytes = Prometheus.Metrics
        .CreateGauge("gc_memory_bytes", "Memory allocated by garbage collector",
            new[] { "generation" });

    // Business-specific metrics
    public static readonly Gauge ActiveCourses = Prometheus.Metrics
        .CreateGauge("active_courses_total", "Number of active courses");

    public static readonly Gauge TotalUsers = Prometheus.Metrics
        .CreateGauge("total_users", "Total number of registered users");

    public static readonly Gauge OnlineUsers = Prometheus.Metrics
        .CreateGauge("online_users", "Number of users currently online");

    public static readonly Counter ReadingSessionsTotal = Prometheus.Metrics
        .CreateCounter("reading_sessions_total", "Total number of reading sessions started",
            new[] { "user_id", "course_id" });

    public static readonly Histogram ReadingSpeedWpm = Prometheus.Metrics
        .CreateHistogram("reading_speed_wpm", "Reading speed in words per minute",
            new HistogramConfiguration
            {
                LabelNames = new[] { "user_id", "course_id", "lesson_id" },
                Buckets = Histogram.LinearBuckets(50, 50, 20) // 50-1000 WPM
            });

    public static readonly Counter QuizAttemptsTotal = Prometheus.Metrics
        .CreateCounter("quiz_attempts_total", "Total number of quiz attempts",
            new[] { "quiz_id", "user_id" });

    public static readonly Counter QuizPassedTotal = Prometheus.Metrics
        .CreateCounter("quiz_passed_total", "Total number of passed quizzes",
            new[] { "quiz_id", "user_id" });

    public static readonly Histogram QuizScorePercent = Prometheus.Metrics
        .CreateHistogram("quiz_score_percent", "Quiz score percentage",
            new HistogramConfiguration
            {
                LabelNames = new[] { "quiz_id", "user_id" },
                Buckets = Histogram.LinearBuckets(0, 10, 11) // 0-100%
            });
}