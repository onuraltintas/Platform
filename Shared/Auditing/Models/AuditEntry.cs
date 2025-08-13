using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace EgitimPlatform.Shared.Auditing.Models;

public class AuditEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public AuditAction Action { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(200)]
    public string? UserName { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(200)]
    public string? Source { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? PropertyChanges { get; set; }
    
    [MaxLength(1000)]
    public string? Reason { get; set; }
    
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    [NotMapped]
    public Dictionary<string, object>? OldValuesObject
    {
        get => string.IsNullOrEmpty(OldValues) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(OldValues);
        set => OldValues = value == null ? null : JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public Dictionary<string, object>? NewValuesObject
    {
        get => string.IsNullOrEmpty(NewValues) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(NewValues);
        set => NewValues = value == null ? null : JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<PropertyChange>? PropertyChangesObject
    {
        get => string.IsNullOrEmpty(PropertyChanges) ? null : JsonSerializer.Deserialize<List<PropertyChange>>(PropertyChanges);
        set => PropertyChanges = value == null ? null : JsonSerializer.Serialize(value);
    }
}

public enum AuditAction
{
    Insert,
    Update,
    Delete,
    SoftDelete,
    Restore,
    Login,
    Logout,
    Access,
    Query,
    Execute,
    Export,
    Import,
    Custom
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string? DisplayName { get; set; }
    public bool IsSensitive { get; set; }
}

public class UserAuditEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? UserName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public UserAuditAction Action { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(100)]
    public string? DeviceId { get; set; }
    
    [MaxLength(50)]
    public string? DeviceType { get; set; }
    
    public bool Success { get; set; } = true;
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    public TimeSpan? Duration { get; set; }
}

public enum UserAuditAction
{
    Login,
    Logout,
    Register,
    PasswordChange,
    PasswordReset,
    EmailConfirmation,
    PhoneConfirmation,
    TwoFactorEnabled,
    TwoFactorDisabled,
    ProfileUpdate,
    AccountLocked,
    AccountUnlocked,
    RoleAssigned,
    RoleRemoved,
    PermissionGranted,
    PermissionRevoked
}

public class ApiAuditEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string Path { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Query { get; set; }
    
    public int StatusCode { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public TimeSpan Duration { get; set; }
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(200)]
    public string? UserName { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? ApiKey { get; set; }
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    
    public long? RequestSize { get; set; }
    public long? ResponseSize { get; set; }
}

public class PerformanceAuditEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Operation { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    
    [MaxLength(100)]
    public string? UserId { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public bool IsSuccessful { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    
    public long? MemoryUsed { get; set; }
    public double? CpuUsage { get; set; }
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}