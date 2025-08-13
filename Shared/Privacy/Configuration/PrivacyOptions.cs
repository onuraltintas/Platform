using EgitimPlatform.Shared.Privacy.Enums;

namespace EgitimPlatform.Shared.Privacy.Configuration;

public class PrivacyOptions
{
    public const string SectionName = "Privacy";

    public ConsentManagementOptions ConsentManagement { get; set; } = new();
    public DataSubjectRightsOptions DataSubjectRights { get; set; } = new();
    public DataRetentionOptions DataRetention { get; set; } = new();
    public CookieConsentOptions CookieConsent { get; set; } = new();
    public ComplianceOptions Compliance { get; set; } = new();
}

public class ConsentManagementOptions
{
    public bool EnableConsentManagement { get; set; } = true;
    public int ConsentExpiryDays { get; set; } = 365;
    public bool RequireExplicitConsent { get; set; } = true;
    public bool EnableConsentWithdrawal { get; set; } = true;
    public bool LogConsentChanges { get; set; } = true;
    public List<string> RequiredConsentPurposes { get; set; } = new();
    public Dictionary<PersonalDataCategory, bool> ConsentRequiredByCategory { get; set; } = new();
}

public class DataSubjectRightsOptions
{
    public bool EnableDataSubjectRights { get; set; } = true;
    public int RequestProcessingTimeoutDays { get; set; } = 30;
    public bool AutoProcessPortabilityRequests { get; set; } = false;
    public bool AutoProcessErasureRequests { get; set; } = false;
    public bool RequireIdentityVerification { get; set; } = true;
    public int MaxRequestsPerUserPerMonth { get; set; } = 5;
    public List<string> NotificationEmails { get; set; } = new();
}

public class DataRetentionOptions
{
    public bool EnableAutoDataDeletion { get; set; } = true;
    public Dictionary<PersonalDataCategory, int> DefaultRetentionPeriods { get; set; } = new();
    public Dictionary<DataRetentionReason, int> RetentionPeriodsByReason { get; set; } = new();
    public bool RequireRetentionJustification { get; set; } = true;
    public int DeletionBatchSize { get; set; } = 1000;
    public string DeletionSchedule { get; set; } = "0 2 * * *"; // Daily at 2 AM
}

public class CookieConsentOptions
{
    public bool EnableCookieConsent { get; set; } = true;
    public List<string> EssentialCookies { get; set; } = new();
    public List<string> AnalyticalCookies { get; set; } = new();
    public List<string> MarketingCookies { get; set; } = new();
    public int CookieConsentExpiryDays { get; set; } = 365;
    public string CookiePolicyUrl { get; set; } = string.Empty;
}

public class ComplianceOptions
{
    public List<string> ApplicableRegulations { get; set; } = new() { "GDPR", "CCPA" };
    public string DataControllerName { get; set; } = string.Empty;
    public string DataControllerEmail { get; set; } = string.Empty;
    public string DataProtectionOfficerEmail { get; set; } = string.Empty;
    public string PrivacyPolicyUrl { get; set; } = string.Empty;
    public string TermsOfServiceUrl { get; set; } = string.Empty;
    public bool EnableComplianceAuditing { get; set; } = true;
    public bool EnablePrivacyDashboard { get; set; } = true;
}