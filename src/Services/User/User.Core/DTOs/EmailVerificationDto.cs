namespace User.Core.DTOs;

/// <summary>
/// Email verification data transfer object
/// </summary>
public class EmailVerificationDto
{
    /// <summary>
    /// Verification ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email address being verified
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Verification token
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether email is verified
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Verification attempt count
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Last verification attempt date
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Verification completion date
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Token expiry date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}