using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// User activity tracking for audit purposes
/// </summary>
public class UserActivity : BaseEntity
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
    /// Activity type (Login, ProfileUpdate, EmailChange, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the activity
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// IP address from which the activity was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device information
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Location information (city, country)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Success status of the activity
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the activity failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Session ID if available
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Risk score for security analysis (0-100)
    /// </summary>
    public int RiskScore { get; set; } = 0;
}