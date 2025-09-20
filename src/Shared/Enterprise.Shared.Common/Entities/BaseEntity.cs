using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Common.Entities;

/// <summary>
/// Base entity class with integer primary key and audit fields
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the last update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the user who created this entity
    /// </summary>
    [MaxLength(256)]
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the user who last updated this entity
    /// </summary>
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base entity class with GUID primary key and audit fields
/// </summary>
public abstract class BaseGuidEntity
{
    /// <summary>
    /// Gets or sets the unique identifier
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the last update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the user who created this entity
    /// </summary>
    [MaxLength(256)]
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the user who last updated this entity
    /// </summary>
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Auditable entity interface for tracking changes
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Soft delete interface for entities that support soft deletion
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
    
    void SoftDelete(string deletedBy);
    void Restore();
}

/// <summary>
/// Base entity class with soft delete functionality
/// </summary>
public abstract class SoftDeleteEntity : BaseEntity, ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Gets or sets the deletion timestamp (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the user who deleted this entity
    /// </summary>
    [MaxLength(256)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Soft delete this entity
    /// </summary>
    /// <param name="deletedBy">The user performing the deletion</param>
    public virtual void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restore this entity from soft deletion
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}