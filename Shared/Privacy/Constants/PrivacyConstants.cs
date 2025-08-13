namespace EgitimPlatform.Shared.Privacy.Constants;

public static class PrivacyConstants
{
    public static class Headers
    {
        public const string PrivacyPolicy = "X-Privacy-Policy";
        public const string DataController = "X-Data-Controller";
        public const string DpoContact = "X-DPO-Contact";
        public const string PrivacyRegulations = "X-Privacy-Regulations";
        public const string RequiresCookieConsent = "X-Requires-Cookie-Consent";
        public const string CookieConsentStatus = "X-Cookie-Consent-Status";
        public const string CookieConsentExpired = "X-Cookie-Consent-Expired";
        public const string CookieConsentInvalid = "X-Cookie-Consent-Invalid";
        public const string CookiePolicy = "X-Cookie-Policy";
    }

    public static class Cookies 
    {
        public const string PrivacyConsent = "privacy-consent";
        public const string EssentialCookies = "essential-cookies";
        public const string AnalyticalCookies = "analytical-cookies";
        public const string MarketingCookies = "marketing-cookies";
    }

    public static class ConsentPurposes
    {
        public const string ProfileManagement = "profile-management";
        public const string Communications = "communications";
        public const string Analytics = "analytics";
        public const string Marketing = "marketing";
        public const string ServiceDelivery = "service-delivery";
        public const string LegalCompliance = "legal-compliance";
        public const string SecurityMonitoring = "security-monitoring";
        public const string CustomerSupport = "customer-support";
    }

    public static class DataRetentionPeriods
    {
        public const int DefaultDays = 365;
        public const int ProfileDataDays = 2555; // 7 years
        public const int FinancialDataDays = 2555; // 7 years
        public const int HealthDataDays = 3650; // 10 years
        public const int LogDataDays = 90;
        public const int SessionDataDays = 30;
        public const int MarketingDataDays = 730; // 2 years
        public const int AnalyticsDataDays = 365;
    }

    public static class ProcessingActivities
    {
        public const string UserRegistration = "user-registration";
        public const string ProfileManagement = "profile-management";
        public const string CommunicationDelivery = "communication-delivery";
        public const string PaymentProcessing = "payment-processing";
        public const string CustomerSupport = "customer-support";
        public const string SecurityMonitoring = "security-monitoring";
        public const string AnalyticsProcessing = "analytics-processing";
        public const string MarketingCampaigns = "marketing-campaigns";
    }

    public static class Regulations
    {
        public const string GDPR = "GDPR";
        public const string CCPA = "CCPA";
        public const string PIPEDA = "PIPEDA";
        public const string LGPD = "LGPD";
        public const string PDPA = "PDPA";
    }

    public static class ErrorMessages
    {
        public const string ConsentRequired = "Valid consent is required for this data processing activity";
        public const string ConsentWithdrawn = "Consent has been withdrawn for this data processing activity";
        public const string ConsentExpired = "Consent has expired and needs to be renewed";
        public const string InsufficientConsent = "The provided consent is insufficient for this data processing activity";
        public const string RetentionPeriodExceeded = "Data retention period has been exceeded";
        public const string SensitiveDataRequiresExplicitConsent = "Processing sensitive personal data requires explicit consent";
        public const string EncryptionRequired = "This data must be encrypted";
        public const string PseudonymizationRequired = "This data must be pseudonymized";
        public const string ThirdCountryTransferRestricted = "Transfer to third country requires adequate safeguards";
        public const string RequestLimitExceeded = "Maximum number of requests exceeded for this time period";
        public const string InvalidDataSubjectRequest = "Invalid data subject rights request";
    }

    public static class AuditEventTypes
    {
        public const string ConsentGiven = "consent-given";
        public const string ConsentWithdrawn = "consent-withdrawn";
        public const string ConsentExpired = "consent-expired";
        public const string DataSubjectRequestCreated = "data-subject-request-created";
        public const string DataSubjectRequestProcessed = "data-subject-request-processed";
        public const string PersonalDataAccessed = "personal-data-accessed";
        public const string PersonalDataModified = "personal-data-modified";
        public const string PersonalDataDeleted = "personal-data-deleted";
        public const string PersonalDataExported = "personal-data-exported";
        public const string DataRetentionProcessed = "data-retention-processed";
        public const string ThirdCountryDataTransfer = "third-country-data-transfer";
        public const string PrivacyViolationDetected = "privacy-violation-detected";
    }

    public static class ConfigurationKeys
    {
        public const string ConsentExpiryDays = "Privacy:ConsentManagement:ConsentExpiryDays";
        public const string RequestTimeoutDays = "Privacy:DataSubjectRights:RequestProcessingTimeoutDays";
        public const string MaxRequestsPerMonth = "Privacy:DataSubjectRights:MaxRequestsPerUserPerMonth";
        public const string EnableAutoDataDeletion = "Privacy:DataRetention:EnableAutoDataDeletion";
        public const string DataControllerName = "Privacy:Compliance:DataControllerName";
        public const string DataControllerEmail = "Privacy:Compliance:DataControllerEmail";
        public const string DpoEmail = "Privacy:Compliance:DataProtectionOfficerEmail";
        public const string PrivacyPolicyUrl = "Privacy:Compliance:PrivacyPolicyUrl";
    }
}