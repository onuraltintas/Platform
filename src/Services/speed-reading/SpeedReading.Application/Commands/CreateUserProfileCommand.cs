using MediatR;
using SpeedReading.Application.DTOs;

namespace SpeedReading.Application.Commands;

public class CreateUserProfileCommand : IRequest<UserProfileDto>
{
    public Guid UserId { get; set; }
    
    public CreateUserProfileCommand(Guid userId)
    {
        UserId = userId;
    }
}

public class UpdateDemographicInfoCommand : IRequest<UserProfileDto>
{
    public Guid UserId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int Gender { get; set; } // 0=NotSpecified, 1=Male, 2=Female
    public string? City { get; set; }
    public string? District { get; set; }
    public int? GradeLevel { get; set; }
    public int SchoolType { get; set; } // 0=NotSpecified, 1=Public, 2=Private, etc.
}

public class UpdateReadingPreferencesCommand : IRequest<UserProfileDto>
{
    public Guid UserId { get; set; }
    public int? TargetReadingSpeed { get; set; }
    public string[]? PreferredTextTypes { get; set; }
    public string? ReadingGoals { get; set; }
    public string PreferredLanguage { get; set; } = "tr-TR";
    public int FontSize { get; set; } = 14;
    public float LineSpacing { get; set; } = 1.5f;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#000000";
}

public class CompleteUserProfileCommand : IRequest<UserProfileDto>
{
    public Guid UserId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Gender { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public int GradeLevel { get; set; }
    public int SchoolType { get; set; }
    public int? TargetReadingSpeed { get; set; }
    public string[]? PreferredTextTypes { get; set; }
    public string? ReadingGoals { get; set; }
}