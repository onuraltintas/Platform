namespace SpeedReading.Application.DTOs;

public class ProfileCompletionStatusDto
{
    public Guid UserId { get; set; }
    public bool IsComplete { get; set; }
    public int CompletionPercentage { get; set; }
    public string[] MissingFields { get; set; } = Array.Empty<string>();
    public string NextStepMessage { get; set; } = string.Empty;
    public string[] RequiredSteps { get; set; } = Array.Empty<string>();
}

public class DemographicInfoDto
{
    public DateTime? DateOfBirth { get; set; }
    public int Gender { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public int? GradeLevel { get; set; }
    public int SchoolType { get; set; }
}

public class ReadingPreferencesDto
{
    public int? TargetReadingSpeed { get; set; }
    public string[]? PreferredTextTypes { get; set; }
    public string? ReadingGoals { get; set; }
    public string PreferredLanguage { get; set; } = "tr-TR";
    public int FontSize { get; set; } = 14;
    public float LineSpacing { get; set; } = 1.5f;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#000000";
}