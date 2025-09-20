using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.Repositories;
using SpeedReading.Domain.Services;

namespace SpeedReading.Application.Services;

public class ExerciseAttemptTrackingService
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IExerciseAttemptRepository _attemptRepository;
    private readonly IExerciseScoreCalculator _scoreCalculator;

    public ExerciseAttemptTrackingService(
        IExerciseRepository exerciseRepository,
        IExerciseAttemptRepository attemptRepository,
        IExerciseScoreCalculator scoreCalculator)
    {
        _exerciseRepository = exerciseRepository;
        _attemptRepository = attemptRepository;
        _scoreCalculator = scoreCalculator;
    }

    public async Task<AttemptTrackingResult> StartAttemptAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var exercise = await _exerciseRepository.GetByIdWithQuestionsAsync(exerciseId, cancellationToken);
        if (exercise == null)
        {
            return AttemptTrackingResult.Failed("Exercise not found");
        }

        // Check if user can attempt this exercise
        var canAttempt = await CanUserAttemptExerciseAsync(exerciseId, userId, cancellationToken);
        if (!canAttempt.CanAttempt)
        {
            return AttemptTrackingResult.Failed(canAttempt.Reason);
        }

        try
        {
            var attempt = exercise.StartAttempt(userId);
            await _attemptRepository.AddAsync(attempt, cancellationToken);

            return AttemptTrackingResult.Success(new AttemptInfo
            {
                AttemptId = attempt.Id,
                ExerciseId = exerciseId,
                UserId = userId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                ExpiresAt = attempt.ExpiresAt,
                TotalQuestions = attempt.TotalQuestions,
                QuestionsAnswered = attempt.QuestionsAnswered,
                RemainingTime = attempt.GetRemainingTime()
            });
        }
        catch (Exception ex)
        {
            return AttemptTrackingResult.Failed($"Failed to start attempt: {ex.Message}");
        }
    }

    public async Task<AttemptTrackingResult> SubmitAnswerAsync(
        Guid attemptId, 
        Guid questionId, 
        string answer, 
        CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdWithAnswersAsync(attemptId, cancellationToken);
        if (attempt == null)
        {
            return AttemptTrackingResult.Failed("Attempt not found");
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            return AttemptTrackingResult.Failed("Attempt is not in progress");
        }

        if (attempt.IsExpired())
        {
            await TimeOutAttemptAsync(attemptId, cancellationToken);
            return AttemptTrackingResult.Failed("Attempt has expired");
        }

        try
        {
            attempt.AnswerQuestion(questionId, answer);
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            return AttemptTrackingResult.Success(new AttemptInfo
            {
                AttemptId = attempt.Id,
                ExerciseId = attempt.ExerciseId,
                UserId = attempt.UserId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                ExpiresAt = attempt.ExpiresAt,
                TotalQuestions = attempt.TotalQuestions,
                QuestionsAnswered = attempt.QuestionsAnswered,
                CompletionPercentage = attempt.GetCompletionPercentage(),
                RemainingTime = attempt.GetRemainingTime()
            });
        }
        catch (Exception ex)
        {
            return AttemptTrackingResult.Failed($"Failed to submit answer: {ex.Message}");
        }
    }

    public async Task<AttemptTrackingResult> CompleteAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdWithAnswersAsync(attemptId, cancellationToken);
        if (attempt == null)
        {
            return AttemptTrackingResult.Failed("Attempt not found");
        }

        var exercise = await _exerciseRepository.GetByIdWithQuestionsAsync(attempt.ExerciseId, cancellationToken);
        if (exercise == null)
        {
            return AttemptTrackingResult.Failed("Exercise not found");
        }

        try
        {
            attempt.Complete();
            
            // Calculate score
            var scoreResult = await _scoreCalculator.CalculateScoreAsync(attempt, exercise.Questions);
            attempt.UpdateScore(scoreResult.TotalScore, exercise.PassingScore);
            
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            return AttemptTrackingResult.Success(new AttemptInfo
            {
                AttemptId = attempt.Id,
                ExerciseId = attempt.ExerciseId,
                UserId = attempt.UserId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                TimeSpent = attempt.TimeSpent,
                TotalScore = attempt.TotalScore,
                ScorePercentage = attempt.ScorePercentage,
                IsPassed = attempt.IsPassed,
                TotalQuestions = attempt.TotalQuestions,
                QuestionsAnswered = attempt.QuestionsAnswered,
                CompletionPercentage = attempt.GetCompletionPercentage()
            });
        }
        catch (Exception ex)
        {
            return AttemptTrackingResult.Failed($"Failed to complete attempt: {ex.Message}");
        }
    }

    public async Task<AttemptTrackingResult> AbandonAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null)
        {
            return AttemptTrackingResult.Failed("Attempt not found");
        }

        try
        {
            attempt.Abandon();
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            return AttemptTrackingResult.Success(new AttemptInfo
            {
                AttemptId = attempt.Id,
                ExerciseId = attempt.ExerciseId,
                UserId = attempt.UserId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                TimeSpent = attempt.TimeSpent
            });
        }
        catch (Exception ex)
        {
            return AttemptTrackingResult.Failed($"Failed to abandon attempt: {ex.Message}");
        }
    }

    public async Task<AttemptTrackingResult> TimeOutAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdWithAnswersAsync(attemptId, cancellationToken);
        if (attempt == null)
        {
            return AttemptTrackingResult.Failed("Attempt not found");
        }

        var exercise = await _exerciseRepository.GetByIdWithQuestionsAsync(attempt.ExerciseId, cancellationToken);
        if (exercise == null)
        {
            return AttemptTrackingResult.Failed("Exercise not found");
        }

        try
        {
            attempt.TimeOut();
            
            // Calculate partial score for answered questions
            var scoreResult = await _scoreCalculator.CalculateScoreAsync(attempt, exercise.Questions);
            attempt.UpdateScore(scoreResult.TotalScore, exercise.PassingScore);
            
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);

            return AttemptTrackingResult.Success(new AttemptInfo
            {
                AttemptId = attempt.Id,
                ExerciseId = attempt.ExerciseId,
                UserId = attempt.UserId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                TimeSpent = attempt.TimeSpent,
                TotalScore = attempt.TotalScore,
                ScorePercentage = attempt.ScorePercentage,
                IsPassed = attempt.IsPassed
            });
        }
        catch (Exception ex)
        {
            return AttemptTrackingResult.Failed($"Failed to timeout attempt: {ex.Message}");
        }
    }

    public async Task<AttemptInfo?> GetCurrentAttemptAsync(Guid exerciseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetActiveAttemptAsync(userId, exerciseId, cancellationToken);
        if (attempt == null) return null;

        return new AttemptInfo
        {
            AttemptId = attempt.Id,
            ExerciseId = attempt.ExerciseId,
            UserId = attempt.UserId,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            ExpiresAt = attempt.ExpiresAt,
            TotalQuestions = attempt.TotalQuestions,
            QuestionsAnswered = attempt.QuestionsAnswered,
            CompletionPercentage = attempt.GetCompletionPercentage(),
            RemainingTime = attempt.GetRemainingTime()
        };
    }

    public async Task<IReadOnlyList<AttemptInfo>> GetUserAttemptsAsync(
        Guid userId, 
        Guid? exerciseId = null,
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default)
    {
        var attempts = exerciseId.HasValue
            ? await _attemptRepository.GetByUserAndExerciseAsync(userId, exerciseId.Value, cancellationToken)
            : await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);

        return attempts.Skip(skip).Take(take).Select(a => new AttemptInfo
        {
            AttemptId = a.Id,
            ExerciseId = a.ExerciseId,
            UserId = a.UserId,
            Status = a.Status,
            StartedAt = a.StartedAt,
            CompletedAt = a.CompletedAt,
            TimeSpent = a.TimeSpent,
            TotalScore = a.TotalScore,
            ScorePercentage = a.ScorePercentage,
            IsPassed = a.IsPassed,
            TotalQuestions = a.TotalQuestions,
            QuestionsAnswered = a.QuestionsAnswered
        }).ToArray();
    }

    public async Task<AttemptStatistics> GetAttemptStatisticsAsync(
        Guid userId, 
        Guid? exerciseId = null,
        CancellationToken cancellationToken = default)
    {
        var attempts = exerciseId.HasValue
            ? await _attemptRepository.GetByUserAndExerciseAsync(userId, exerciseId.Value, cancellationToken)
            : await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);

        var completedAttempts = attempts.Where(a => a.Status == AttemptStatus.Completed).ToArray();
        var passedAttempts = completedAttempts.Where(a => a.IsPassed).ToArray();

        return new AttemptStatistics
        {
            UserId = userId,
            ExerciseId = exerciseId,
            TotalAttempts = attempts.Count,
            CompletedAttempts = completedAttempts.Length,
            PassedAttempts = passedAttempts.Length,
            PassRate = completedAttempts.Length > 0 ? (double)passedAttempts.Length / completedAttempts.Length * 100 : 0,
            AverageScore = completedAttempts.Length > 0 ? completedAttempts.Average(a => a.ScorePercentage) : 0,
            BestScore = completedAttempts.Any() ? completedAttempts.Max(a => a.ScorePercentage) : 0,
            AverageTimeSpent = completedAttempts.Length > 0 
                ? TimeSpan.FromTicks((long)completedAttempts.Where(a => a.TimeSpent.HasValue).Average(a => a.TimeSpent!.Value.Ticks))
                : TimeSpan.Zero,
            FirstAttemptDate = attempts.Any() ? attempts.Min(a => a.StartedAt) : null,
            LastAttemptDate = attempts.Any() ? attempts.Max(a => a.StartedAt) : null,
            ImprovementTrend = CalculateImprovementTrend(completedAttempts.OrderBy(a => a.StartedAt).ToArray())
        };
    }

    public async Task<int> ProcessExpiredAttemptsAsync(CancellationToken cancellationToken = default)
    {
        return await _attemptRepository.TimeOutExpiredAttemptsAsync(cancellationToken);
    }

    public async Task<CanAttemptResult> CanUserAttemptExerciseAsync(
        Guid exerciseId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(exerciseId, cancellationToken);
        if (exercise == null)
        {
            return new CanAttemptResult { CanAttempt = false, Reason = "Exercise not found" };
        }

        if (exercise.Status != ExerciseStatus.Active)
        {
            return new CanAttemptResult { CanAttempt = false, Reason = "Exercise is not active" };
        }

        var hasActiveAttempt = await _attemptRepository.HasActiveAttemptAsync(userId, exerciseId, cancellationToken);
        if (hasActiveAttempt)
        {
            return new CanAttemptResult { CanAttempt = false, Reason = "User already has an active attempt" };
        }

        var userAttempts = await _attemptRepository.GetByUserAndExerciseAsync(userId, exerciseId, cancellationToken);
        var completedAttempts = userAttempts.Count(a => a.Status == AttemptStatus.Completed);

        if (!exercise.AllowRetry && completedAttempts > 0)
        {
            return new CanAttemptResult { CanAttempt = false, Reason = "Retries are not allowed for this exercise" };
        }

        if (completedAttempts >= exercise.MaxRetries)
        {
            return new CanAttemptResult { CanAttempt = false, Reason = $"Maximum retry limit ({exercise.MaxRetries}) reached" };
        }

        var remainingAttempts = exercise.MaxRetries - completedAttempts;
        
        return new CanAttemptResult 
        { 
            CanAttempt = true, 
            RemainingAttempts = remainingAttempts 
        };
    }

    private double CalculateImprovementTrend(ExerciseAttempt[] orderedAttempts)
    {
        if (orderedAttempts.Length < 2) return 0;

        var firstHalf = orderedAttempts.Take(orderedAttempts.Length / 2).Average(a => a.ScorePercentage);
        var secondHalf = orderedAttempts.Skip(orderedAttempts.Length / 2).Average(a => a.ScorePercentage);

        return secondHalf - firstHalf;
    }
}

