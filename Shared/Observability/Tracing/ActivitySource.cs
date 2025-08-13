using System.Diagnostics;

namespace EgitimPlatform.Shared.Observability.Tracing;

public static class ApplicationActivitySource
{
    public const string SourceName = "EgitimPlatform";
    public const string Version = "1.0.0";

    private static readonly ActivitySource _activitySource = new(SourceName, Version);

    public static ActivitySource Instance => _activitySource;

    // HTTP activities
    public static Activity? StartHttpActivity(string name, string method, string url, string? route = null)
    {
        var activity = _activitySource.StartActivity($"HTTP {method} {route ?? url}", ActivityKind.Server);
        return activity?.SetHttpTags(method, url, route);
    }

    public static Activity? StartHttpClientActivity(string name, string method, string url)
    {
        var activity = _activitySource.StartActivity($"HTTP {method} {url}", ActivityKind.Client);
        return activity?.SetHttpTags(method, url);
    }

    // Database activities
    public static Activity? StartDatabaseActivity(string operation, string? table = null, string? database = null)
    {
        var activityName = table != null ? $"DB {operation} {table}" : $"DB {operation}";
        var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);
        return activity?.SetDatabaseTags("sqlserver", operation, table, database);
    }

    // Messaging activities
    public static Activity? StartMessagePublishActivity(string destination, string messageType)
    {
        var activity = _activitySource.StartActivity($"PUBLISH {destination}", ActivityKind.Producer);
        return activity?.SetMessagingTags("rabbitmq", destination, "publish")
                       ?.SetCustomTag("message.type", messageType);
    }

    public static Activity? StartMessageConsumeActivity(string destination, string messageType, string? messageId = null)
    {
        var activity = _activitySource.StartActivity($"CONSUME {destination}", ActivityKind.Consumer);
        return activity?.SetMessagingTags("rabbitmq", destination, "receive", messageId)
                       ?.SetCustomTag("message.type", messageType);
    }

    // Business domain activities
    public static Activity? StartUserActivity(string operation, string userId, string? email = null)
    {
        var activity = _activitySource.StartActivity($"USER {operation}", ActivityKind.Internal);
        return activity?.SetUserTags(userId, email);
    }

    public static Activity? StartCourseActivity(string operation, string courseId, string? userId = null)
    {
        var activity = _activitySource.StartActivity($"COURSE {operation}", ActivityKind.Internal);
        activity?.SetBusinessTags(courseId: courseId);
        
        if (!string.IsNullOrEmpty(userId))
        {
            activity?.SetUserTags(userId);
        }
        
        return activity;
    }

    public static Activity? StartLessonActivity(string operation, string courseId, string lessonId, string? userId = null)
    {
        var activity = _activitySource.StartActivity($"LESSON {operation}", ActivityKind.Internal);
        activity?.SetBusinessTags(courseId, lessonId);
        
        if (!string.IsNullOrEmpty(userId))
        {
            activity?.SetUserTags(userId);
        }
        
        return activity;
    }

    public static Activity? StartQuizActivity(string operation, string quizId, string? userId = null)
    {
        var activity = _activitySource.StartActivity($"QUIZ {operation}", ActivityKind.Internal);
        activity?.SetBusinessTags(quizId: quizId);
        
        if (!string.IsNullOrEmpty(userId))
        {
            activity?.SetUserTags(userId);
        }
        
        return activity;
    }

    public static Activity? StartReadingActivity(string operation, string courseId, string lessonId, string userId)
    {
        var activity = _activitySource.StartActivity($"READING {operation}", ActivityKind.Internal);
        return activity?.SetBusinessTags(courseId, lessonId)
                       ?.SetUserTags(userId);
    }

    public static Activity? StartAuthenticationActivity(string operation, string? provider = null)
    {
        var activity = _activitySource.StartActivity($"AUTH {operation}", ActivityKind.Internal);
        
        if (!string.IsNullOrEmpty(provider))
        {
            activity?.SetCustomTag("auth.provider", provider);
        }
        
        return activity;
    }

    // Generic activity starter
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, Activity? parent = null, ActivityContext context = default)
    {
        if (parent != null)
        {
            return _activitySource.StartActivity(name, kind, parent.Context);
        }
        
        if (context != default)
        {
            return _activitySource.StartActivity(name, kind, context);
        }
        
        return _activitySource.StartActivity(name, kind);
    }

    public static void Dispose()
    {
        _activitySource?.Dispose();
    }
}