namespace Enterprise.Shared.Privacy.Models;

public class PersonalDataRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DataCategory Category { get; set; }
    public string DataType { get; set; } = string.Empty; // Email, Phone, Address, etc.
    public string OriginalValue { get; set; } = string.Empty;
    public string? ProcessedValue { get; set; }
    public AnonymizationLevel AnonymizationLevel { get; set; } = AnonymizationLevel.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public DateTime? RetentionExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Active;
}

public class DataRetentionPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DataCategory DataCategory { get; set; }
    public string? DataType { get; set; }
    public int RetentionPeriodDays { get; set; }
    public bool AutoDelete { get; set; } = true;
    public bool ArchiveBeforeDelete { get; set; } = false;
    public LegalBasis LegalBasis { get; set; } = LegalBasis.LegitimateInterests;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, string> Rules { get; set; } = new();
}

public class DataExportRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DataCategory[] Categories { get; set; } = Array.Empty<DataCategory>();
    public DataCategory[] RequestedCategories { get; set; } = Array.Empty<DataCategory>();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public string? ExportFilePath { get; set; }
    public byte[]? ExportData { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CompletionNotes { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> ExportSummary { get; set; } = new();
}

public class DataDeletionRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DataSubjectRight RequestType { get; set; } = DataSubjectRight.Erasure;
    public DataCategory[] Categories { get; set; } = Array.Empty<DataCategory>();
    public DataCategory[] RequestedCategories { get; set; } = Array.Empty<DataCategory>();
    public string[] SpecificDataTypes { get; set; } = Array.Empty<string>();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public bool ArchiveBeforeDelete { get; set; } = true;
    public string? Reason { get; set; }
    public int DeletedRecordsCount { get; set; }
    public string? CompletionNotes { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> DeletionSummary { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class DataProcessingRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string DataRecordId { get; set; } = string.Empty;
    public string ProcessingActivity { get; set; } = string.Empty;
    public ConsentPurpose Purpose { get; set; }
    public LegalBasis LegalBasis { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string ProcessedBy { get; set; } = string.Empty; // System, User, Process
    public Dictionary<string, string> ProcessingDetails { get; set; } = new();
    public bool ConsentRequired { get; set; } = true;
    public bool ConsentObtained { get; set; } = false;
}

public class RetentionSummary
{
    public DataCategory Category { get; set; }
    public int TotalRecords { get; set; }
    public int ExpiredRecords { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ActiveRecords { get; set; }
    public int AverageAge { get; set; }
    public DateTime? OldestRecord { get; set; }
    public DateTime LastProcessed { get; set; }
}