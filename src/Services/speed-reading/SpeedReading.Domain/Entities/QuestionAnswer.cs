namespace SpeedReading.Domain.Entities;

public class QuestionAnswer
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid AttemptId { get; private set; }
    public ExerciseAttempt Attempt { get; private set; } = null!;

    public Guid QuestionId { get; private set; }
    public Question Question { get; private set; } = null!;

    public string UserAnswer { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int PointsEarned { get; private set; }
    public DateTime? AnsweredAt { get; private set; }
    public TimeSpan? TimeSpent { get; private set; }

    public string? Feedback { get; private set; }
    public string Metadata { get; private set; } = string.Empty;

    private QuestionAnswer() { }

    public QuestionAnswer(
        Guid attemptId,
        Guid questionId,
        string userAnswer)
    {
        Id = Guid.NewGuid();
        AttemptId = attemptId;
        QuestionId = questionId;
        UserAnswer = userAnswer ?? string.Empty;
        IsCorrect = false;
        PointsEarned = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(userAnswer))
        {
            AnsweredAt = DateTime.UtcNow;
        }
    }

    public void UpdateAnswer(string userAnswer)
    {
        var previousAnswerTime = AnsweredAt;
        var wasEmpty = string.IsNullOrEmpty(UserAnswer);
        
        UserAnswer = userAnswer ?? string.Empty;
        
        if (!string.IsNullOrEmpty(userAnswer))
        {
            if (!AnsweredAt.HasValue)
            {
                AnsweredAt = DateTime.UtcNow;
            }
            else if (wasEmpty)
            {
                // Reset the answered time if this was previously empty
                AnsweredAt = DateTime.UtcNow;
            }
        }
        else
        {
            // If answer is cleared, reset the answered time
            AnsweredAt = null;
            TimeSpent = null;
        }
    }

    public void SetCorrect(bool isCorrect, int pointsEarned)
    {
        IsCorrect = isCorrect;
        PointsEarned = pointsEarned;
    }

    public void SetTimeSpent(TimeSpan timeSpent)
    {
        TimeSpent = timeSpent;
    }

    public void AddFeedback(string feedback)
    {
        Feedback = feedback;
    }

    public void UpdateMetadata(string metadata)
    {
        Metadata = metadata ?? string.Empty;
    }

    public bool IsAnswered()
    {
        return !string.IsNullOrWhiteSpace(UserAnswer);
    }
}