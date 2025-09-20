using System.ComponentModel.DataAnnotations;
using SpeedReading.Domain.Enums;

namespace SpeedReading.API.Models;

/// <summary>
/// Request model for starting a new exercise attempt
/// </summary>
public class StartExerciseAttemptRequest
{
    /// <summary>
    /// Exercise ID to attempt
    /// </summary>
    [Required]
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// User ID attempting the exercise
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
}

/// <summary>
/// Request model for answering a question
/// </summary>
public class AnswerQuestionRequest
{
    /// <summary>
    /// Question ID being answered
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// User's answer (option ID for multiple choice, text for open-ended)
    /// </summary>
    [Required]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Time spent on this question in seconds
    /// </summary>
    [Range(0, 3600, ErrorMessage = "Time spent must be between 0 and 3600 seconds")]
    public int? TimeSpentSeconds { get; set; }
}

/// <summary>
/// Request model for completing an exercise attempt
/// </summary>
public class CompleteExerciseAttemptRequest
{
    /// <summary>
    /// Final answers for all questions
    /// </summary>
    public List<QuestionAnswerDto> Answers { get; set; } = new();

    /// <summary>
    /// Optional notes from the user
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Question answer DTO for submission
/// </summary>
public class QuestionAnswerDto
{
    /// <summary>
    /// Question ID
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// User's answer
    /// </summary>
    [Required]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Time spent on this question in seconds
    /// </summary>
    public int? TimeSpentSeconds { get; set; }
}

/// <summary>
/// Response model for exercise attempt data
/// </summary>
public class ExerciseAttemptResponse
{
    /// <summary>
    /// Attempt unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Exercise ID
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Attempt status
    /// </summary>
    public AttemptStatus Status { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Completion time (null if not completed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Expiration time (null if no time limit)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Total score achieved
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// Maximum possible score
    /// </summary>
    public int MaxPossibleScore { get; set; }

    /// <summary>
    /// Score as percentage
    /// </summary>
    public double ScorePercentage { get; set; }

    /// <summary>
    /// Whether the attempt passed
    /// </summary>
    public bool IsPassed { get; set; }

    /// <summary>
    /// Time spent on the exercise
    /// </summary>
    public TimeSpan? TimeSpent { get; set; }

    /// <summary>
    /// Number of questions answered
    /// </summary>
    public int QuestionsAnswered { get; set; }

    /// <summary>
    /// Total number of questions
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Completion percentage
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// Remaining time (for in-progress attempts)
    /// </summary>
    public TimeSpan? RemainingTime { get; set; }

    /// <summary>
    /// User notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Exercise basic information
    /// </summary>
    public AttemptExerciseDto Exercise { get; set; } = new();

    /// <summary>
    /// Question answers (only shown after completion)
    /// </summary>
    public List<AttemptQuestionAnswerDto>? Answers { get; set; }

    /// <summary>
    /// Attempt results and feedback
    /// </summary>
    public AttemptResultDto? Result { get; set; }
}

/// <summary>
/// Exercise information for attempts
/// </summary>
public class AttemptExerciseDto
{
    /// <summary>
    /// Exercise ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Exercise type
    /// </summary>
    public ExerciseType Type { get; set; }

    /// <summary>
    /// Time limit in minutes
    /// </summary>
    public int? TimeLimit { get; set; }

    /// <summary>
    /// Passing score percentage
    /// </summary>
    public int PassingScore { get; set; }

    /// <summary>
    /// Total questions
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Maximum score
    /// </summary>
    public int MaxScore { get; set; }
}

/// <summary>
/// Question answer details for attempts
/// </summary>
public class AttemptQuestionAnswerDto
{
    /// <summary>
    /// Question ID
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Question text
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// User's answer
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Correct answer (shown after completion)
    /// </summary>
    public string? CorrectAnswer { get; set; }

    /// <summary>
    /// Whether the answer was correct
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Points earned for this question
    /// </summary>
    public int PointsEarned { get; set; }

    /// <summary>
    /// Maximum points for this question
    /// </summary>
    public int MaxPoints { get; set; }

    /// <summary>
    /// Time spent on this question
    /// </summary>
    public TimeSpan? TimeSpent { get; set; }

    /// <summary>
    /// Answer explanation or feedback
    /// </summary>
    public string? Explanation { get; set; }
}

/// <summary>
/// Attempt result and analysis
/// </summary>
public class AttemptResultDto
{
    /// <summary>
    /// Overall performance level
    /// </summary>
    public string PerformanceLevel { get; set; } = string.Empty;

    /// <summary>
    /// Strengths identified in the attempt
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Areas for improvement
    /// </summary>
    public List<string> AreasForImprovement { get; set; } = new();

    /// <summary>
    /// Recommendations for further study
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Reading speed analysis (for applicable exercises)
    /// </summary>
    public ReadingSpeedAnalysisDto? ReadingSpeedAnalysis { get; set; }

    /// <summary>
    /// Comprehension analysis by categories
    /// </summary>
    public Dictionary<string, CategoryScoreDto>? ComprehensionAnalysis { get; set; }
}

/// <summary>
/// Reading speed analysis DTO
/// </summary>
public class ReadingSpeedAnalysisDto
{
    /// <summary>
    /// Words per minute achieved
    /// </summary>
    public int WordsPerMinute { get; set; }

    /// <summary>
    /// Reading speed level assessment
    /// </summary>
    public string SpeedLevel { get; set; } = string.Empty;

    /// <summary>
    /// Speed percentile compared to other users
    /// </summary>
    public int SpeedPercentile { get; set; }

    /// <summary>
    /// Feedback on reading speed
    /// </summary>
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// Category score DTO for comprehension analysis
/// </summary>
public class CategoryScoreDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Score in this category
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Maximum possible score in this category
    /// </summary>
    public int MaxScore { get; set; }

    /// <summary>
    /// Score as percentage
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Number of questions in this category
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// Number of correct answers in this category
    /// </summary>
    public int CorrectAnswers { get; set; }
}

/// <summary>
/// Request model for getting user's exercise attempts
/// </summary>
public class GetUserAttemptsRequest
{
    /// <summary>
    /// Filter by exercise ID
    /// </summary>
    public Guid? ExerciseId { get; set; }

    /// <summary>
    /// Filter by attempt status
    /// </summary>
    public AttemptStatus? Status { get; set; }

    /// <summary>
    /// Filter by date range - start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by date range - end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Only show passed attempts
    /// </summary>
    public bool? OnlyPassed { get; set; }

    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = "StartedAt";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Exercise attempt summary for listings
/// </summary>
public class ExerciseAttemptSummaryDto
{
    /// <summary>
    /// Attempt ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Exercise ID
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string ExerciseTitle { get; set; } = string.Empty;

    /// <summary>
    /// Exercise type
    /// </summary>
    public ExerciseType ExerciseType { get; set; }

    /// <summary>
    /// Attempt status
    /// </summary>
    public AttemptStatus Status { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Score percentage
    /// </summary>
    public double ScorePercentage { get; set; }

    /// <summary>
    /// Whether passed
    /// </summary>
    public bool IsPassed { get; set; }

    /// <summary>
    /// Time spent
    /// </summary>
    public TimeSpan? TimeSpent { get; set; }
}