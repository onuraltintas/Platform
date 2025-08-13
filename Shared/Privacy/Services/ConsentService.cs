using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;
using EgitimPlatform.Shared.Privacy.Attributes;
using System.Reflection;

namespace EgitimPlatform.Shared.Privacy.Services;

public class ConsentService : IConsentService
{
    private readonly ILogger<ConsentService> _logger;
    private readonly PrivacyOptions _options;
    private readonly List<ConsentRecord> _consents; // In-memory storage for demo - replace with actual repository

    public ConsentService(ILogger<ConsentService> logger, IOptions<PrivacyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _consents = new List<ConsentRecord>();
    }

    public async Task<ConsentRecord> CreateConsentAsync(string userId, PersonalDataCategory dataCategory, 
        string purpose, string consentText, string ipAddress, string userAgent, DateTime? expiryDate = null)
    {
        var consent = new ConsentRecord
        {
            UserId = userId,
            DataCategory = dataCategory,
            Purpose = purpose,
            ConsentText = consentText,
            Status = ConsentStatus.Given,
            ConsentGiven = DateTime.UtcNow,
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddDays(_options.ConsentManagement.ConsentExpiryDays),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CollectionMethod = "Web Form"
        };

        _consents.Add(consent);
        
        _logger.LogInformation("Consent created for user {UserId} for {DataCategory} - {Purpose}", 
            userId, dataCategory, purpose);

        return await Task.FromResult(consent);
    }

