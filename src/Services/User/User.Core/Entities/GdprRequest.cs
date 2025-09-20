using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// GDPR compliance requests (data export, deletion, etc.)
/// </summary>
public class GdprRequest : BaseEntity
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
    /// Type of GDPR request (Export, Delete, Rectify, Restrict)
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the request
    /// </summary>
    public OperationStatus Status { get; set; } = OperationStatus.Pending;

    /// <summary>
    /// Reason for the request
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the request was submitted
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the request was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Who processed the request
    /// </summary>
    public string? ProcessedBy { get; set; }

    /// <summary>
    /// Result data (file URL for exports, confirmation for deletions)
    /// </summary>
    public string? ResultData { get; set; }

    /// <summary>
    /// Verification token for security
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// Expiry date for the verification token
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }

    /// <summary>
    /// Additional notes from the processor
    /// </summary>
    public string? ProcessorNotes { get; set; }

    /// <summary>
    /// Is this request verified by the user
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// When the request was verified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Priority level of the request
    /// </summary>
    public Priority Priority { get; set; } = Priority.Normal;
}