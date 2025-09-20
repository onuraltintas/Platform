namespace Enterprise.Shared.Configuration.Models;

/// <summary>
/// Record of configuration changes for auditing
/// </summary>
public class ConfigurationChangeRecord
{
    /// <summary>
    /// Unique identifier for the change record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Configuration key that changed
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (if any)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// User or system that made the change
    /// </summary>
    [Required]
    public string ChangedBy { get; set; } = "System";

    /// <summary>
    /// Timestamp when the change occurred (UTC)
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional reason for the change
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Environment where the change occurred
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Source of the change (UI, API, System, etc.)
    /// </summary>
    public string? Source { get; set; }
}

/// <summary>
/// Event arguments for configuration changes
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Configuration key that changed
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (if any)
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Timestamp when the change occurred (UTC)
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User or system that made the change
    /// </summary>
    public string? ChangedBy { get; set; }
}

/// <summary>
/// Feature flag evaluation result
/// </summary>
public class FeatureFlagResult
{
    /// <summary>
    /// Feature flag name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// User ID for context-specific evaluation
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Evaluation timestamp
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source of the flag value (Configuration, Database, Cache, etc.)
    /// </summary>
    public string Source { get; set; } = "Configuration";

    /// <summary>
    /// Additional metadata about the evaluation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Configuration section that was validated
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Timestamp of validation
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ConfigurationValidationResult Success(string? sectionName = null) => 
        new() { IsValid = true, SectionName = sectionName };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ConfigurationValidationResult Failure(IEnumerable<string> errors, string? sectionName = null) => 
        new() { IsValid = false, Errors = errors.ToList(), SectionName = sectionName };

    /// <summary>
    /// Creates a validation result with warnings
    /// </summary>
    public static ConfigurationValidationResult WithWarnings(IEnumerable<string> warnings, string? sectionName = null) => 
        new() { IsValid = true, Warnings = warnings.ToList(), SectionName = sectionName };
}