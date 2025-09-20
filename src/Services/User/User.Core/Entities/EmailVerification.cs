using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// Email verification tokens and status
/// </summary>
public class EmailVerification : BaseEntity
{
    /// <summary>
    /// User ID reference
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to user profile
    /// </summary>
    public virtual UserProfile UserProfile { get; set; } = null!;

    /// <summary>
    /// Email address being verified
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Verification token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiry date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Is this verification completed
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// When the email was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// IP address from which verification was requested
    /// </summary>
    public string? RequestIpAddress { get; set; }

    /// <summary>
    /// IP address from which verification was completed
    /// </summary>
    public string? VerificationIpAddress { get; set; }

    /// <summary>
    /// Number of verification attempts
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Last attempt date
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Verification type (Registration, EmailChange, Recovery)
    /// </summary>
    public string VerificationType { get; set; } = "Registration";

    /// <summary>
    /// Is this token used
    /// </summary>
    public bool IsUsed { get; set; }
}