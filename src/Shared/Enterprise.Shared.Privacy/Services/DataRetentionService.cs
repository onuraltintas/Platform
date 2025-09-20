using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Privacy.Services;

public class DataRetentionService : IDataRetentionService
{
    private readonly PrivacySettings _settings;
    private readonly ILogger<DataRetentionService> _logger;
    private readonly IPrivacyAuditService? _auditService;
    
    // In-memory storage for demo purposes
    private readonly List<PersonalDataRecord> _dataRecords = new();
    private readonly List<DataRetentionPolicy> _retentionPolicies = new();

    public DataRetentionService(
        IOptions<PrivacySettings> settings,
        ILogger<DataRetentionService> logger,
        IPrivacyAuditService? auditService = null)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService;
        
        InitializeDefaultPolicies();
    }

    public async Task<DataRetentionPolicy> CreateRetentionPolicyAsync(DataRetentionPolicy policy, CancellationToken cancellationToken = default)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        try
        {
            policy.Id = Guid.NewGuid().ToString();
            policy.CreatedAt = DateTime.UtcNow;
            policy.IsActive = true;

            _retentionPolicies.Add(policy);

            _logger.LogInformation("Data retention policy created: {PolicyName} for category {Category}",
                policy.Name, policy.DataCategory);

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create retention policy {PolicyName}", policy.Name);
            throw;
        }
    }

    public async Task<DataRetentionPolicy[]> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await GetRetentionPoliciesByCategoryAsync(null, cancellationToken);
    }

    public async Task<DataRetentionPolicy[]> GetRetentionPoliciesByCategoryAsync(DataCategory? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var policies = _retentionPolicies.Where(p => p.IsActive);
            
            if (category.HasValue)
                policies = policies.Where(p => p.DataCategory == category.Value);

            return policies.OrderBy(p => p.Name).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retention policies");
            throw;
        }
    }

    public async Task<DataRetentionPolicy> UpdateRetentionPolicyAsync(DataRetentionPolicy policy, CancellationToken cancellationToken = default)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        try
        {
            var existingPolicy = _retentionPolicies.FirstOrDefault(p => p.Id == policy.Id);
            if (existingPolicy == null)
                throw new ArgumentException($"Retention policy {policy.Id} not found", nameof(policy));

            existingPolicy.Name = policy.Name;
            existingPolicy.Description = policy.Description;
            existingPolicy.RetentionPeriodDays = policy.RetentionPeriodDays;
            existingPolicy.IsActive = policy.IsActive;
            existingPolicy.UpdatedAt = DateTime.UtcNow;
            existingPolicy.Rules = policy.Rules;

            _logger.LogInformation("Retention policy updated: {PolicyId}", policy.Id);
            return existingPolicy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update retention policy {PolicyId}", policy.Id);
            throw;
        }
    }

    public async Task<bool> DeleteRetentionPolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = _retentionPolicies.FirstOrDefault(p => p.Id == policyId);
            if (policy == null)
                return false;

            policy.IsActive = false;
            policy.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Retention policy deactivated: {PolicyId}", policyId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete retention policy {PolicyId}", policyId);
            throw;
        }
    }

    public async Task<PersonalDataRecord[]> GetExpiredDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredData = new List<PersonalDataRecord>();

            foreach (var record in _dataRecords)
            {
                if (record.RetentionExpiresAt.HasValue && record.RetentionExpiresAt <= now)
                {
                    expiredData.Add(record);
                }
                else if (!record.RetentionExpiresAt.HasValue)
                {
                    // Apply default retention policy
                    var policy = await GetApplicableRetentionPolicyAsync(record.Category, cancellationToken);
                    if (policy != null)
                    {
                        var expirationDate = record.CreatedAt.AddDays(policy.RetentionPeriodDays);
                        if (expirationDate <= now)
                        {
                            record.RetentionExpiresAt = expirationDate;
                            expiredData.Add(record);
                        }
                    }
                }
            }

            return expiredData.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expired data");
            throw;
        }
    }

    public async Task<DataRetentionPolicy?> GetRetentionPolicyAsync(DataCategory category, string? dataType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = _retentionPolicies
                .Where(p => p.DataCategory == category && p.IsActive)
                .FirstOrDefault();

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retention policy for category {Category}", category);
            throw;
        }
    }

    public async Task<int> ApplyRetentionPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await ProcessExpiredDataAsync(cancellationToken);
    }

    public async Task<PersonalDataRecord[]> GetDataForDeletionAsync(CancellationToken cancellationToken = default)
    {
        return await GetExpiredDataAsync(cancellationToken);
    }

    public async Task<PersonalDataRecord[]> GetDataForArchivalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var archivalDate = now.AddDays(-(_settings.DataRetention.DefaultRetentionDays / 2));
            
            return _dataRecords
                .Where(r => r.CreatedAt <= archivalDate && 
                           r.ProcessingStatus == ProcessingStatus.Active)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data for archival");
            throw;
        }
    }

    public async Task<int> ArchiveExpiredDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dataForArchival = await GetDataForArchivalAsync(cancellationToken);
            var archivedCount = 0;

            foreach (var record in dataForArchival)
            {
                record.ProcessingStatus = ProcessingStatus.Archived;
                record.Metadata["ArchivedAt"] = DateTime.UtcNow.ToString("O");
                archivedCount++;
            }

            _logger.LogInformation("Archived {Count} data records", archivedCount);
            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive expired data");
            throw;
        }
    }

    public async Task<int> DeleteExpiredDataAsync(bool forceDelete = false, CancellationToken cancellationToken = default)
    {
        return await ProcessExpiredDataAsync(cancellationToken);
    }

    public async Task<bool> ExtendRetentionPeriodAsync(string dataRecordId, int additionalDays, string reason, CancellationToken cancellationToken = default)
    {
        return await ExtendRetentionAsync(dataRecordId, additionalDays, reason, cancellationToken);
    }

    public async Task SetRetentionExpiryAsync(string userId, DataCategory category, DateTime expiryDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRecords = _dataRecords
                .Where(r => r.UserId == userId && r.Category == category)
                .ToArray();

            foreach (var record in userRecords)
            {
                record.RetentionExpiresAt = expiryDate;
            }

            _logger.LogInformation("Updated retention expiry for {Count} records", userRecords.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set retention expiry for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<DataCategory, DateTime?>> GetUserDataExpiryDatesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRecords = await GetUserDataRecordsAsync(userId, cancellationToken);
            
            return userRecords
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Min(r => r.RetentionExpiresAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user data expiry dates for {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetRetentionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await GetRetentionSummaryAsync(cancellationToken);
            
            return new Dictionary<string, object>
            {
                ["TotalRecords"] = _dataRecords.Count,
                ["TotalExpired"] = summary.Values.Sum(s => s.ExpiredRecords),
                ["TotalActive"] = summary.Values.Sum(s => s.ActiveRecords),
                ["CategoriesSummary"] = summary,
                ["LastProcessed"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retention statistics");
            throw;
        }
    }

    public async Task<bool> ValidateRetentionComplianceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allUsers = _dataRecords.Select(r => r.UserId).Distinct().ToArray();
            var complianceResults = new List<bool>();

            foreach (var userId in allUsers)
            {
                var result = await ValidateRetentionComplianceAsync(userId, cancellationToken);
                complianceResults.Add(result);
            }

            return complianceResults.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate retention compliance");
            return false;
        }
    }

    public async Task<int> ProcessExpiredDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredData = await GetExpiredDataAsync(cancellationToken);
            var deletedCount = 0;

            foreach (var record in expiredData)
            {
                try
                {
                    var policy = await GetApplicableRetentionPolicyAsync(record.Category, cancellationToken);
                    if (policy?.AutoDelete == true)
                    {
                        await DeleteDataRecordAsync(record.Id, cancellationToken);
                        deletedCount++;

                        if (_auditService != null)
                        {
                            await _auditService.LogDataDeletionAsync(record.UserId, 
                                new[] { record.Category }, 1, cancellationToken);
                        }
                    }
                    else
                    {
                        record.ProcessingStatus = ProcessingStatus.PendingDeletion;
                        
                        _logger.LogWarning("Data record {RecordId} expired but not auto-deleted due to policy", record.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process expired data record {RecordId}", record.Id);
                }
            }

            _logger.LogInformation("Processed {TotalExpired} expired records, deleted {Deleted}", 
                expiredData.Length, deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process expired data");
            throw;
        }
    }

    public async Task<PersonalDataRecord> AddDataRecordAsync(PersonalDataRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        try
        {
            record.Id = Guid.NewGuid().ToString();
            record.CreatedAt = DateTime.UtcNow;

            // Apply retention policy
            var policy = await GetApplicableRetentionPolicyAsync(record.Category);
            if (policy != null && !record.RetentionExpiresAt.HasValue)
            {
                record.RetentionExpiresAt = record.CreatedAt.AddDays(policy.RetentionPeriodDays);
            }
            else if (!record.RetentionExpiresAt.HasValue)
            {
                // Use default retention period
                record.RetentionExpiresAt = record.CreatedAt.AddDays(_settings.DataRetention.DefaultRetentionDays);
            }

            _dataRecords.Add(record);

            _logger.LogInformation("Data record added: {RecordId} for user {UserId}", 
                record.Id, record.UserId);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add data record");
            throw;
        }
    }

    public async Task<PersonalDataRecord?> GetDataRecordAsync(string recordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = _dataRecords.FirstOrDefault(r => r.Id == recordId);
            
            if (record != null)
            {
                record.LastAccessedAt = DateTime.UtcNow;
                
                if (_auditService != null)
                {
                    await _auditService.LogDataAccessAsync(record.UserId, recordId, 
                        record.Category, "DataRetentionService", cancellationToken);
                }
            }

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data record {RecordId}", recordId);
            throw;
        }
    }

    public async Task<PersonalDataRecord[]> GetUserDataRecordsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var records = _dataRecords
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToArray();

            if (records.Length > 0 && _auditService != null)
            {
                await _auditService.LogDataAccessAsync(userId, 
                    string.Join(",", records.Select(r => r.Id)),
                    DataCategory.Personal, "DataRetentionService", cancellationToken);
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data records for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteDataRecordAsync(string recordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = _dataRecords.FirstOrDefault(r => r.Id == recordId);
            if (record == null)
                return false;

            _dataRecords.Remove(record);

            _logger.LogInformation("Data record deleted: {RecordId}", recordId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete data record {RecordId}", recordId);
            throw;
        }
    }

    public async Task<int> DeleteUserDataAsync(string userId, DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRecords = _dataRecords.Where(r => r.UserId == userId);
            
            if (categories != null && categories.Length > 0)
            {
                userRecords = userRecords.Where(r => categories.Contains(r.Category));
            }

            var recordsToDelete = userRecords.ToArray();
            foreach (var record in recordsToDelete)
            {
                _dataRecords.Remove(record);
            }

            if (_auditService != null && recordsToDelete.Length > 0)
            {
                await _auditService.LogDataDeletionAsync(userId, 
                    categories ?? recordsToDelete.Select(r => r.Category).Distinct().ToArray(),
                    recordsToDelete.Length, cancellationToken);
            }

            _logger.LogInformation("Deleted {Count} data records for user {UserId}", 
                recordsToDelete.Length, userId);

            return recordsToDelete.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user data for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ExtendRetentionAsync(string recordId, int additionalDays, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = _dataRecords.FirstOrDefault(r => r.Id == recordId);
            if (record == null)
                return false;

            var oldExpiry = record.RetentionExpiresAt;
            record.RetentionExpiresAt = (record.RetentionExpiresAt ?? DateTime.UtcNow).AddDays(additionalDays);
            
            record.Metadata["RetentionExtended"] = DateTime.UtcNow.ToString("O");
            record.Metadata["ExtensionReason"] = reason;
            record.Metadata["AdditionalDays"] = additionalDays.ToString();

            _logger.LogInformation("Extended retention for record {RecordId} by {Days} days. Reason: {Reason}", 
                recordId, additionalDays, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend retention for record {RecordId}", recordId);
            throw;
        }
    }

    public async Task<Dictionary<DataCategory, RetentionSummary>> GetRetentionSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = new Dictionary<DataCategory, RetentionSummary>();
            var now = DateTime.UtcNow;

            var recordsByCategory = _dataRecords.GroupBy(r => r.Category);

            foreach (var group in recordsByCategory)
            {
                var records = group.ToArray();
                var expired = records.Count(r => r.RetentionExpiresAt <= now);
                var expiring = records.Count(r => r.RetentionExpiresAt.HasValue && 
                                                  r.RetentionExpiresAt <= now.AddDays(30) && 
                                                  r.RetentionExpiresAt > now);

                summary[group.Key] = new RetentionSummary
                {
                    Category = group.Key,
                    TotalRecords = records.Length,
                    ExpiredRecords = expired,
                    ExpiringIn30Days = expiring,
                    ActiveRecords = records.Length - expired,
                    AverageAge = records.Any() ? (int)records.Average(r => (now - r.CreatedAt).TotalDays) : 0,
                    OldestRecord = records.Any() ? records.Min(r => r.CreatedAt) : (DateTime?)null,
                    LastProcessed = DateTime.UtcNow
                };
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retention summary");
            throw;
        }
    }

    public async Task<bool> ValidateRetentionComplianceAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRecords = await GetUserDataRecordsAsync(userId, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var record in userRecords)
            {
                var policy = await GetApplicableRetentionPolicyAsync(record.Category);
                if (policy == null) continue;

                var expectedExpiry = record.CreatedAt.AddDays(policy.RetentionPeriodDays);
                
                // Check if record should have been deleted
                if (policy.AutoDelete && expectedExpiry <= now)
                {
                    _logger.LogWarning("Retention compliance violation: Record {RecordId} should have been deleted", 
                        record.Id);
                    return false;
                }

                // Check if expiry date is set correctly
                if (!record.RetentionExpiresAt.HasValue || 
                    Math.Abs((record.RetentionExpiresAt.Value - expectedExpiry).TotalDays) > 1)
                {
                    _logger.LogWarning("Retention compliance issue: Record {RecordId} has incorrect expiry date", 
                        record.Id);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate retention compliance for user {UserId}", userId);
            return false;
        }
    }

    private async Task<DataRetentionPolicy?> GetApplicableRetentionPolicyAsync(DataCategory category, CancellationToken cancellationToken = default)
    {
        var policies = await GetRetentionPoliciesByCategoryAsync(category, cancellationToken);
        return policies.FirstOrDefault(p => p.IsActive);
    }

    private void InitializeDefaultPolicies()
    {
        var defaultPolicies = new[]
        {
            new DataRetentionPolicy
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Personal Data Policy",
                Description = "Standard retention for personal data",
                DataCategory = DataCategory.Personal,
                RetentionPeriodDays = _settings.DataRetention.DefaultRetentionDays,
                AutoDelete = _settings.DataRetention.EnableAutomaticDeletion,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Rules = new Dictionary<string, string>
                {
                    ["ApplicableRegions"] = "EU,US,UK",
                    ["LegalBasis"] = "GDPR Article 5(1)(e)",
                    ["ReviewPeriod"] = "Annual"
                }
            },
            new DataRetentionPolicy
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Sensitive Data Policy",
                Description = "Shorter retention for sensitive data",
                DataCategory = DataCategory.Sensitive,
                RetentionPeriodDays = Math.Min(_settings.DataRetention.DefaultRetentionDays, 365),
                AutoDelete = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Rules = new Dictionary<string, string>
                {
                    ["ApplicableRegions"] = "EU,US,UK",
                    ["LegalBasis"] = "GDPR Article 9",
                    ["ReviewPeriod"] = "Quarterly"
                }
            },
            new DataRetentionPolicy
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Financial Data Policy",
                Description = "Extended retention for financial records",
                DataCategory = DataCategory.Financial,
                RetentionPeriodDays = 2555, // 7 years
                AutoDelete = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Rules = new Dictionary<string, string>
                {
                    ["ApplicableRegions"] = "US,EU",
                    ["LegalBasis"] = "Tax regulations",
                    ["ReviewPeriod"] = "Annual"
                }
            }
        };

        _retentionPolicies.AddRange(defaultPolicies);
    }
}

