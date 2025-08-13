using System.Diagnostics;

namespace EgitimPlatform.Shared.Observability.Tracing;

public static class ActivityExtensions
{
    // Standard OpenTelemetry semantic conventions
    public const string HttpMethodTag = "http.method";
    public const string HttpUrlTag = "http.url";
    public const string HttpStatusCodeTag = "http.status_code";
    public const string HttpUserAgentTag = "http.user_agent";
    public const string HttpRouteTag = "http.route";
    public const string HttpSchemeTag = "http.scheme";
    public const string HttpHostTag = "http.host";
    public const string HttpTargetTag = "http.target";

    public const string DbSystemTag = "db.system";
    public const string DbConnectionStringTag = "db.connection_string";
    public const string DbUserTag = "db.user";
    public const string DbNameTag = "db.name";
    public const string DbStatementTag = "db.statement";
    public const string DbOperationTag = "db.operation";
    public const string DbTableTag = "db.sql.table";

    public const string MessageSystemTag = "messaging.system";
    public const string MessageDestinationTag = "messaging.destination";
    public const string MessageDestinationKindTag = "messaging.destination_kind";
    public const string MessageOperationTag = "messaging.operation";
    public const string MessageMessageIdTag = "messaging.message_id";
    public const string MessageConversationIdTag = "messaging.conversation_id";

    public const string UserIdTag = "user.id";
    public const string UserEmailTag = "user.email";
    public const string SessionIdTag = "session.id";
    public const string TenantIdTag = "tenant.id";

    // Custom business tags
    public const string CourseIdTag = "course.id";
    public const string LessonIdTag = "lesson.id";
    public const string QuizIdTag = "quiz.id";
    public const string AchievementIdTag = "achievement.id";
    public const string ReadingSpeedTag = "reading.speed_wpm";
    public const string ReadingTimeTag = "reading.time_seconds";

    public static Activity? SetHttpTags(this Activity? activity, string method, string url, string? route = null)
    {
        if (activity == null) return activity;

        activity.SetTag(HttpMethodTag, method);
        activity.SetTag(HttpUrlTag, url);
        
        if (!string.IsNullOrEmpty(route))
        {
            activity.SetTag(HttpRouteTag, route);
        }

        return activity;
    }

    public static Activity? SetHttpResult(this Activity? activity, int statusCode, string? userAgent = null)
    {
        if (activity == null) return activity;

        activity.SetTag(HttpStatusCodeTag, statusCode);
        
        if (!string.IsNullOrEmpty(userAgent))
        {
            activity.SetTag(HttpUserAgentTag, userAgent);
        }

        // Set status based on HTTP status code
        if (statusCode >= 400)
        {
            activity.SetStatus(ActivityStatusCode.Error, $"HTTP {statusCode}");
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        return activity;
    }

    public static Activity? SetDatabaseTags(this Activity? activity, string system, string operation, string? table = null, string? database = null)
    {
        if (activity == null) return activity;

        activity.SetTag(DbSystemTag, system);
        activity.SetTag(DbOperationTag, operation);
        
        if (!string.IsNullOrEmpty(table))
        {
            activity.SetTag(DbTableTag, table);
        }
        
        if (!string.IsNullOrEmpty(database))
        {
            activity.SetTag(DbNameTag, database);
        }

        return activity;
    }

    public static Activity? SetDatabaseStatement(this Activity? activity, string statement, bool includeStatement = false)
    {
        if (activity == null) return activity;

        // Only include statement if explicitly allowed (security consideration)
        if (includeStatement)
        {
            activity.SetTag(DbStatementTag, statement);
        }

        return activity;
    }

    public static Activity? SetMessagingTags(this Activity? activity, string system, string destination, string operation, string? messageId = null)
    {
        if (activity == null) return activity;

        activity.SetTag(MessageSystemTag, system);
        activity.SetTag(MessageDestinationTag, destination);
        activity.SetTag(MessageOperationTag, operation);
        
        if (!string.IsNullOrEmpty(messageId))
        {
            activity.SetTag(MessageMessageIdTag, messageId);
        }

        return activity;
    }

    public static Activity? SetUserTags(this Activity? activity, string userId, string? email = null, string? sessionId = null)
    {
        if (activity == null) return activity;

        activity.SetTag(UserIdTag, userId);
        
        if (!string.IsNullOrEmpty(email))
        {
            activity.SetTag(UserEmailTag, email);
        }
        
        if (!string.IsNullOrEmpty(sessionId))
        {
            activity.SetTag(SessionIdTag, sessionId);
        }

        return activity;
    }

    public static Activity? SetBusinessTags(this Activity? activity, string? courseId = null, string? lessonId = null, string? quizId = null)
    {
        if (activity == null) return activity;

        if (!string.IsNullOrEmpty(courseId))
        {
            activity.SetTag(CourseIdTag, courseId);
        }
        
        if (!string.IsNullOrEmpty(lessonId))
        {
            activity.SetTag(LessonIdTag, lessonId);
        }
        
        if (!string.IsNullOrEmpty(quizId))
        {
            activity.SetTag(QuizIdTag, quizId);
        }

        return activity;
    }

    public static Activity? SetReadingMetrics(this Activity? activity, int speedWpm, double timeSeconds)
    {
        if (activity == null) return activity;

        activity.SetTag(ReadingSpeedTag, speedWpm);
        activity.SetTag(ReadingTimeTag, timeSeconds);

        return activity;
    }

    public static Activity? AddEvent(this Activity? activity, string name, DateTimeOffset? timestamp = null, ActivityTagsCollection? tags = null)
    {
        if (activity == null) return activity;

        var eventTimestamp = timestamp ?? DateTimeOffset.UtcNow;
        var activityEvent = new ActivityEvent(name, eventTimestamp, tags ?? new ActivityTagsCollection());
        activity.AddEvent(activityEvent);

        return activity;
    }

    public static Activity? AddException(this Activity? activity, Exception exception, bool setErrorStatus = true)
    {
        if (activity == null) return activity;

        var tags = new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.StackTrace
        };

        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));

        if (setErrorStatus)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        return activity;
    }

    public static Activity? SetCustomTag(this Activity? activity, string key, object? value)
    {
        if (activity == null || string.IsNullOrEmpty(key)) return activity;

        activity.SetTag(key, value?.ToString());
        return activity;
    }

    public static Activity? SetCustomTags(this Activity? activity, Dictionary<string, object?> tags)
    {
        if (activity == null || tags == null) return activity;

        foreach (var tag in tags)
        {
            activity.SetTag(tag.Key, tag.Value?.ToString());
        }

        return activity;
    }

    public static string? GetTraceId(this Activity? activity)
    {
        return activity?.TraceId.ToString();
    }

    public static string? GetSpanId(this Activity? activity)
    {
        return activity?.SpanId.ToString();
    }

    public static bool IsRecording(this Activity? activity)
    {
        return activity?.IsAllDataRequested == true;
    }
}