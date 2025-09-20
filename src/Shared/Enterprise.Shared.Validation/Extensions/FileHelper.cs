namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// File utility helper for Turkish applications
/// </summary>
public static class FileHelper
{
    private static readonly Dictionary<string, List<string>> MimeTypeMap = new()
    {
        { "image", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico" } },
        { "document", new List<string> { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt" } },
        { "spreadsheet", new List<string> { ".xls", ".xlsx", ".csv", ".ods" } },
        { "presentation", new List<string> { ".ppt", ".pptx", ".odp" } },
        { "video", new List<string> { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" } },
        { "audio", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma" } },
        { "archive", new List<string> { ".zip", ".rar", ".7z", ".tar", ".gz" } },
        { "code", new List<string> { ".js", ".css", ".html", ".xml", ".json", ".cs", ".py", ".java" } }
    };

    /// <summary>
    /// Gets file category based on extension
    /// </summary>
    public static string GetFileCategory(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "unknown";
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return MimeTypeMap.FirstOrDefault(kvp => kvp.Value.Contains(extension)).Key ?? "unknown";
    }

    /// <summary>
    /// Checks if file is an image
    /// </summary>
    public static bool IsImageFile(string fileName)
    {
        return GetFileCategory(fileName) == "image";
    }

    /// <summary>
    /// Checks if file is a document
    /// </summary>
    public static bool IsDocumentFile(string fileName)
    {
        return GetFileCategory(fileName) == "document";
    }

    /// <summary>
    /// Checks if file is a video
    /// </summary>
    public static bool IsVideoFile(string fileName)
    {
        return GetFileCategory(fileName) == "video";
    }

    /// <summary>
    /// Checks if file is an audio file
    /// </summary>
    public static bool IsAudioFile(string fileName)
    {
        return GetFileCategory(fileName) == "audio";
    }

    /// <summary>
    /// Generates unique file name with Turkish timestamp
    /// </summary>
    public static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName).RemoveSpecialCharacters();
        var turkeyTime = DateTimeExtensions.GetTurkeyNow();
        var timestamp = turkeyTime.ToString("yyyyMMdd-HHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        
        return $"{nameWithoutExtension}_{timestamp}_{guid}{extension}";
    }

    /// <summary>
    /// Gets content type for file extension
    /// </summary>
    public static string GetContentType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "application/octet-stream";
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain; charset=utf-8",
            ".rtf" => "application/rtf",
            ".csv" => "text/csv; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".ogg" => "audio/ogg",
            ".wma" => "audio/x-ms-wma",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Validates file name for Turkish characters and invalid characters
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        
        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c)) && fileName.Length <= 255;
    }

    /// <summary>
    /// Sanitizes file name for Turkish compatibility
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "dosya";
        
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        
        // Replace Turkish characters with safe equivalents for file systems
        sanitized = sanitized.Replace("ç", "c").Replace("Ç", "C")
                            .Replace("ğ", "g").Replace("Ğ", "G")
                            .Replace("ı", "i").Replace("İ", "I")
                            .Replace("ö", "o").Replace("Ö", "O")
                            .Replace("ş", "s").Replace("Ş", "S")
                            .Replace("ü", "u").Replace("Ü", "U");
        
        // Remove multiple spaces and trim
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ").Trim();
        
        return string.IsNullOrWhiteSpace(sanitized) ? "dosya" : sanitized;
    }

    /// <summary>
    /// Gets file size in human readable format (Turkish)
    /// </summary>
    public static string GetHumanReadableSize(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;
        const long tb = gb * 1024;

        return bytes switch
        {
            >= tb => $"{bytes / (double)tb:F2} TB",
            >= gb => $"{bytes / (double)gb:F2} GB",
            >= mb => $"{bytes / (double)mb:F2} MB",
            >= kb => $"{bytes / (double)kb:F2} KB",
            _ => $"{bytes} Bayt"
        };
    }

    /// <summary>
    /// Validates file extension against allowed list
    /// </summary>
    public static bool IsAllowedExtension(string fileName, string[] allowedExtensions)
    {
        if (string.IsNullOrEmpty(fileName) || allowedExtensions == null || allowedExtensions.Length == 0)
            return false;
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Any(allowed => 
            string.Equals(extension, allowed.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets allowed extensions for a category
    /// </summary>
    public static string[] GetAllowedExtensionsForCategory(string category)
    {
        var categoryLower = category.ToLowerInvariant();
        
        if (MimeTypeMap.TryGetValue(categoryLower, out var extensions))
        {
            return extensions.ToArray();
        }

        return categoryLower switch
        {
            "images" => MimeTypeMap["image"].ToArray(),
            "documents" => MimeTypeMap["document"].ToArray(),
            "videos" => MimeTypeMap["video"].ToArray(),
            "audios" => MimeTypeMap["audio"].ToArray(),
            "archives" => MimeTypeMap["archive"].ToArray(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Checks if file size is within limits for category
    /// </summary>
    public static bool IsFileSizeValid(long fileSizeBytes, string category, int? customLimitMB = null)
    {
        var sizeMB = fileSizeBytes / (1024.0 * 1024.0);
        
        if (customLimitMB.HasValue)
        {
            return sizeMB <= customLimitMB.Value;
        }

        return category.ToLowerInvariant() switch
        {
            "image" => sizeMB <= Models.CommonConstants.FileValidation.MaxImageSizeMB,
            "document" => sizeMB <= Models.CommonConstants.FileValidation.MaxDocumentSizeMB,
            "video" => sizeMB <= Models.CommonConstants.FileValidation.MaxVideoSizeMB,
            "audio" => sizeMB <= Models.CommonConstants.FileValidation.MaxAudioSizeMB,
            _ => sizeMB <= 10 // Default 10MB for other files
        };
    }

    /// <summary>
    /// Creates a file info object with Turkish metadata
    /// </summary>
    public static FileMetadata GetFileMetadata(string fileName, long fileSizeBytes, string? contentType = null)
    {
        return new FileMetadata
        {
            FileName = fileName,
            SanitizedFileName = SanitizeFileName(fileName),
            Extension = Path.GetExtension(fileName).ToLowerInvariant(),
            SizeBytes = fileSizeBytes,
            SizeFormatted = GetHumanReadableSize(fileSizeBytes),
            ContentType = contentType ?? GetContentType(fileName),
            Category = GetFileCategory(fileName),
            IsValid = IsValidFileName(fileName),
            CreatedAt = DateTimeExtensions.GetTurkeyNow()
        };
    }
}

/// <summary>
/// File metadata model
/// </summary>
public class FileMetadata
{
    public string FileName { get; set; } = string.Empty;
    public string SanitizedFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
}