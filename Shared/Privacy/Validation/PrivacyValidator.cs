using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using EgitimPlatform.Shared.Privacy.Attributes;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Services;

namespace EgitimPlatform.Shared.Privacy.Validation;

public class PrivacyValidator : IPrivacyValidator
{
    private readonly ILogger<PrivacyValidator> _logger;
    private readonly PrivacyOptions _options;
    private readonly IConsentService _consentService;
    private readonly IDataSubjectRightsService _dataSubjectRightsService;
    private readonly IDataProcessingActivityService _activityService;

    public PrivacyValidator(
        ILogger<PrivacyValidator> logger,
        IOptions<PrivacyOptions> options,
        IConsentService consentService,
        IDataSubjectRightsService dataSubjectRightsService,
        IDataProcessingActivityService activityService)
    {
        _logger = logger;
        _options = options.Value;
        _consentService = consentService;
        _dataSubjectRightsService = dataSubjectRightsService;
        _activityService = activityService;
    }

    public async Task<ValidationResult> ValidateConsentRequiredAsync<T>(T entity, string userId) where T : class
    {
        var result = new ValidationResult { IsValid = true };
        var entityType = typeof(T);

        var personalDataProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<PersonalDataAttribute>() != null)
            .ToList();

        foreach (var property in personalDataProperties)
        {
            var attr = property.GetCustomAttribute<PersonalDataAttribute>()!;
            
            if (attr.RequiresConsent)
            {
                var hasConsent = await _consentService.HasValidConsentAsync(userId, attr.Category, attr.Purpose);
                
                if (!hasConsent)
                {
                    result.AddError($"Missing consent for {property.Name} ({attr.Category})");
                    _logger.LogWarning("Missing consent for user {UserId} property {Property} category {Category}",
                        userId, property.Name, attr.Category);
                }
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDataRetentionAsync<T>(T entity) where T : class
    {
        var result = new ValidationResult { IsValid = true };
        var entityType = typeof(T);

        var retentionProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<DataRetentionAttribute>() != null)
            .ToList();

        foreach (var property in retentionProperties)
        {
            var attr = property.GetCustomAttribute<DataRetentionAttribute>()!;
            
            // Check if data has exceeded retention period
            var createdAtProperty = entityType.GetProperty("CreatedAt") ?? entityType.GetProperty("Created");
            if (createdAtProperty?.GetValue(entity) is DateTime createdAt)
            {
                var retentionExpiry = createdAt.AddDays(attr.RetentionPeriodDays);
                
                if (DateTime.UtcNow > retentionExpiry)
                {
                    if (attr.AutoDelete)
                    {
                        result.AddError($"Data retention period exceeded for {property.Name} - automatic deletion required");
                    }
                    else
                    {
                        result.AddWarning($"Data retention period exceeded for {property.Name}");
                    }
                }
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<ValidationResult> ValidatePersonalDataProcessingAsync<T>(T entity, DataProcessingLawfulBasis lawfulBasis) where T : class
    {
        var result = new ValidationResult { IsValid = true };
        var entityType = typeof(T);

        var personalDataProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<PersonalDataAttribute>() != null)
            .ToList();

        foreach (var property in personalDataProperties)
        {
            var attr = property.GetCustomAttribute<PersonalDataAttribute>()!;
            
            // Validate lawful basis matches attribute requirements
            if (attr.LawfulBasis != lawfulBasis)
            {
                result.AddWarning($"Lawful basis mismatch for {property.Name}: expected {attr.LawfulBasis}, got {lawfulBasis}");
            }

            // Check encryption requirements
            if (attr.IsEncrypted && !HasEncryptionAttribute(property))
            {
                result.AddError($"Property {property.Name} requires encryption but is not encrypted");
            }

            // Check pseudonymization requirements
            if (attr.IsPseudonymized && !HasPseudonymizationAttribute(property))
            {
                result.AddError($"Property {property.Name} requires pseudonymization but is not pseudonymized");
            }
        }

        return await Task.FromResult(result);
    }

    public async Task<ValidationResult> ValidateSensitiveDataProcessingAsync<T>(T entity, string userId) where T : class
    {
        var result = new ValidationResult { IsValid = true };
        var entityType = typeof(T);

        var sensitiveDataProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<SensitivePersonalDataAttribute>() != null)
            .ToList();

        foreach (var property in sensitiveDataProperties)
        {
            var attr = property.GetCustomAttribute<SensitivePersonalDataAttribute>()!;
            
            // Sensitive data requires explicit consent
            if (attr.RequiresExplicitConsent)
            {
                var hasConsent = await _consentService.HasValidConsentAsync(
                    userId, PersonalDataCategory.SensitivePersonalData, attr.Purpose);
                
                if (!hasConsent)
                {
                    result.AddError($"Missing explicit consent for sensitive data: {property.Name}");
                }
            }

            // Sensitive data requires special protection (encryption)
            if (attr.RequiresSpecialProtection && !HasEncryptionAttribute(property))
            {
                result.AddError($"Sensitive data {property.Name} requires encryption");
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDataSubjectRightRequestAsync(string userId, DataSubjectRightType requestType)
    {
        var result = new ValidationResult { IsValid = true };

        // Check if user can make this type of request
        var canMakeRequest = await _dataSubjectRightsService.CanUserMakeRequestAsync(userId, requestType);
        
        if (!canMakeRequest)
        {
            result.AddError($"User has exceeded the maximum number of {requestType} requests allowed");
        }

        // Check request type specific validations
        switch (requestType)
        {
            case DataSubjectRightType.Erasure:
                // Validate if erasure is possible (no legal obligations preventing it)
                var hasLegalObligations = await CheckLegalObligationsAsync(userId);
                if (hasLegalObligations)
                {
                    result.AddWarning("Some data may not be erasable due to legal obligations");
                }
                break;

            case DataSubjectRightType.DataPortability:
                // Validate if data portability is applicable
                var hasPortableData = await CheckPortableDataAsync(userId);
                if (!hasPortableData)
                {
                    result.AddWarning("Limited data available for portability");
                }
                break;
        }

        return result;
    }

    public async Task<ValidationResult> ValidateCrossContainerTransferAsync<T>(T entity, string targetContainer) where T : class
    {
        var result = new ValidationResult { IsValid = true };

        // Check if target container requires additional safeguards
        var isThirdCountry = IsThirdCountryContainer(targetContainer);
        
        if (isThirdCountry)
        {
            result.AddWarning("Cross-border transfer requires adequate safeguards");
            result.AddMetadata("RequiresAdequacyDecision", true);
            result.AddMetadata("RequiresSafeguards", true);
        }

        // Check data categories being transferred
        var entityType = typeof(T);
        var sensitiveProperties = entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<SensitivePersonalDataAttribute>() != null)
            .ToList();

        if (sensitiveProperties.Any() && isThirdCountry)
        {
            result.AddError("Sensitive data transfer to third country requires explicit consent and safeguards");
        }

        return await Task.FromResult(result);
    }

    public async Task<ValidationResult> ValidateDataProcessingActivityAsync(string activityId)
    {
        var result = new ValidationResult { IsValid = true };

        var isCompliant = await _activityService.ValidateActivityComplianceAsync(activityId);
        
        if (!isCompliant)
        {
            result.AddError($"Data processing activity {activityId} is not compliant");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateComplianceAsync<T>(T entity, string userId) where T : class
    {
        var result = new ValidationResult { IsValid = true };

        // Run all validation checks
        var consentValidation = await ValidateConsentRequiredAsync(entity, userId);
        var retentionValidation = await ValidateDataRetentionAsync(entity);
        var sensitiveDataValidation = await ValidateSensitiveDataProcessingAsync(entity, userId);

        // Combine results
        result.Errors.AddRange(consentValidation.Errors);
        result.Errors.AddRange(retentionValidation.Errors);
        result.Errors.AddRange(sensitiveDataValidation.Errors);

        result.Warnings.AddRange(consentValidation.Warnings);
        result.Warnings.AddRange(retentionValidation.Warnings);
        result.Warnings.AddRange(sensitiveDataValidation.Warnings);

        result.IsValid = !result.Errors.Any();

        // Add compliance metadata
        result.AddMetadata("ComplianceChecksRun", new[] { "Consent", "Retention", "SensitiveData" });
        result.AddMetadata("ValidationTimestamp", DateTime.UtcNow);

        if (!result.IsValid)
        {
            _logger.LogWarning("Compliance validation failed for entity {EntityType} user {UserId}: {Errors}",
                typeof(T).Name, userId, string.Join("; ", result.Errors));
        }

        return result;
    }

    private static bool HasEncryptionAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<EncryptedDataAttribute>() != null;
    }

    private static bool HasPseudonymizationAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<PseudonymizedDataAttribute>() != null;
    }

    private async Task<bool> CheckLegalObligationsAsync(string userId)
    {
        // This would check if there are any legal obligations preventing data erasure
        // For demo purposes, we'll return false
        return await Task.FromResult(false);
    }

    private async Task<bool> CheckPortableDataAsync(string userId)
    {
        // This would check if the user has data that can be ported
        // For demo purposes, we'll return true
        return await Task.FromResult(true);
    }

    private static bool IsThirdCountryContainer(string container)
    {
        // This would check if the target container is in a third country
        // For demo purposes, we'll check for common third country indicators
        var thirdCountryIndicators = new[] { "us-", "cn-", "ru-", "in-" };
        return thirdCountryIndicators.Any(indicator => container.ToLowerInvariant().StartsWith(indicator));
    }
}