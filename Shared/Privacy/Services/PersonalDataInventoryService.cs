using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public class PersonalDataInventoryService : IPersonalDataInventoryService
{
    private readonly ILogger<PersonalDataInventoryService> _logger;
    private readonly PrivacyOptions _options;
    private readonly List<PersonalDataInventory> _inventory; // In-memory storage for demo

    public PersonalDataInventoryService(ILogger<PersonalDataInventoryService> logger, IOptions<PrivacyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _inventory = new List<PersonalDataInventory>();
    }

    public async Task<PersonalDataInventory> RecordPersonalDataAsync(string userId, PersonalDataCategory dataCategory,
        string dataField, string storageLocation, DataProcessingLawfulBasis lawfulBasis,
        string processingPurpose, string processingSystem, int retentionPeriodDays = 365)
    {
        var inventory = new PersonalDataInventory
        {
            UserId = userId,
            DataCategory = dataCategory,
            DataField = dataField,
            StorageLocation = storageLocation,
            LawfulBasis = lawfulBasis,
            ProcessingPurpose = processingPurpose,
            ProcessingSystem = processingSystem,
            RetentionPeriodDays = retentionPeriodDays,
            CollectedAt = DateTime.UtcNow,
            RetentionReason = GetRetentionReasonFromLawfulBasis(lawfulBasis)
        };

        _inventory.Add(inventory);

        _logger.LogInformation("Personal data recorded for user {UserId}: {DataField} in {StorageLocation}",
            userId, dataField, storageLocation);

        return await Task.FromResult(inventory);
    }

    public async Task<List<PersonalDataInventory>> GetUserPersonalDataAsync(string userId)
    {
        var userInventory = _inventory.Where(i => i.UserId == userId).ToList();
        return await Task.FromResult(userInventory);
    }

    public async Task<PersonalDataSummary> GetPersonalDataSummaryAsync(string userId)
    {
        var userInventory = await GetUserPersonalDataAsync(userId);

        if (!userInventory.Any())
        {
            return new PersonalDataSummary { UserId = userId };
        }

        var summary = new PersonalDataSummary
        {
            UserId = userId,
            TotalDataPoints = userInventory.Count,
            EncryptedDataPoints = userInventory.Count(i => i.IsEncrypted),
            PseudonymizedDataPoints = userInventory.Count(i => i.IsPseudonymized),
            AnonymizedDataPoints = userInventory.Count(i => i.IsAnonymized),
            OldestDataPoint = userInventory.Min(i => i.CollectedAt),
            NewestDataPoint = userInventory.Max(i => i.CollectedAt),
            DataCountByCategory = userInventory.GroupBy(i => i.DataCategory)
                .ToDictionary(g => g.Key, g => g.Count()),
            DataCountByLawfulBasis = userInventory.GroupBy(i => i.LawfulBasis)
                .ToDictionary(g => g.Key, g => g.Count()),
            StorageLocations = userInventory.Select(i => i.StorageLocation).Distinct().ToList(),
            ProcessingSystems = userInventory.Select(i => i.ProcessingSystem).Distinct().ToList()
        };

        return summary;
    }

    public async Task<List<PersonalDataInventory>> GetPersonalDataByCategory(string userId, PersonalDataCategory category)
    {
        var categoryData = _inventory
            .Where(i => i.UserId == userId && i.DataCategory == category)
            .ToList();

        return await Task.FromResult(categoryData);
    }

    public async Task<bool> UpdatePersonalDataAsync(string inventoryId, Dictionary<string, object> updates)
    {
        var inventory = _inventory.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory == null)
            return false;

        foreach (var update in updates)
        {
            var property = typeof(PersonalDataInventory).GetProperty(update.Key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(inventory, update.Value);
            }
        }

        inventory.UpdatedAt = DateTime.UtcNow;
        inventory.LastModifiedAt = DateTime.UtcNow;

        _logger.LogInformation("Personal data inventory {InventoryId} updated", inventoryId);

        return await Task.FromResult(true);
    }

    public async Task<bool> DeletePersonalDataAsync(string inventoryId)
    {
        var inventory = _inventory.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory == null)
            return false;

        _inventory.Remove(inventory);

        _logger.LogInformation("Personal data inventory {InventoryId} deleted for user {UserId}",
            inventoryId, inventory.UserId);

        return await Task.FromResult(true);
    }

    public async Task<bool> MarkForDeletionAsync(string userId, List<PersonalDataCategory> categories, DateTime deletionDate)
    {
        var userInventory = await GetUserPersonalDataAsync(userId);
        var itemsToMark = userInventory.Where(i => categories.Contains(i.DataCategory)).ToList();

        foreach (var item in itemsToMark)
        {
            item.ScheduledForDeletionAt = deletionDate;
            item.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("Marked {Count} data items for deletion for user {UserId} on {DeletionDate}",
            itemsToMark.Count, userId, deletionDate);

        return await Task.FromResult(true);
    }

    public async Task<List<PersonalDataInventory>> GetDataScheduledForDeletionAsync(DateTime? beforeDate = null)
    {
        var cutoffDate = beforeDate ?? DateTime.UtcNow;

        var scheduledData = _inventory
            .Where(i => i.ScheduledForDeletionAt.HasValue && i.ScheduledForDeletionAt <= cutoffDate)
            .ToList();

        return await Task.FromResult(scheduledData);
    }

    public async Task<int> ProcessScheduledDeletionsAsync()
    {
        var dataToDelete = await GetDataScheduledForDeletionAsync();

        foreach (var item in dataToDelete)
        {
            await DeletePersonalDataAsync(item.Id);
        }

        _logger.LogInformation("Processed {Count} scheduled deletions", dataToDelete.Count);

        return dataToDelete.Count;
    }

    public async Task<bool> UpdateLastAccessedAsync(string inventoryId)
    {
        var inventory = _inventory.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory == null)
            return false;

        inventory.LastAccessedAt = DateTime.UtcNow;

        return await Task.FromResult(true);
    }

    public async Task<List<PersonalDataInventory>> GetStaleDataAsync(int daysSinceLastAccess = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastAccess);

        var staleData = _inventory
            .Where(i => !i.LastAccessedAt.HasValue || i.LastAccessedAt < cutoffDate)
            .ToList();

        return await Task.FromResult(staleData);
    }

    public async Task<List<PersonalDataInventory>> GetDataByRetentionReasonAsync(DataRetentionReason reason)
    {
        var data = _inventory.Where(i => i.RetentionReason == reason).ToList();
        return await Task.FromResult(data);
    }

    public async Task<Dictionary<string, int>> GetDataCountByStorageLocationAsync()
    {
        var counts = _inventory
            .GroupBy(i => i.StorageLocation)
            .ToDictionary(g => g.Key, g => g.Count());

        return await Task.FromResult(counts);
    }

    public async Task<Dictionary<string, int>> GetDataCountByProcessingSystemAsync()
    {
        var counts = _inventory
            .GroupBy(i => i.ProcessingSystem)
            .ToDictionary(g => g.Key, g => g.Count());

        return await Task.FromResult(counts);
    }

    public async Task<bool> AnonymizePersonalDataAsync(string inventoryId)
    {
        var inventory = _inventory.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory == null)
            return false;

        inventory.IsAnonymized = true;
        inventory.IsPseudonymized = false; // Can't be both
        inventory.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Personal data inventory {InventoryId} anonymized", inventoryId);

        return await Task.FromResult(true);
    }

    public async Task<bool> PseudonymizePersonalDataAsync(string inventoryId, string pseudonymizationKey)
    {
        var inventory = _inventory.FirstOrDefault(i => i.Id == inventoryId);
        if (inventory == null)
            return false;

        inventory.IsPseudonymized = true;
        inventory.IsAnonymized = false; // Can't be both
        inventory.UpdatedAt = DateTime.UtcNow;

        // Store pseudonymization metadata
        inventory.Metadata["PseudonymizationKey"] = pseudonymizationKey;
        inventory.Metadata["PseudonymizedAt"] = DateTime.UtcNow;

        _logger.LogInformation("Personal data inventory {InventoryId} pseudonymized", inventoryId);

        return await Task.FromResult(true);
    }

    private static DataRetentionReason GetRetentionReasonFromLawfulBasis(DataProcessingLawfulBasis lawfulBasis)
    {
        return lawfulBasis switch
        {
            DataProcessingLawfulBasis.Consent => DataRetentionReason.ConsentGiven,
            DataProcessingLawfulBasis.Contract => DataRetentionReason.OngoingContract,
            DataProcessingLawfulBasis.LegalObligation => DataRetentionReason.LegalObligation,
            DataProcessingLawfulBasis.LegitimateInterests => DataRetentionReason.LegitimateInterest,
            _ => DataRetentionReason.ConsentGiven
        };
    }
}