using SpeedReading.Application.DTOs;
using SpeedReading.Application.Interfaces;

namespace SpeedReading.Application.Services;

public class ProfileValidationService : IProfileValidationService
{
    private readonly IAgeGradeValidationService _ageGradeValidationService;
    private readonly ICityDistrictValidationService _cityDistrictValidationService;

    public ProfileValidationService(
        IAgeGradeValidationService ageGradeValidationService,
        ICityDistrictValidationService cityDistrictValidationService)
    {
        _ageGradeValidationService = ageGradeValidationService;
        _cityDistrictValidationService = cityDistrictValidationService;
    }

    public async Task<ValidationResult> ValidateProfileAsync(DemographicInfoDto demographicInfo, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Yaş doğrulaması
        if (demographicInfo.DateOfBirth.HasValue)
        {
            var age = _ageGradeValidationService.CalculateAge(demographicInfo.DateOfBirth.Value);
            if (age < 5 || age > 100)
            {
                result.AddError("Yaş 5-100 arasında olmalıdır.");
            }
        }

        // Cinsiyet doğrulaması
        if (demographicInfo.Gender < 0 || demographicInfo.Gender > 2)
        {
            result.AddError("Geçerli bir cinsiyet seçiniz.");
        }

        // Sınıf seviyesi doğrulaması
        if (demographicInfo.GradeLevel.HasValue)
        {
            if (demographicInfo.GradeLevel.Value < 1 || demographicInfo.GradeLevel.Value > 20)
            {
                result.AddError("Sınıf seviyesi 1-20 arasında olmalıdır.");
            }
        }

        // Şehir-İlçe doğrulaması
        if (!string.IsNullOrEmpty(demographicInfo.City) && !string.IsNullOrEmpty(demographicInfo.District))
        {
            var isCityDistrictValid = await _cityDistrictValidationService.ValidateAsync(
                demographicInfo.City, 
                demographicInfo.District, 
                cancellationToken);
                
            if (!isCityDistrictValid)
            {
                result.AddError("Seçilen şehir ve ilçe kombinasyonu geçerli değil.");
            }
        }

        // Yaş-Sınıf uyumluluğu
        if (demographicInfo.DateOfBirth.HasValue && demographicInfo.GradeLevel.HasValue)
        {
            var age = _ageGradeValidationService.CalculateAge(demographicInfo.DateOfBirth.Value);
            var isCompatible = _ageGradeValidationService.IsCompatible(age, demographicInfo.GradeLevel.Value);
            
            if (!isCompatible)
            {
                var message = _ageGradeValidationService.GetCompatibilityMessage(age, demographicInfo.GradeLevel.Value);
                result.AddWarning(message);
            }
        }

        return result;
    }

    public async Task<bool> ValidateAgeGradeCompatibilityAsync(DateTime? dateOfBirth, int? gradeLevel, CancellationToken cancellationToken = default)
    {
        if (!dateOfBirth.HasValue || !gradeLevel.HasValue)
        {
            return true; // Eksik bilgi varsa geçerli sayıyoruz
        }

        var age = _ageGradeValidationService.CalculateAge(dateOfBirth.Value);
        return _ageGradeValidationService.IsCompatible(age, gradeLevel.Value);
    }

