using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// User uploaded documents and files
/// </summary>
public class UserDocument : BaseEntity
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
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored file name (usually a GUID)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File type category
    /// </summary>
    public FileType FileType { get; set; } = FileType.Unknown;

    /// <summary>
    /// Document category (ProfilePicture, Document, Avatar, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description or notes about the document
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Storage bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Storage object key/path
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// Public URL if accessible
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// Thumbnail URL for images
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Is this file currently active/visible
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// File checksum for integrity verification
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Virus scan status
    /// </summary>
    public string? VirusScanStatus { get; set; }

    /// <summary>
    /// Virus scan result
    /// </summary>
    public string? VirusScanResult { get; set; }

    /// <summary>
    /// When the file expires (for temporary files)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Download count
    /// </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// Last downloaded date
    /// </summary>
    public DateTime? LastDownloadedAt { get; set; }
}