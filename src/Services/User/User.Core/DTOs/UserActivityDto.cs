namespace User.Core.DTOs;

/// <summary>
/// User activity data transfer object
/// </summary>
public class UserActivityDto
{
    /// <summary>
    /// Activity ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

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

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}