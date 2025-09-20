using Microsoft.AspNetCore.Identity;

namespace Identity.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? About { get; set; }
    
    // Group/Tenant Support
    public Guid? DefaultGroupId { get; set; }
    public DateTime? LastGroupSwitch { get; set; }
    
    // Security
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string? LastLoginDevice { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? TwoFactorSecretKey { get; set; }
    
    // GDPR & Privacy
    public DateTime? ConsentGivenAt { get; set; }
    public DateTime? DataRetentionUntil { get; set; }
    public bool MarketingEmailsConsent { get; set; }
    public bool DataProcessingConsent { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation Properties
    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();
    public virtual ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
    public virtual ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();
}