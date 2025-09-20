using System.ComponentModel.DataAnnotations;
using SpeedReading.Domain.Enums;

namespace SpeedReading.API.Models;

/// <summary>
/// Request model for creating a new reading text
/// </summary>
public class CreateReadingTextRequest
{
    /// <summary>
    /// Title of the reading text
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content of the reading text
    /// </summary>
    [Required]
    [StringLength(50000, MinimumLength = 50, ErrorMessage = "Content must be between 50 and 50,000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source of the reading text (e.g., author, publication)
    /// </summary>
    [StringLength(300, ErrorMessage = "Source cannot exceed 300 characters")]
    public string? Source { get; set; }

    /// <summary>
    /// Target education level for this text
    /// </summary>
    [Required]
    public EducationCategory TargetEducationLevel { get; set; }

    /// <summary>
    /// Text category or genre
    /// </summary>
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Language of the text
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    public string Language { get; set; } = "tr";

    /// <summary>
    /// Whether the text is published and available for use
    /// </summary>
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// Request model for updating an existing reading text
/// </summary>
public class UpdateReadingTextRequest
{
    /// <summary>
    /// Title of the reading text
    /// </summary>
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Content of the reading text
    /// </summary>
    [StringLength(50000, MinimumLength = 50, ErrorMessage = "Content must be between 50 and 50,000 characters")]
    public string? Content { get; set; }

    /// <summary>
    /// Source of the reading text
    /// </summary>
    [StringLength(300, ErrorMessage = "Source cannot exceed 300 characters")]
    public string? Source { get; set; }

    /// <summary>
    /// Target education level for this text
    /// </summary>
    public EducationCategory? TargetEducationLevel { get; set; }

    /// <summary>
    /// Text category or genre
    /// </summary>
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Whether the text is published and available for use
    /// </summary>
    public bool? IsPublished { get; set; }
}

/// <summary>
/// Response model for reading text data
/// </summary>
public class ReadingTextResponse
{
    /// <summary>
    /// Unique identifier for the reading text
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title of the reading text
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content of the reading text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source of the reading text
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Target education level for this text
    /// </summary>
    public EducationCategory TargetEducationLevel { get; set; }

    /// <summary>
    /// Text category or genre
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Language of the text
    /// </summary>
    public string Language { get; set; } = "tr";

    /// <summary>
    /// Whether the text is published and available for use
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Text difficulty assessment
    /// </summary>
    public TextDifficulty Difficulty { get; set; }

    /// <summary>
    /// Text creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Text last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Text statistics
    /// </summary>
    public TextStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// Text metadata
    /// </summary>
    public TextMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// Text statistics DTO
/// </summary>
public class TextStatisticsDto
{
    /// <summary>
    /// Total word count
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Total sentence count
    /// </summary>
    public int SentenceCount { get; set; }

    /// <summary>
    /// Total paragraph count
    /// </summary>
    public int ParagraphCount { get; set; }

    /// <summary>
    /// Average words per sentence
    /// </summary>
    public double AverageWordsPerSentence { get; set; }

    /// <summary>
    /// Average sentence length
    /// </summary>
    public double AverageSentenceLength { get; set; }

    /// <summary>
    /// Average word length
    /// </summary>
    public double AverageWordLength { get; set; }

    /// <summary>
    /// Lexical diversity (unique words / total words)
    /// </summary>
    public double LexicalDiversity { get; set; }

    /// <summary>
    /// Readability score (0-100, higher is easier)
    /// </summary>
    public double ReadabilityScore { get; set; }

    /// <summary>
    /// Estimated reading time in minutes
    /// </summary>
    public int EstimatedReadingTimeMinutes { get; set; }
}

/// <summary>
/// Text metadata DTO
/// </summary>
public class TextMetadataDto
{
    /// <summary>
    /// Keywords extracted from the text
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Text summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Main topic or theme
    /// </summary>
    public string MainTopic { get; set; } = string.Empty;

    /// <summary>
    /// Estimated reading level
    /// </summary>
    public string ReadingLevel { get; set; } = string.Empty;

    /// <summary>
    /// Complex words count
    /// </summary>
    public int ComplexWordsCount { get; set; }

    /// <summary>
    /// Text quality score (0-100)
    /// </summary>
    public double QualityScore { get; set; }
}

/// <summary>
/// Request model for searching reading texts
/// </summary>
public class SearchReadingTextsRequest
{
    /// <summary>
    /// Search term for title and content
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by education level
    /// </summary>
    public EducationCategory? EducationLevel { get; set; }

    /// <summary>
    /// Filter by text difficulty
    /// </summary>
    public TextDifficulty? Difficulty { get; set; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Filter by language
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Only show published texts
    /// </summary>
    public bool? OnlyPublished { get; set; } = true;

    /// <summary>
    /// Minimum word count
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Minimum word count cannot be negative")]
    public int? MinWordCount { get; set; }

    /// <summary>
    /// Maximum word count
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Maximum word count cannot be negative")]
    public int? MaxWordCount { get; set; }

    /// <summary>
    /// Sort by field
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

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
/// Request model for analyzing text
/// </summary>
public class AnalyzeTextRequest
{
    /// <summary>
    /// Text content to analyze
    /// </summary>
    [Required]
    [StringLength(50000, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 50,000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Language of the text
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    public string Language { get; set; } = "tr";
}

/// <summary>
/// Response model for text analysis
/// </summary>
public class TextAnalysisResponse
{
    /// <summary>
    /// Text statistics
    /// </summary>
    public TextStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// Text difficulty assessment
    /// </summary>
    public TextDifficulty Difficulty { get; set; }

    /// <summary>
    /// Recommended education level
    /// </summary>
    public EducationCategory RecommendedEducationLevel { get; set; }

    /// <summary>
    /// Text metadata including keywords and summary
    /// </summary>
    public TextMetadataDto Metadata { get; set; } = new();

    /// <summary>
    /// Readability recommendations
    /// </summary>
    public List<string> ReadabilityRecommendations { get; set; } = new();
}