    public async Task<ConsentRecord?> GetConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose)
    {
        var consent = _consents
            .Where(c => c.UserId == userId && c.DataCategory == dataCategory && c.Purpose == purpose)
            .OrderBy(c => c.ConsentGiven)
            .LastOrDefault();

        return await Task.FromResult(consent);
    }

    public async Task<List<ConsentRecord>> GetUserConsentsAsync(string userId)
    {
        var consents = _consents.Where(c => c.UserId == userId).ToList();
        return await Task.FromResult(consents);
    }

    public async Task<ConsentSummary> GetConsentSummaryAsync(string userId)
    {
        var userConsents = await GetUserConsentsAsync(userId);
        
        var summary = new ConsentSummary
        {
            UserId = userId,
            TotalConsents = userConsents.Count,
            ActiveConsents = userConsents.Count(c => c.Status == ConsentStatus.Given),
            WithdrawnConsents = userConsents.Count(c => c.Status == ConsentStatus.Withdrawn),
            LastUpdated = userConsents.Max(c => c.UpdatedAt) ?? userConsents.Max(c => c.CreatedAt),
            ConsentsByCategory = userConsents
                .GroupBy(c => c.DataCategory)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.ConsentGiven).First().Status)
        };

        return summary;
    }

    public async Task<bool> HasValidConsentAsync(string userId, PersonalDataCategory dataCategory, string purpose)
    {
        var consent = await GetConsentAsync(userId, dataCategory, purpose);
        
        if (consent == null || consent.Status != ConsentStatus.Given)
            return false;

        if (consent.ExpiryDate.HasValue && consent.ExpiryDate < DateTime.UtcNow)
        {
            await UpdateConsentAsync(consent.Id, ConsentStatus.Expired, "Consent expired");
            return false;
        }

        return true;
    }

    public async Task<ConsentRecord> WithdrawConsentAsync(string userId, PersonalDataCategory dataCategory, 
        string purpose, string reason)
    {
        var consent = await GetConsentAsync(userId, dataCategory, purpose);
        
        if (consent == null)
            throw new InvalidOperationException("Consent not found");

        if (!consent.IsWithdrawable)
            throw new InvalidOperationException("This consent cannot be withdrawn");

        consent.Status = ConsentStatus.Withdrawn;
        consent.ConsentWithdrawn = DateTime.UtcNow;
        consent.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Consent withdrawn for user {UserId} for {DataCategory} - {Purpose}. Reason: {Reason}", 
            userId, dataCategory, purpose, reason);

        return consent;
    }

    public async Task<ConsentRecord> UpdateConsentAsync(string consentId, ConsentStatus status, string? reason = null)
    {
        var consent = _consents.FirstOrDefault(c => c.Id == consentId);
        
        if (consent == null)
            throw new InvalidOperationException("Consent not found");

        consent.Status = status;
        consent.UpdatedAt = DateTime.UtcNow;

        if (status == ConsentStatus.Withdrawn)
            consent.ConsentWithdrawn = DateTime.UtcNow;

        _logger.LogInformation("Consent {ConsentId} updated to status {Status}. Reason: {Reason}", 
            consentId, status, reason ?? "Not specified");

        return await Task.FromResult(consent);
    }

    public async Task<List<ConsentRecord>> GetExpiringConsentsAsync(int daysBeforeExpiry = 30)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(daysBeforeExpiry);
        
        var expiringConsents = _consents
            .Where(c => c.Status == ConsentStatus.Given && 
                       c.ExpiryDate.HasValue && 
                       c.ExpiryDate <= expiryThreshold && 
                       c.ExpiryDate > DateTime.UtcNow)
            .ToList();

        return await Task.FromResult(expiringConsents);
    }

    public async Task<List<ConsentRecord>> GetExpiredConsentsAsync()
    {
        var expiredConsents = _consents
            .Where(c => c.Status == ConsentStatus.Given && 
                       c.ExpiryDate.HasValue && 
                       c.ExpiryDate <= DateTime.UtcNow)
            .ToList();

        return await Task.FromResult(expiredConsents);
    }

    public async Task<int> CleanupExpiredConsentsAsync()
    {
        var expiredConsents = await GetExpiredConsentsAsync();
        
        foreach (var consent in expiredConsents)
        {
            await UpdateConsentAsync(consent.Id, ConsentStatus.Expired, "Automatic expiry cleanup");
        }

        _logger.LogInformation("Cleaned up {Count} expired consents", expiredConsents.Count);
        
        return expiredConsents.Count;
    }

    public async Task<bool> ValidateConsentRequirementsAsync(object entity)
    {
        var entityType = entity.GetType();
        var properties = entityType.GetProperties();

        foreach (var property in properties)
        {
            var personalDataAttr = property.GetCustomAttribute<PersonalDataAttribute>();
            if (personalDataAttr?.RequiresConsent == true)
            {
                // Extract user ID from entity - this would need to be implemented based on your entity structure
                var userIdProperty = entityType.GetProperty("UserId");
                if (userIdProperty == null) continue;

                var userId = userIdProperty.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(userId)) continue;

                var hasConsent = await HasValidConsentAsync(userId, personalDataAttr.Category, personalDataAttr.Purpose);
                if (!hasConsent)
                {
                    _logger.LogWarning("Missing consent for user {UserId} for {DataCategory} - {Purpose}", 
                        userId, personalDataAttr.Category, personalDataAttr.Purpose);
                    return false;
                }
            }
        }

        return true;
    }

    public async Task<Dictionary<PersonalDataCategory, bool>> GetConsentStatusByUserAsync(string userId)
    {
        var userConsents = await GetUserConsentsAsync(userId);
        
        var consentStatus = userConsents
            .GroupBy(c => c.DataCategory)
            .ToDictionary(
                g => g.Key, 
                g => g.OrderByDescending(c => c.ConsentGiven)
                      .First()
                      .Status == ConsentStatus.Given
            );

        return consentStatus;
    }

    public async Task<List<ConsentRecord>> GetConsentHistoryAsync(string userId, PersonalDataCategory? dataCategory = null)
    {
        var query = _consents.Where(c => c.UserId == userId);
        
        if (dataCategory.HasValue)
            query = query.Where(c => c.DataCategory == dataCategory.Value);

        var history = query.OrderBy(c => c.ConsentGiven).ToList();
        
        return await Task.FromResult(history);
    }

    public async Task<ConsentRecord> RenewConsentAsync(string userId, PersonalDataCategory dataCategory, 
        string purpose, string newConsentText, string ipAddress, string userAgent)
    {
        // Withdraw existing consent
        var existingConsent = await GetConsentAsync(userId, dataCategory, purpose);
        if (existingConsent != null)
        {
            await WithdrawConsentAsync(userId, dataCategory, purpose, "Renewed with updated consent");
        }

        // Create new consent
        var newConsent = await CreateConsentAsync(userId, dataCategory, purpose, newConsentText, 
            ipAddress, userAgent);

        _logger.LogInformation("Consent renewed for user {UserId} for {DataCategory} - {Purpose}", 
            userId, dataCategory, purpose);

        return newConsent;
    }
}