    public async Task<bool> ValidateCityDistrictAsync(string? city, string? district, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district))
        {
            return true; // Eksik bilgi varsa geçerli sayıyoruz
        }

        return await _cityDistrictValidationService.ValidateAsync(city, district, cancellationToken);
    }

    public ValidationResult ValidateReadingPreferences(ReadingPreferencesDto preferences)
    {
        var result = new ValidationResult();

        // Hedef okuma hızı doğrulaması
        if (preferences.TargetReadingSpeed.HasValue)
        {
            if (preferences.TargetReadingSpeed.Value < 50 || preferences.TargetReadingSpeed.Value > 2000)
            {
                result.AddError("Hedef okuma hızı 50-2000 WPM arasında olmalıdır.");
            }
        }

        // Font boyutu doğrulaması
        if (preferences.FontSize < 8 || preferences.FontSize > 32)
        {
            result.AddError("Font boyutu 8-32 arasında olmalıdır.");
        }

        // Satır aralığı doğrulaması
        if (preferences.LineSpacing < 1.0f || preferences.LineSpacing > 3.0f)
        {
            result.AddError("Satır aralığı 1.0-3.0 arasında olmalıdır.");
        }

        // Renk kodları doğrulaması
        if (!IsValidHexColor(preferences.BackgroundColor))
        {
            result.AddError("Geçerli bir arka plan rengi kodu giriniz (örn: #FFFFFF).");
        }

        if (!IsValidHexColor(preferences.TextColor))
        {
            result.AddError("Geçerli bir metin rengi kodu giriniz (örn: #000000).");
        }

        // Dil doğrulaması
        if (string.IsNullOrEmpty(preferences.PreferredLanguage))
        {
            result.AddError("Tercih edilen dil belirtilmelidir.");
        }

        return result;
    }

    public ProfileCompletionStatusDto CalculateCompletionStatus(DemographicInfoDto demographicInfo, ReadingPreferencesDto? preferences = null)
    {
        var missingFields = new List<string>();
        var requiredSteps = new List<string>();
        int totalFields = 6; // Temel demografik alanlar
        int completedFields = 0;

        // Demografik bilgi kontrolleri
        if (!demographicInfo.DateOfBirth.HasValue)
        {
            missingFields.Add("Doğum Tarihi");
            requiredSteps.Add("Doğum tarihinizi giriniz");
        }
        else
        {
            completedFields++;
        }

        if (demographicInfo.Gender == 0)
        {
            missingFields.Add("Cinsiyet");
            requiredSteps.Add("Cinsiyetinizi seçiniz");
        }
        else
        {
            completedFields++;
        }

        if (string.IsNullOrEmpty(demographicInfo.City))
        {
            missingFields.Add("Şehir");
            requiredSteps.Add("Yaşadığınız şehri seçiniz");
        }
        else
        {
            completedFields++;
        }

        if (string.IsNullOrEmpty(demographicInfo.District))
        {
            missingFields.Add("İlçe");
            requiredSteps.Add("Yaşadığınız ilçeyi seçiniz");
        }
        else
        {
            completedFields++;
        }

        if (!demographicInfo.GradeLevel.HasValue)
        {
            missingFields.Add("Sınıf Seviyesi");
            requiredSteps.Add("Eğitim seviyenizi belirtiniz");
        }
        else
        {
            completedFields++;
        }

        if (demographicInfo.SchoolType == 0)
        {
            missingFields.Add("Okul Türü");
            requiredSteps.Add("Okul türünüzü seçiniz");
        }
        else
        {
            completedFields++;
        }

        // Tercihleri de sayıyorsak
        if (preferences != null)
        {
            totalFields += 2; // Temel tercihler
            
            if (preferences.TargetReadingSpeed.HasValue)
                completedFields++;
            else
                requiredSteps.Add("Hedef okuma hızınızı belirleyiniz");

            if (!string.IsNullOrEmpty(preferences.ReadingGoals))
                completedFields++;
            else
                requiredSteps.Add("Okuma hedeflerinizi yazınız");
        }

        int completionPercentage = (int)((double)completedFields / totalFields * 100);
        bool isComplete = missingFields.Count == 0;

        string nextStepMessage = isComplete 
            ? "Profiliniz tamamlandı! Hızlı okuma eğitimine başlayabilirsiniz."
            : requiredSteps.FirstOrDefault() ?? "Profil bilgilerinizi tamamlayınız";

        return new ProfileCompletionStatusDto
        {
            UserId = Guid.Empty, // Bu handler'da set edilecek
            IsComplete = isComplete,
            CompletionPercentage = completionPercentage,
            MissingFields = missingFields.ToArray(),
            NextStepMessage = nextStepMessage,
            RequiredSteps = requiredSteps.ToArray()
        };
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color))
            return false;

        if (!color.StartsWith("#"))
            return false;

        if (color.Length != 7)
            return false;

        return color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}