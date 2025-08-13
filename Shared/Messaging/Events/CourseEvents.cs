namespace EgitimPlatform.Shared.Messaging.Events;

public class CourseCreatedEvent : IntegrationEvent
{
    public CourseCreatedEvent(string courseId, string title, string authorId, string category)
    {
        CourseId = courseId;
        Title = title;
        AuthorId = authorId;
        Category = category;
    }

    public string CourseId { get; private set; }
    public string Title { get; private set; }
    public string AuthorId { get; private set; }
    public string Category { get; private set; }
}

public class CoursePublishedEvent : IntegrationEvent
{
    public CoursePublishedEvent(string courseId, string title, string authorId)
    {
        CourseId = courseId;
        Title = title;
        AuthorId = authorId;
    }

    public string CourseId { get; private set; }
    public string Title { get; private set; }
    public string AuthorId { get; private set; }
}

public class CourseUpdatedEvent : IntegrationEvent
{
    public CourseUpdatedEvent(string courseId, string title, string authorId)
    {
        CourseId = courseId;
        Title = title;
        AuthorId = authorId;
    }

    public string CourseId { get; private set; }
    public string Title { get; private set; }
    public string AuthorId { get; private set; }
}

public class UserEnrolledInCourseEvent : IntegrationEvent
{
    public UserEnrolledInCourseEvent(string userId, string courseId, string courseTitle, decimal price)
    {
        UserId = userId;
        CourseId = courseId;
        CourseTitle = courseTitle;
        Price = price;
    }

    public string UserId { get; private set; }
    public string CourseId { get; private set; }
    public string CourseTitle { get; private set; }
    public decimal Price { get; private set; }
}

public class UserCompletedCourseEvent : IntegrationEvent
{
    public UserCompletedCourseEvent(string userId, string courseId, string courseTitle, int score)
    {
        UserId = userId;
        CourseId = courseId;
        CourseTitle = courseTitle;
        Score = score;
    }

    public string UserId { get; private set; }
    public string CourseId { get; private set; }
    public string CourseTitle { get; private set; }
    public int Score { get; private set; }
}

public class UserProgressUpdatedEvent : IntegrationEvent
{
    public UserProgressUpdatedEvent(
        string userId, 
        string courseId, 
        string lessonId, 
        int progressPercentage, 
        int timeSpentMinutes)
    {
        UserId = userId;
        CourseId = courseId;
        LessonId = lessonId;
        ProgressPercentage = progressPercentage;
        TimeSpentMinutes = timeSpentMinutes;
    }

    public string UserId { get; private set; }
    public string CourseId { get; private set; }
    public string LessonId { get; private set; }
    public int ProgressPercentage { get; private set; }
    public int TimeSpentMinutes { get; private set; }
}

public class AchievementUnlockedEvent : IntegrationEvent
{
    public AchievementUnlockedEvent(
        string userId, 
        string achievementId, 
        string achievementTitle, 
        int points)
    {
        UserId = userId;
        AchievementId = achievementId;
        AchievementTitle = achievementTitle;
        Points = points;
    }

    public string UserId { get; private set; }
    public string AchievementId { get; private set; }
    public string AchievementTitle { get; private set; }
    public int Points { get; private set; }
}