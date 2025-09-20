using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// User profile entity containing all user information
/// </summary>
public class UserProfile : BaseEntity
{
    /// <summary>
    /// User ID from Identity Service (same as Identity.Users.Id)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name computed property
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's bio/about information
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// User's timezone
    /// </summary>
    public Enterprise.Shared.Common.Enums.TimeZone TimeZone { get; set; } = Enterprise.Shared.Common.Enums.TimeZone.Utc;

    /// <summary>
    /// User's preferred language
    /// </summary>
    public LanguageCode Language { get; set; } = LanguageCode.En;

    /// <summary>
    /// User preferences
    /// </summary>
    public virtual UserPreferences? Preferences { get; set; }

    /// <summary>
    /// User addresses
    /// </summary>
    public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

    /// <summary>
    /// User activities
    /// </summary>
    public virtual ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();

    /// <summary>
    /// User documents
    /// </summary>
    public virtual ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();

    /// <summary>
    /// GDPR requests
    /// </summary>
    public virtual ICollection<GdprRequest> GdprRequests { get; set; } = new List<GdprRequest>();

    /// <summary>
    /// Email verifications
    /// </summary>
    public virtual ICollection<EmailVerification> EmailVerifications { get; set; } = new List<EmailVerification>();
}