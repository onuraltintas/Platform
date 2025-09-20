using SpeedReading.Domain.Enums;

namespace SpeedReading.Domain.Entities;

public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public ExerciseType Type { get; private set; }
    public ExerciseStatus Status { get; private set; }
    public EducationCategory TargetEducationLevel { get; private set; }
    public int? MinGradeLevel { get; private set; }
    public int? MaxGradeLevel { get; private set; }
    public TextDifficulty DifficultyLevel { get; private set; }
    
    public Guid? ReadingTextId { get; private set; }
    public ReadingText? ReadingText { get; private set; }
    
    public int TimeLimit { get; private set; } // Dakika cinsinden
    public int MaxScore { get; private set; }
    public int PassingScore { get; private set; }
    public bool IsTimeLimited { get; private set; }
    public bool IsRandomized { get; private set; }
    public bool ShowResults { get; private set; }
    public bool AllowRetry { get; private set; }
    public int MaxRetries { get; private set; }
    
    public string Tags { get; private set; } = string.Empty;
    public string Metadata { get; private set; } = string.Empty;
    
    public DateTime? PublishedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public bool IsActive { get; private set; }
    
    private readonly List<Question> _questions = new List<Question>();
    public IReadOnlyList<Question> Questions => _questions.AsReadOnly();
    
    private readonly List<ExerciseAttempt> _attempts = new List<ExerciseAttempt>();
    public IReadOnlyList<ExerciseAttempt> Attempts => _attempts.AsReadOnly();

    private Exercise() { }

    public Exercise(
        string title,
        string description,
        string instructions,
        ExerciseType type,
        EducationCategory targetEducationLevel,
        TextDifficulty difficultyLevel,
        Guid createdBy,
        Guid? readingTextId = null)
    {
        Id = Guid.NewGuid();
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        Type = type;
        TargetEducationLevel = targetEducationLevel;
        DifficultyLevel = difficultyLevel;
        CreatedBy = createdBy;
        ReadingTextId = readingTextId;
        
        SetGradeLevelRange(targetEducationLevel);
        
        Status = ExerciseStatus.Draft;
        TimeLimit = 30; // Varsayılan 30 dakika
        MaxScore = 100;
        PassingScore = 60;
        IsTimeLimited = true;
        IsRandomized = false;
        ShowResults = true;
        AllowRetry = true;
        MaxRetries = 3;
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;

        // AddDomainEvent(new ExerciseCreatedEvent(Id, Title, Type));
    }

    public void UpdateBasicInfo(string title, string description, string instructions)
    {
        if (Status == ExerciseStatus.Active && _attempts.Any(a => a.Status == AttemptStatus.InProgress))
        {
            throw new InvalidOperationException("Active exercise with ongoing attempts cannot be modified.");
        }

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        UpdatedAt = DateTime.UtcNow;

        // AddDomainEvent(new ExerciseUpdatedEvent(Id, Title));
    }

    public void UpdateSettings(
        int timeLimit,
        int maxScore,
        int passingScore,
        bool isTimeLimited,
        bool isRandomized,
        bool showResults,
        bool allowRetry,
        int maxRetries)
    {
        if (timeLimit <= 0) throw new ArgumentException("Time limit must be positive.", nameof(timeLimit));
        if (maxScore <= 0) throw new ArgumentException("Max score must be positive.", nameof(maxScore));
        if (passingScore < 0 || passingScore > maxScore) 
            throw new ArgumentException("Passing score must be between 0 and max score.", nameof(passingScore));
        if (maxRetries < 0) throw new ArgumentException("Max retries cannot be negative.", nameof(maxRetries));

        TimeLimit = timeLimit;
        MaxScore = maxScore;
        PassingScore = passingScore;
        IsTimeLimited = isTimeLimited;
        IsRandomized = isRandomized;
        ShowResults = showResults;
        AllowRetry = allowRetry;
        MaxRetries = maxRetries;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddQuestion(Question question)
    {
        if (question == null) throw new ArgumentNullException(nameof(question));
        if (Status == ExerciseStatus.Active && _attempts.Any(a => a.Status == AttemptStatus.InProgress))
        {
            throw new InvalidOperationException("Cannot add questions to active exercise with ongoing attempts.");
        }

        _questions.Add(question);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveQuestion(Guid questionId)
    {
        if (Status == ExerciseStatus.Active && _attempts.Any(a => a.Status == AttemptStatus.InProgress))
        {
            throw new InvalidOperationException("Cannot remove questions from active exercise with ongoing attempts.");
        }

        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            _questions.Remove(question);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Publish()
    {
        if (Status == ExerciseStatus.Active)
        {
            throw new InvalidOperationException("Exercise is already published.");
        }

        if (!_questions.Any())
        {
            throw new InvalidOperationException("Cannot publish exercise without questions.");
        }

        Status = ExerciseStatus.Active;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // AddDomainEvent(new ExercisePublishedEvent(Id, Title));
    }

    public void Deactivate()
    {
        Status = ExerciseStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ExerciseStatus.Archived;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public ExerciseAttempt StartAttempt(Guid userId)
    {
        if (Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException("Only active exercises can be attempted.");
        }

        var existingAttempts = _attempts.Where(a => a.UserId == userId).ToList();
        
        if (existingAttempts.Any(a => a.Status == AttemptStatus.InProgress))
        {
            throw new InvalidOperationException("User already has an ongoing attempt for this exercise.");
        }

        if (!AllowRetry && existingAttempts.Any(a => a.Status == AttemptStatus.Completed))
        {
            throw new InvalidOperationException("Retries are not allowed for this exercise.");
        }

        var completedAttempts = existingAttempts.Count(a => a.Status == AttemptStatus.Completed);
        if (completedAttempts >= MaxRetries)
        {
            throw new InvalidOperationException($"Maximum retry limit ({MaxRetries}) reached.");
        }

        var questions = IsRandomized ? _questions.OrderBy(x => Guid.NewGuid()).ToList() : _questions.ToList();
        var attempt = new ExerciseAttempt(Id, userId, questions, IsTimeLimited ? TimeLimit : null);
        
        _attempts.Add(attempt);
        // AddDomainEvent(new ExerciseAttemptStartedEvent(Id, attempt.Id, userId));
        
        return attempt;
    }

    public void UpdateTags(string tags)
    {
        Tags = tags ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(string metadata)
    {
        Metadata = metadata ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetGradeLevelRange(EducationCategory category)
    {
        switch (category)
        {
            case EducationCategory.Elementary:
                MinGradeLevel = 1;
                MaxGradeLevel = 4;
                break;
            case EducationCategory.MiddleSchool:
                MinGradeLevel = 5;
                MaxGradeLevel = 8;
                break;
            case EducationCategory.HighSchool:
                MinGradeLevel = 9;
                MaxGradeLevel = 12;
                break;
            case EducationCategory.University:
                MinGradeLevel = 13;
                MaxGradeLevel = 16;
                break;
            case EducationCategory.Graduate:
                MinGradeLevel = 17;
                MaxGradeLevel = 20;
                break;
            case EducationCategory.Adult:
                MinGradeLevel = 13;
                MaxGradeLevel = null;
                break;
            default:
                MinGradeLevel = 1;
                MaxGradeLevel = null;
                break;
        }
    }
}

// Domain Events
public record ExerciseCreatedEvent(Guid ExerciseId, string Title, ExerciseType Type);
public record ExerciseUpdatedEvent(Guid ExerciseId, string Title);
public record ExercisePublishedEvent(Guid ExerciseId, string Title);
public record ExerciseAttemptStartedEvent(Guid ExerciseId, Guid AttemptId, Guid UserId);