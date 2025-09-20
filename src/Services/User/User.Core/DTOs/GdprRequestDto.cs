namespace User.Core.DTOs;

/// <summary>
/// GDPR request data transfer object
/// </summary>
public class GdprRequestDto
{
    /// <summary>
    /// Request ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Request type (DataExport, DataDeletion, DataCorrection)
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Request status (Pending, InProgress, Completed, Failed)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Request description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data export URL if applicable
    /// </summary>
    public string? DataExportUrl { get; set; }

    /// <summary>
    /// Processing notes
    /// </summary>
    public string? ProcessingNotes { get; set; }

    /// <summary>
    /// Request date
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Processing completion date
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Data retention expiry date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}