using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Entities;

public class ExerciseAttempt
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid ExerciseId { get; private set; }
    public Exercise Exercise { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public AttemptStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public int TotalScore { get; private set; }
    public int MaxPossibleScore { get; private set; }
    public double ScorePercentage { get; private set; }
    public bool IsPassed { get; private set; }

    public TimeSpan? TimeSpent { get; private set; }
    public int QuestionsAnswered { get; private set; }
    public int TotalQuestions { get; private set; }

    public string? Notes { get; private set; }
    public string Metadata { get; private set; } = string.Empty;

    private readonly List<QuestionAnswer> _answers = new();
    public IReadOnlyList<QuestionAnswer> Answers => _answers.AsReadOnly();

    private ExerciseAttempt() { }

    public ExerciseAttempt(
        Guid exerciseId,
        Guid userId,
        IReadOnlyList<Question> questions,
        int? timeLimitInMinutes = null)
    {
        Id = Guid.NewGuid();
        ExerciseId = exerciseId;
        UserId = userId;
        Status = AttemptStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (timeLimitInMinutes.HasValue)
        {
            ExpiresAt = StartedAt.AddMinutes(timeLimitInMinutes.Value);
        }

        TotalQuestions = questions.Count;
        MaxPossibleScore = questions.Sum(q => q.Points);
        TotalScore = 0;
        ScorePercentage = 0;
        QuestionsAnswered = 0;
        IsPassed = false;

        // Initialize empty answers for all questions
        foreach (var question in questions)
        {
            _answers.Add(new QuestionAnswer(Id, question.Id, string.Empty));
        }
    }

    public void AnswerQuestion(Guid questionId, string answer)
    {
        if (Status != AttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot answer questions on completed attempt.");
        }

        if (IsExpired())
        {
            TimeOut();
            throw new InvalidOperationException("Attempt has expired.");
        }

        var questionAnswer = _answers.FirstOrDefault(a => a.QuestionId == questionId);
        if (questionAnswer == null)
        {
            throw new ArgumentException("Question not found in this attempt.", nameof(questionId));
        }

        var wasEmpty = string.IsNullOrEmpty(questionAnswer.UserAnswer);
        questionAnswer.UpdateAnswer(answer);
        
        if (wasEmpty && !string.IsNullOrEmpty(answer))
        {
            QuestionsAnswered++;
        }
        else if (!wasEmpty && string.IsNullOrEmpty(answer))
        {
            QuestionsAnswered--;
        }
    }

    public void Complete()
    {
        if (Status != AttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress attempts can be completed.");
        }

        Status = AttemptStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        TimeSpent = CompletedAt.Value - StartedAt;
        
        CalculateScore();
    }

    public void Abandon()
    {
        if (Status != AttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress attempts can be abandoned.");
        }

        Status = AttemptStatus.Abandoned;
        CompletedAt = DateTime.UtcNow;
        TimeSpent = CompletedAt.Value - StartedAt;
    }

    public void TimeOut()
    {
        if (Status != AttemptStatus.InProgress)
        {
            return;
        }

        Status = AttemptStatus.TimedOut;
        CompletedAt = DateTime.UtcNow;
        TimeSpent = CompletedAt.Value - StartedAt;
        
        CalculateScore();
    }

    public void AddNotes(string notes)
    {
        Notes = notes;
    }

    public void UpdateMetadata(string metadata)
    {
        Metadata = metadata ?? string.Empty;
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    public double GetCompletionPercentage()
    {
        return TotalQuestions > 0 ? (double)QuestionsAnswered / TotalQuestions * 100 : 0;
    }

    public TimeSpan? GetRemainingTime()
    {
        if (!ExpiresAt.HasValue || Status != AttemptStatus.InProgress)
        {
            return null;
        }

        var remaining = ExpiresAt.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void CalculateScore()
    {
        // This will be calculated with the Exercise and Question entities
        // For now, we'll set it to 0 and update it in the domain service
        TotalScore = 0;
        ScorePercentage = MaxPossibleScore > 0 ? (double)TotalScore / MaxPossibleScore * 100 : 0;
        IsPassed = false; // Will be determined by exercise passing score
    }

    public void UpdateScore(int totalScore, int passingScore)
    {
        TotalScore = totalScore;
        ScorePercentage = MaxPossibleScore > 0 ? (double)totalScore / MaxPossibleScore * 100 : 0;
        IsPassed = totalScore >= passingScore;
    }
}