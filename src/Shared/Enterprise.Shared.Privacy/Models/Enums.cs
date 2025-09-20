namespace Enterprise.Shared.Privacy.Models;

public enum ConsentStatus
{
    Pending,
    Granted,
    Withdrawn,
    Expired,
    Invalid
}

public enum ConsentPurpose
{
    Marketing,
    Analytics,
    Essential,
    Functional,
    Performance,
    Advertising,
    Personalization,
    Research,
    Legal,
    Security
}

public enum DataCategory
{
    Personal,
    Sensitive,
    Financial,
    Health,
    Biometric,
    Location,
    Behavioral,
    Technical,
    Communication,
    Preference
}

public enum AnonymizationLevel
{
    None,
    Masked,
    Hashed,
    Encrypted,
    Pseudonymized,
    Anonymized,
    Deleted
}

public enum DataSubjectRight
{
    Access,
    Rectification,
    Erasure,
    DataPortability,
    Restriction,
    Objection,
    Withdraw
}

public enum AuditEventType
{
    DataAccess,
    DataCreated,
    DataModified,
    DataDeleted,
    ConsentGranted,
    ConsentWithdrawn,
    ConsentExpired,
    DataExported,
    DataAnonymized,
    UserRightExercised,
    PolicyViolation,
    SecurityBreach
}

public enum LegalBasis
{
    NotSpecified,
    Consent,
    Contract,
    LegalObligation,
    VitalInterests,
    PublicTask,
    LegitimateInterests
}

public enum ProcessingStatus
{
    Pending,
    InProgress,
    Active,
    Suspended,
    Completed,
    Failed,
    Terminated,
    UnderReview,
    Restricted,
    PendingDeletion,
    Archived
}