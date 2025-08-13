namespace EgitimPlatform.Shared.Privacy.Enums;

public enum DataProcessingLawfulBasis
{
    Consent = 1,
    Contract = 2,
    LegalObligation = 3,
    VitalInterests = 4,
    PublicTask = 5,
    LegitimateInterests = 6
}

public enum ConsentStatus
{
    NotGiven = 0,
    Given = 1,
    Withdrawn = 2,
    Expired = 3
}

public enum DataSubjectRightType
{
    Access = 1,
    Rectification = 2,
    Erasure = 3,
    RestrictProcessing = 4,
    DataPortability = 5,
    Object = 6,
    NotToBeSubjectToAutomatedDecisionMaking = 7
}

public enum DataSubjectRequestStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Rejected = 4,
    PartiallyCompleted = 5
}

public enum PersonalDataCategory
{
    BasicIdentity = 1,
    ContactInformation = 2,
    FinancialInformation = 3,
    HealthInformation = 4,
    BiometricData = 5,
    LocationData = 6,
    OnlineIdentifiers = 7,
    BehavioralData = 8,
    SensitivePersonalData = 9
}

public enum DataRetentionReason
{
    LegalObligation = 1,
    OngoingContract = 2,
    ConsentGiven = 3,
    LegitimateInterest = 4,
    UserRequest = 5
}