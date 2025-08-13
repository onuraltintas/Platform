using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Validation;

public interface IPrivacyValidator
{
    Task<ValidationResult> ValidateConsentRequiredAsync<T>(T entity, string userId) where T : class;
    Task<ValidationResult> ValidateDataRetentionAsync<T>(T entity) where T : class;
    Task<ValidationResult> ValidatePersonalDataProcessingAsync<T>(T entity, DataProcessingLawfulBasis lawfulBasis) where T : class;
    Task<ValidationResult> ValidateSensitiveDataProcessingAsync<T>(T entity, string userId) where T : class;
    Task<ValidationResult> ValidateDataSubjectRightRequestAsync(string userId, DataSubjectRightType requestType);
    Task<ValidationResult> ValidateCrossContainerTransferAsync<T>(T entity, string targetContainer) where T : class;
    Task<ValidationResult> ValidateDataProcessingActivityAsync(string activityId);
    Task<ValidationResult> ValidateComplianceAsync<T>(T entity, string userId) where T : class;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}