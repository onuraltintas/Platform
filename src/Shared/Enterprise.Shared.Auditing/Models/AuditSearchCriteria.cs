namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Criteria for searching audit events
/// </summary>
public class AuditSearchCriteria
{
    /// <summary>
    /// Start date for the search range
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the search range
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// User ID to filter by
    /// </summary>
    [StringLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Username to filter by
    /// </summary>
    [StringLength(256)]
    public string? Username { get; set; }

    /// <summary>
    /// Action to filter by
    /// </summary>
    [StringLength(100)]
    public string? Action { get; set; }

    /// <summary>
    /// Resource to filter by
    /// </summary>
    [StringLength(100)]
    public string? Resource { get; set; }

    /// <summary>
    /// Resource ID to filter by
    /// </summary>
    [StringLength(100)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Result to filter by
    /// </summary>
    [StringLength(50)]
    public string? Result { get; set; }

    /// <summary>
    /// Service name to filter by
    /// </summary>
    [StringLength(100)]
    public string? ServiceName { get; set; }

    /// <summary>
    /// Correlation ID to filter by
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// IP address to filter by
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Category to filter by
    /// </summary>
    public AuditEventCategory? Category { get; set; }

    /// <summary>
    /// Minimum severity level
    /// </summary>
    public AuditSeverity? MinSeverity { get; set; }

    /// <summary>
    /// Tags to filter by (all must match)
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Free text search in details and metadata
    /// </summary>
    [StringLength(500)]
    public string? SearchText { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [Range(1, 1000)]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    [StringLength(50)]
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Whether to include detailed metadata in results
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Environment to filter by
    /// </summary>
    [StringLength(50)]
    public string? Environment { get; set; }

    /// <summary>
    /// Validates the search criteria
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            errors.Add("Start date cannot be later than end date");
        }

        if (EndDate.HasValue && EndDate > DateTime.UtcNow.AddDays(1))
        {
            errors.Add("End date cannot be in the future");
        }

        if (PageSize > 1000)
        {
            errors.Add("Page size cannot exceed 1000");
        }

        if (!string.IsNullOrEmpty(SearchText) && SearchText.Length > 500)
        {
            errors.Add("Search text cannot exceed 500 characters");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(string.Join("; ", errors));
    }

    /// <summary>
    /// Gets the skip count for pagination
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the take count for pagination
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Creates criteria for a specific user
    /// </summary>
    public static AuditSearchCriteria ForUser(string userId, int days = 30)
    {
        return new AuditSearchCriteria
        {
            UserId = userId,
            StartDate = DateTime.UtcNow.AddDays(-days),
            EndDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates criteria for security events
    /// </summary>
    public static AuditSearchCriteria ForSecurity(int days = 7)
    {
        return new AuditSearchCriteria
        {
            Category = AuditEventCategory.Security,
            StartDate = DateTime.UtcNow.AddDays(-days),
            EndDate = DateTime.UtcNow,
            MinSeverity = AuditSeverity.Warning
        };
    }

    /// <summary>
    /// Creates criteria for failed operations
    /// </summary>
    public static AuditSearchCriteria ForFailures(int hours = 24)
    {
        return new AuditSearchCriteria
        {
            Result = "Failed",
            StartDate = DateTime.UtcNow.AddHours(-hours),
            EndDate = DateTime.UtcNow,
            MinSeverity = AuditSeverity.Warning
        };
    }

    /// <summary>
    /// Creates criteria for a specific correlation ID
    /// </summary>
    public static AuditSearchCriteria ForCorrelation(string correlationId)
    {
        return new AuditSearchCriteria
        {
            CorrelationId = correlationId,
            PageSize = 1000 // Get all events for correlation
        };
    }
}

/// <summary>
/// Sort direction for audit search results
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending order
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Descending order
    /// </summary>
    Descending = 1
}

/// <summary>
/// Result of an audit search operation
/// </summary>
public class AuditSearchResult
{
    /// <summary>
    /// The search criteria used
    /// </summary>
    public AuditSearchCriteria Criteria { get; set; } = new();

    /// <summary>
    /// The audit events found
    /// </summary>
    public List<AuditEvent> Events { get; set; } = new();

    /// <summary>
    /// Total number of events matching the criteria
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Criteria.PageSize);

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage => Criteria.PageNumber < TotalPages;

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage => Criteria.PageNumber > 1;

    /// <summary>
    /// The time taken to execute the search
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Creates a successful search result
    /// </summary>
    public static AuditSearchResult Success(
        AuditSearchCriteria criteria,
        List<AuditEvent> events,
        int totalCount,
        TimeSpan executionTime)
    {
        return new AuditSearchResult
        {
            Criteria = criteria,
            Events = events,
            TotalCount = totalCount,
            ExecutionTime = executionTime
        };
    }
}