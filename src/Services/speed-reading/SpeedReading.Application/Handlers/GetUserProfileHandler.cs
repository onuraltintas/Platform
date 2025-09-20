using MediatR;
using SpeedReading.Application.DTOs;
using SpeedReading.Application.Interfaces;
using SpeedReading.Application.Queries;

namespace SpeedReading.Application.Handlers;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserProfileRepository _profileRepository;

    public GetUserProfileHandler(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        return profile?.MapToDto();
    }
}

public class GetProfileCompletionStatusHandler : IRequestHandler<GetProfileCompletionStatusQuery, ProfileCompletionStatusDto>
{
    private readonly IUserProfileRepository _profileRepository;
    private readonly IProfileValidationService _validationService;

    public GetProfileCompletionStatusHandler(
        IUserProfileRepository profileRepository,
        IProfileValidationService validationService)
    {
        _profileRepository = profileRepository;
        _validationService = validationService;
    }

    public async Task<ProfileCompletionStatusDto> Handle(GetProfileCompletionStatusQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        if (profile == null)
        {
            return new ProfileCompletionStatusDto
            {
                UserId = request.UserId,
                IsComplete = false,
                CompletionPercentage = 0,
                MissingFields = new[] { "Profil oluşturulmamış" },
                NextStepMessage = "Önce profil oluşturmanız gerekiyor",
                RequiredSteps = new[] { "Profil oluştur", "Demografik bilgileri doldur", "Okuma tercihlerini ayarla" }
            };
        }

        var demographicInfo = new DemographicInfoDto
        {
            DateOfBirth = profile.Demographics.DateOfBirth,
            Gender = (int)profile.Demographics.Gender,
            City = profile.Demographics.City,
            District = profile.Demographics.District,
            GradeLevel = profile.Demographics.GradeLevel,
            SchoolType = (int)profile.Demographics.SchoolType
        };

        var readingPreferences = new ReadingPreferencesDto
        {
            TargetReadingSpeed = profile.Preferences.TargetReadingSpeed,
            PreferredTextTypes = profile.Preferences.PreferredTextTypes,
            ReadingGoals = profile.Preferences.ReadingGoals,
            PreferredLanguage = profile.Preferences.PreferredLanguage,
            FontSize = profile.Preferences.FontSize,
            LineSpacing = profile.Preferences.LineSpacing,
            BackgroundColor = profile.Preferences.BackgroundColor,
            TextColor = profile.Preferences.TextColor
        };

        return _validationService.CalculateCompletionStatus(demographicInfo, readingPreferences);
    }
}