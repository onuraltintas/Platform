namespace Identity.Core.Entities;

public class UserConsent
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    public ConsentType Type { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGranted { get; set; }
    public DateTime ConsentedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // GDPR
    public string LegalBasis { get; set; } = "Consent";
    public string? DataController { get; set; }
    public string? DataProcessor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Version Control
    public string Version { get; set; } = "1.0";
    public string? PreviousVersion { get; set; }
}

public enum ConsentType
{
    DataProcessing = 1,
    Marketing = 2,
    Cookies = 3,
    Analytics = 4,
    ThirdPartySharing = 5,
    Profiling = 6,
    Newsletter = 7,
    TermsOfService = 8,
    PrivacyPolicy = 9
}