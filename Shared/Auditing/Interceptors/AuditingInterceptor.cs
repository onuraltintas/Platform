using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using EgitimPlatform.Shared.Auditing.Configuration;
using EgitimPlatform.Shared.Auditing.Models;
using EgitimPlatform.Shared.Auditing.Services;
using EgitimPlatform.Shared.Auditing.Attributes;

namespace EgitimPlatform.Shared.Auditing.Interceptors;

public class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly AuditingOptions _options;
    private readonly IAuditService _auditService;
    private readonly IAuditContextProvider _contextProvider;
    private readonly ILogger<AuditingInterceptor> _logger;
    private readonly List<AuditEntry> _auditEntries = new();

    public AuditingInterceptor(
        IOptions<AuditingOptions> options,
        IAuditService auditService,
        IAuditContextProvider contextProvider,
        ILogger<AuditingInterceptor> logger)
    {
        _options = options.Value;
        _auditService = auditService;
        _contextProvider = contextProvider;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (_options.EnableAuditing && eventData.Context != null)
        {
            OnBeforeSaveChanges(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_options.EnableAuditing && eventData.Context != null)
        {
            OnBeforeSaveChanges(eventData.Context);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_options.EnableAuditing && _auditEntries.Any())
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditService.LogAuditsAsync(_auditEntries);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log audit entries after save changes");
                }
                finally
                {
                    _auditEntries.Clear();
                }
            });
        }
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_options.EnableAuditing && _auditEntries.Any())
        {
            try
            {
                await _auditService.LogAuditsAsync(_auditEntries, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entries after save changes");
            }
            finally
            {
                _auditEntries.Clear();
            }
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void OnBeforeSaveChanges(DbContext context)
    {
        try
        {
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .Where(e => ShouldAuditEntity(e.Entity))
                .ToList();

            foreach (var entry in entries)
            {
                var auditEntry = CreateAuditEntry(entry);
                if (auditEntry != null)
                {
                    _auditEntries.Add(auditEntry);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating audit entries");
        }
    }

    private bool ShouldAuditEntity(object entity)
    {
        var entityType = entity.GetType();

        // Check if entity type is excluded
        if (_options.ExcludedEntityTypes.Contains(entityType.Name))
            return false;

        // Check for NoAudit attribute
        if (entityType.GetCustomAttributes(typeof(NoAuditAttribute), true).Any())
            return false;

        // Check for Auditable attribute (if present, always audit)
        if (entityType.GetCustomAttributes(typeof(AuditableAttribute), true).Any())
            return true;

        // Default to audit if not explicitly excluded
        return true;
    }

    private AuditEntry? CreateAuditEntry(EntityEntry entry)
    {
        try
        {
            var entityType = entry.Entity.GetType();
            var primaryKey = GetPrimaryKeyValue(entry);

            if (primaryKey == null)
            {
                _logger.LogWarning("Could not determine primary key for entity type {EntityType}", entityType.Name);
                return null;
            }

            var auditEntry = new AuditEntry
            {
                EntityType = entityType.Name,
                EntityId = primaryKey.ToString()!,
                Action = GetAuditAction(entry.State),
                Timestamp = DateTime.UtcNow,
                Source = "EntityFramework"
            };

            // Add property changes
            var propertyChanges = new List<PropertyChange>();

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.NewValuesObject = GetEntityValues(entry, EntityState.Added);
                    break;

                case EntityState.Modified:
                    auditEntry.OldValuesObject = GetEntityValues(entry, EntityState.Modified, true);
                    auditEntry.NewValuesObject = GetEntityValues(entry, EntityState.Modified, false);
                    
                    if (_options.AuditOnlyModifiedProperties)
                    {
                        propertyChanges = GetModifiedProperties(entry);
                    }
                    break;

                case EntityState.Deleted:
                    auditEntry.OldValuesObject = GetEntityValues(entry, EntityState.Deleted);
                    
                    // Check if this is a soft delete
                    if (IsSoftDelete(entry))
                    {
                        auditEntry.Action = AuditAction.SoftDelete;
                    }
                    break;
            }

            if (propertyChanges.Any())
            {
                auditEntry.PropertyChangesObject = propertyChanges;
            }

            return auditEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit entry for entity {EntityType}", entry.Entity.GetType().Name);
            return null;
        }
    }

    private object? GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperties = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        
        if (keyProperties.Count == 1)
        {
            return keyProperties[0].CurrentValue;
        }
        
        if (keyProperties.Count > 1)
        {
            // Composite key - create a string representation
            return string.Join(",", keyProperties.Select(p => p.CurrentValue?.ToString()));
        }

        return null;
    }

    private static AuditAction GetAuditAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => AuditAction.Insert,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => AuditAction.Custom
        };
    }

    private Dictionary<string, object> GetEntityValues(EntityEntry entry, EntityState state, bool originalValues = false)
    {
        var values = new Dictionary<string, object>();

        foreach (var property in entry.Properties)
        {
            if (ShouldIncludeProperty(property.Metadata.Name))
            {
                var value = originalValues ? property.OriginalValue : property.CurrentValue;
                
                if (IsSensitiveProperty(property.Metadata.Name))
                {
                    values[property.Metadata.Name] = "***SENSITIVE***";
                }
                else if (value != null)
                {
                    values[property.Metadata.Name] = value;
                }
            }
        }

        return values;
    }

    private List<PropertyChange> GetModifiedProperties(EntityEntry entry)
    {
        var changes = new List<PropertyChange>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (ShouldIncludeProperty(property.Metadata.Name))
            {
                var change = new PropertyChange
                {
                    PropertyName = property.Metadata.Name,
                    DisplayName = GetPropertyDisplayName(property.Metadata.Name),
                    IsSensitive = IsSensitiveProperty(property.Metadata.Name)
                };

                if (change.IsSensitive)
                {
                    change.OldValue = "***SENSITIVE***";
                    change.NewValue = "***SENSITIVE***";
                }
                else
                {
                    change.OldValue = property.OriginalValue;
                    change.NewValue = property.CurrentValue;
                }

                changes.Add(change);
            }
        }

        return changes;
    }

    private bool ShouldIncludeProperty(string propertyName)
    {
        return !_options.ExcludedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsSensitiveProperty(string propertyName)
    {
        return _options.SensitiveProperties.Any(sp => 
            propertyName.Contains(sp, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetPropertyDisplayName(string propertyName)
    {
        // Convert PascalCase to readable format
        return System.Text.RegularExpressions.Regex.Replace(propertyName, "([a-z])([A-Z])", "$1 $2");
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        // Check for common soft delete properties
        return entry.Properties.Any(p => 
            (p.Metadata.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) ||
             p.Metadata.Name.Equals("Deleted", StringComparison.OrdinalIgnoreCase) ||
             p.Metadata.Name.Equals("DeletedAt", StringComparison.OrdinalIgnoreCase)) &&
            p.IsModified);
    }
}