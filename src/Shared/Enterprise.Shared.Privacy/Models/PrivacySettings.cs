namespace Enterprise.Shared.Privacy.Models;

public class PrivacySettings
{
    public DataAnonymizationSettings Anonymization { get; set; } = new();
    public ConsentManagementSettings ConsentManagement { get; set; } = new();
    public DataRetentionSettings DataRetention { get; set; } = new();
    public GdprComplianceSettings GdprCompliance { get; set; } = new();
    public AuditLoggingSettings AuditLogging { get; set; } = new();
}

public class DataAnonymizationSettings
{
    public bool EnableAnonymization { get; set; } = true;
    public string HashingSalt { get; set; } = string.Empty;
    public int HashingIterations { get; set; } = 10000;
    public string EncryptionKey { get; set; } = string.Empty;
    public bool EnableDataMasking { get; set; } = true;
    public bool EnablePseudonymization { get; set; } = true;
}

public class ConsentManagementSettings
{
    public bool EnableConsentTracking { get; set; } = true;
    public int ConsentExpirationDays { get; set; } = 365;
    public bool RequireExplicitConsent { get; set; } = true;
    public bool EnableConsentWithdrawal { get; set; } = true;
    public bool EnableConsentHistory { get; set; } = true;
    public string[] SupportedPurposes { get; set; } = Array.Empty<string>();
}

public class DataRetentionSettings
{
    public bool EnableAutomaticDeletion { get; set; } = true;
    public int DefaultRetentionDays { get; set; } = 2555; // 7 years default
    public Dictionary<string, int> CategoryRetentionPeriods { get; set; } = new();
    public bool EnableDataArchiving { get; set; } = true;
    public string ArchivePath { get; set; } = string.Empty;
    public bool NotifyBeforeDeletion { get; set; } = true;
}

public class GdprComplianceSettings
{
    public bool EnableGdprCompliance { get; set; } = true;
    public string DataControllerName { get; set; } = string.Empty;
    public string DataProtectionOfficerEmail { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = "Consent";
    public bool EnableRightOfAccess { get; set; } = true;
    public bool EnableRightOfRectification { get; set; } = true;
    public bool EnableRightOfErasure { get; set; } = true;
    public bool EnableDataPortability { get; set; } = true;
    public int ResponseTimeLimit { get; set; } = 30; // days
}

public class AuditLoggingSettings
{
    public bool EnableAuditLogging { get; set; } = true;
    public bool LogDataAccess { get; set; } = true;
    public bool LogDataModification { get; set; } = true;
    public bool LogConsentChanges { get; set; } = true;
    public bool LogDataDeletion { get; set; } = true;
    public string AuditLogPath { get; set; } = string.Empty;
    public int AuditLogRetentionDays { get; set; } = 2555; // 7 years
    public bool EnableStructuredLogging { get; set; } = true;
}