// Supporting classes
public class AttemptTrackingResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public AttemptInfo? AttemptInfo { get; set; }

    public static AttemptTrackingResult Success(AttemptInfo attemptInfo)
    {
        return new AttemptTrackingResult
        {
            IsSuccess = true,
            AttemptInfo = attemptInfo
        };
    }

    public static AttemptTrackingResult Failed(string errorMessage)
    {
        return new AttemptTrackingResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

public class AttemptInfo
{
    public Guid AttemptId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid UserId { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public TimeSpan? TimeSpent { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    
    public int TotalScore { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    
    public int TotalQuestions { get; set; }
    public int QuestionsAnswered { get; set; }
    public double CompletionPercentage { get; set; }
}

public class AttemptStatistics
{
    public Guid UserId { get; set; }
    public Guid? ExerciseId { get; set; }
    
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public int PassedAttempts { get; set; }
    public double PassRate { get; set; }
    
    public double AverageScore { get; set; }
    public double BestScore { get; set; }
    public TimeSpan AverageTimeSpent { get; set; }
    
    public DateTime? FirstAttemptDate { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    
    public double ImprovementTrend { get; set; } // Positive indicates improvement
}

public class CanAttemptResult
{
    public bool CanAttempt { get; set; }
    public string? Reason { get; set; }
    public int RemainingAttempts { get; set; }
    public TimeSpan? CooldownPeriod { get; set; }
}