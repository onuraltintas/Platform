namespace Enterprise.Shared.Email.Models;

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// File name of the attachment
    /// </summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type (MIME type) of the attachment
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// File content as byte array
    /// </summary>
    [Required]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Content ID for inline attachments
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// Indicates if this is an inline attachment
    /// </summary>
    public bool IsInline { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size => Content.Length;

    /// <summary>
    /// Creates an attachment from file content
    /// </summary>
    public static EmailAttachment FromBytes(string fileName, byte[] content, string? contentType = null)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType ?? GetContentType(fileName)
        };
    }

    /// <summary>
    /// Creates an attachment from a file path
    /// </summary>
    public static async Task<EmailAttachment> FromFileAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var content = await File.ReadAllBytesAsync(filePath);
        var contentType = GetContentType(fileName);

        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType
        };
    }

    /// <summary>
    /// Creates an inline attachment (for embedding images in HTML emails)
    /// </summary>
    public static EmailAttachment Inline(string fileName, byte[] content, string contentId, string? contentType = null)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType ?? GetContentType(fileName),
            ContentId = contentId,
            IsInline = true
        };
    }

    /// <summary>
    /// Gets the appropriate MIME type for a file extension
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Validates the attachment
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FileName))
        {
            errors.Add("File name is required");
        }

        if (Content == null || Content.Length == 0)
        {
            errors.Add("File content is required");
        }

        if (Content?.Length > 25 * 1024 * 1024) // 25MB limit
        {
            errors.Add("File size exceeds the maximum limit of 25MB");
        }

        return errors.Count == 0;
    }
}