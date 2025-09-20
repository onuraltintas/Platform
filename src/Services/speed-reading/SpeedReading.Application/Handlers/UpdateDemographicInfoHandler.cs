using MediatR;
using SpeedReading.Application.Commands;
using SpeedReading.Application.DTOs;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;

namespace SpeedReading.Application.Handlers;

public class UpdateDemographicInfoHandler : IRequestHandler<UpdateDemographicInfoCommand, UserProfileDto>
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IProfileValidationService _validationService;

    public UpdateDemographicInfoHandler(
        IUserProfileRepository profileRepository,
        IProfileValidationService validationService)
    {
        _profileRepository = profileRepository;
        _validationService = validationService;
    }

    public async Task<UserProfileDto> Handle(UpdateDemographicInfoCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile not found for user {request.UserId}");
        }

        // Validation
        var demographicInfo = new DemographicInfoDto
        {
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            City = request.City,
            District = request.District,
            GradeLevel = request.GradeLevel,
            SchoolType = request.SchoolType
        };

        var validationResult = await _validationService.ValidateProfileAsync(demographicInfo, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        // Update demographics
        var demographics = new Demographics(
            request.DateOfBirth,
            (Gender)request.Gender,
            request.City,
            request.District,
            request.GradeLevel,
            (SchoolType)request.SchoolType
        );

        profile.UpdateDemographics(demographics);

        var updatedProfile = await _profileRepository.UpdateAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return updatedProfile.MapToDto();
    }
}

// Extension method sınıfını CreateUserProfileHandler içinde static helper olarak kullanabiliriz
public static class UserProfileExtensions
{
    public static UserProfileDto MapToDto(this SpeedReading.Domain.Entities.UserReadingProfile profile)
    {
        return new UserProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            DateOfBirth = profile.Demographics.DateOfBirth,
            Age = profile.Demographics.CalculateAge(),
            Gender = profile.Demographics.Gender,
            GenderDisplay = GetGenderDisplay(profile.Demographics.Gender),
            City = profile.Demographics.City,
            District = profile.Demographics.District,
            GradeLevel = profile.Demographics.GradeLevel,
            GradeLevelDisplay = GetGradeLevelDisplay(profile.Demographics.GradeLevel),
            SchoolType = profile.Demographics.SchoolType,
            SchoolTypeDisplay = GetSchoolTypeDisplay(profile.Demographics.SchoolType),
            EducationCategory = profile.Demographics.GetEducationCategory(),
            EducationCategoryDisplay = GetEducationCategoryDisplay(profile.Demographics.GetEducationCategory()),
            CurrentLevel = profile.CurrentLevel,
            CurrentLevelDisplay = GetReadingLevelDisplay(profile.CurrentLevel),
            TargetReadingSpeed = profile.Preferences.TargetReadingSpeed,
            PreferredTextTypes = profile.Preferences.PreferredTextTypes,
            ReadingGoals = profile.Preferences.ReadingGoals,
            PreferredLanguage = profile.Preferences.PreferredLanguage,
            FontSize = profile.Preferences.FontSize,
            LineSpacing = profile.Preferences.LineSpacing,
            BackgroundColor = profile.Preferences.BackgroundColor,
            TextColor = profile.Preferences.TextColor,
            TotalReadingTime = profile.TotalReadingTime,
            TotalWordsRead = profile.TotalWordsRead,
            AverageReadingSpeed = profile.AverageReadingSpeed,
            AverageComprehension = profile.AverageComprehension,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            IsActive = profile.IsActive,
            IsProfileComplete = profile.IsProfileComplete()
        };
    }

    private static string GetGenderDisplay(Gender gender) => gender switch
    {
        Gender.Male => "Erkek",
        Gender.Female => "Kadın",
        _ => "Belirtilmemiş"
    };

    private static string? GetGradeLevelDisplay(int? gradeLevel) => gradeLevel switch
    {
        >= 1 and <= 4 => $"{gradeLevel}. Sınıf",
        >= 5 and <= 8 => $"{gradeLevel}. Sınıf",
        >= 9 and <= 12 => $"{gradeLevel}. Sınıf",
        13 => "Üniversite 1. Yıl",
        14 => "Üniversite 2. Yıl",
        15 => "Üniversite 3. Yıl",
        16 => "Üniversite 4. Yıl",
        17 => "Yüksek Lisans",
        18 => "Doktora",
        19 => "Yetişkin Eğitimi",
        _ => gradeLevel?.ToString()
    };

    private static string GetSchoolTypeDisplay(SchoolType schoolType) => schoolType switch
    {
        SchoolType.Public => "Devlet",
        SchoolType.Private => "Özel",
        SchoolType.Homeschool => "Evde Eğitim",
        SchoolType.OpenEducation => "Açık Öğretim",
        SchoolType.Online => "Online Eğitim",
        _ => "Belirtilmemiş"
    };

    private static string GetEducationCategoryDisplay(EducationCategory category) => category switch
    {
        EducationCategory.Elementary => "İlkokul",
        EducationCategory.MiddleSchool => "Ortaokul",
        EducationCategory.HighSchool => "Lise",
        EducationCategory.University => "Üniversite",
        EducationCategory.Graduate => "Lisansüstü",
        EducationCategory.Adult => "Yetişkin",
        _ => "Diğer"
    };

    private static string GetReadingLevelDisplay(ReadingLevel level) => level switch
    {
        ReadingLevel.Beginner => "Başlangıç",
        ReadingLevel.Intermediate => "Orta",
        ReadingLevel.Advanced => "İleri",
        ReadingLevel.Expert => "Uzman",
        _ => level.ToString()
    };
}