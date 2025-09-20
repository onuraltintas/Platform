namespace Enterprise.Shared.Validation.Models;

/// <summary>
/// Base entity with integer ID and audit fields using Turkey timezone
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = GetTurkeyTime();
    public DateTime UpdatedAt { get; set; } = GetTurkeyTime();
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    private static DateTime GetTurkeyTime()
    {
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
    }

    public void UpdateAudit(string updatedBy)
    {
        UpdatedAt = GetTurkeyTime();
        UpdatedBy = updatedBy;
    }
}

/// <summary>
/// Base entity with GUID ID and audit fields using Turkey timezone
/// </summary>
public abstract class BaseGuidEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = GetTurkeyTime();
    public DateTime UpdatedAt { get; set; } = GetTurkeyTime();
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    private static DateTime GetTurkeyTime()
    {
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
    }

    public void UpdateAudit(string updatedBy)
    {
        UpdatedAt = GetTurkeyTime();
        UpdatedBy = updatedBy;
    }
}

/// <summary>
/// Base entity with soft delete capability and audit fields using Turkey timezone
/// </summary>
public abstract class SoftDeleteEntity : BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = GetTurkeyTime();
        DeletedBy = deletedBy;
        UpdateAudit(deletedBy);
    }

    public void Restore(string restoredBy)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdateAudit(restoredBy);
    }

    private static DateTime GetTurkeyTime()
    {
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
    }
}