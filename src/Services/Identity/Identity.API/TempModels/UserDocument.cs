using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserDocument
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string FileExtension { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public int FileType { get; set; }

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public string BucketName { get; set; } = null!;

    public string ObjectKey { get; set; } = null!;

    public string? PublicUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public bool IsActive { get; set; }

    public string? Checksum { get; set; }

    public string? VirusScanStatus { get; set; }

    public string? VirusScanResult { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int DownloadCount { get; set; }

    public DateTime? LastDownloadedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
