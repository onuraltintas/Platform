namespace User.Core.DTOs;

/// <summary>
/// User document data transfer object
/// </summary>
public class UserDocumentDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Document type (ProfilePicture, IdentityDocument, etc.)
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File URL in storage
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}