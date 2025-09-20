using SpeedReading.Application.DTOs;

namespace SpeedReading.Application.Interfaces;

public interface IProfileValidationService
{
    Task<ValidationResult> ValidateProfileAsync(DemographicInfoDto demographicInfo, CancellationToken cancellationToken = default);
    Task<bool> ValidateAgeGradeCompatibilityAsync(DateTime? dateOfBirth, int? gradeLevel, CancellationToken cancellationToken = default);
    Task<bool> ValidateCityDistrictAsync(string? city, string? district, CancellationToken cancellationToken = default);
    ValidationResult ValidateReadingPreferences(ReadingPreferencesDto preferences);
    ProfileCompletionStatusDto CalculateCompletionStatus(DemographicInfoDto demographicInfo, ReadingPreferencesDto? preferences = null);
}

public interface IAgeGradeValidationService
{
    bool IsCompatible(int age, int gradeLevel);
    (int Min, int Max) GetExpectedAgeRange(int gradeLevel);
    int CalculateAge(DateTime dateOfBirth);
    string GetCompatibilityMessage(int age, int gradeLevel);
}

public interface ICityDistrictValidationService
{
    Task<bool> ValidateAsync(string cityName, string districtName, CancellationToken cancellationToken = default);
    Task<List<string>> GetCitySuggestionsAsync(string partialName, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistrictSuggestionsAsync(string cityName, string partialDistrictName, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public ValidationResult()
    {
        IsValid = true;
    }
    
    public ValidationResult(params string[] errors)
    {
        IsValid = false;
        Errors.AddRange(errors);
    }
    
    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}