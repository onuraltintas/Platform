using System.ComponentModel.DataAnnotations;
using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PersonalDataAttribute : Attribute
{
    public PersonalDataCategory Category { get; }
    public DataProcessingLawfulBasis LawfulBasis { get; set; } = DataProcessingLawfulBasis.Consent;
    public string Purpose { get; set; } = string.Empty;
    public int RetentionPeriodDays { get; set; } = 365;
    public bool RequiresConsent { get; set; } = true;
    public bool IsEncrypted { get; set; } = false;
    public bool IsPseudonymized { get; set; } = false;
    public string Description { get; set; } = string.Empty;

    public PersonalDataAttribute(PersonalDataCategory category)
    {
        Category = category;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SensitivePersonalDataAttribute : PersonalDataAttribute
{
    public bool RequiresExplicitConsent { get; set; } = true;
    public bool RequiresSpecialProtection { get; set; } = true;

    public SensitivePersonalDataAttribute() : base(PersonalDataCategory.SensitivePersonalData)
    {
        RequiresConsent = true;
        IsEncrypted = true;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PseudonymizedDataAttribute : Attribute
{
    public string PseudonymizationMethod { get; set; } = string.Empty;
    public bool IsReversible { get; set; } = true;
    public string KeyStorageLocation { get; set; } = string.Empty;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class EncryptedDataAttribute : Attribute
{
    public string EncryptionMethod { get; set; } = "AES-256";
    public string KeyManagementService { get; set; } = string.Empty;
    public bool IsTransparentEncryption { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DataRetentionAttribute : Attribute
{
    public int RetentionPeriodDays { get; }
    public DataRetentionReason Reason { get; set; } = DataRetentionReason.ConsentGiven;
    public string Justification { get; set; } = string.Empty;
    public bool AutoDelete { get; set; } = true;

    public DataRetentionAttribute(int retentionPeriodDays)
    {
        RetentionPeriodDays = retentionPeriodDays;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DataProcessingActivityAttribute : Attribute
{
    public string ActivityName { get; }
    public DataProcessingLawfulBasis LawfulBasis { get; set; } = DataProcessingLawfulBasis.Consent;
    public string Purpose { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public bool RequiresDPIA { get; set; } = false;

    public DataProcessingActivityAttribute(string activityName)
    {
        ActivityName = activityName;
    }
}