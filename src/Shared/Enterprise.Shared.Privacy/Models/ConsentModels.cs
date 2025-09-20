namespace Enterprise.Shared.Privacy.Models;

public class ConsentRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public ConsentPurpose Purpose { get; set; }
    public ConsentStatus Status { get; set; } = ConsentStatus.Pending;
    public LegalBasis LegalBasis { get; set; } = LegalBasis.Consent;
    public DateTime GrantedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Version { get; set; } = "1.0";
    public string Source { get; set; } = string.Empty; // Web, Mobile, API, etc.
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? WithdrawalReason { get; set; }
    public bool IsActive => Status == ConsentStatus.Granted && 
                           (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}

public class ConsentRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public ConsentPurpose[] Purposes { get; set; } = Array.Empty<ConsentPurpose>();
    public LegalBasis LegalBasis { get; set; } = LegalBasis.Consent;
    public string Source { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ConsentWithdrawalRequest
{
    public string UserId { get; set; } = string.Empty;
    public ConsentPurpose[] Purposes { get; set; } = Array.Empty<ConsentPurpose>();
    public string? Reason { get; set; }
    public bool DeleteAssociatedData { get; set; } = false;
}

public class ConsentSummary
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Dictionary<ConsentPurpose, ConsentStatus> ConsentsByPurpose { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public int TotalConsents { get; set; }
    public int ActiveConsents { get; set; }
    public int WithdrawnConsents { get; set; }
    public DateTime? NextExpiration { get; set; }
}

public class ConsentHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConsentRecordId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ConsentStatus PreviousStatus { get; set; }
    public ConsentStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty; // System, User, Admin
    public Dictionary<string, string> ChangeDetails { get; set; } = new();
}