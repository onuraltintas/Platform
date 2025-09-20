using MediatR;
using SpeedReading.Application.Commands;
using SpeedReading.Application.DTOs;
using SpeedReading.Application.Interfaces;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Application.Handlers;

public class CreateUserProfileHandler : IRequestHandler<CreateUserProfileCommand, UserProfileDto>
{
    private readonly IUserProfileRepository _profileRepository;

    public CreateUserProfileHandler(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<UserProfileDto> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // Zaten profil var mı kontrol et
        var existingProfile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingProfile != null)
        {
            throw new InvalidOperationException($"User {request.UserId} already has a profile");
        }

        var profile = new UserReadingProfile(request.UserId);
        
        var savedProfile = await _profileRepository.AddAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(savedProfile);
    }

    public static UserProfileDto MapToDto(UserReadingProfile profile)
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