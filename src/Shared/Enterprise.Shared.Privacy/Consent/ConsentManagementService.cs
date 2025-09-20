using System.Collections.Concurrent;
using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Privacy.Consent;

public class ConsentManagementService : IConsentManagementService
{
    private readonly PrivacySettings _settings;
    private readonly ILogger<ConsentManagementService> _logger;
    private readonly IPrivacyAuditService? _auditService;
    
    // In-memory storage for demo purposes - in production this would be a database
    private readonly ConcurrentDictionary<string, ConsentRecord> _consentRecords = new();
    private readonly ConcurrentDictionary<string, List<ConsentHistory>> _consentHistory = new();

    public ConsentManagementService(
        IOptions<PrivacySettings> settings,
        ILogger<ConsentManagementService> logger,
        IPrivacyAuditService? auditService = null)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService;
    }

    public async Task<ConsentRecord> GrantConsentAsync(ConsentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Purposes.Length == 0)
            throw new ArgumentException("At least one purpose must be specified", nameof(request));

        try
        {
            var consentRecord = new ConsentRecord
            {
                UserId = request.UserId,
                UserEmail = request.UserEmail,
                Purpose = request.Purposes[0], // For simplicity, taking first purpose
                Status = ConsentStatus.Granted,
                LegalBasis = request.LegalBasis,
                GrantedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiryDate ?? DateTime.UtcNow.AddDays(_settings.ConsentManagement.ConsentExpirationDays),
                Version = request.Version,
                Source = request.Source,
                Metadata = new Dictionary<string, string>(request.Metadata ?? new Dictionary<string, string>())
            };

            _consentRecords[consentRecord.Id] = consentRecord;

            // Log consent history
            await AddConsentHistoryAsync(consentRecord.Id, ConsentStatus.Pending, ConsentStatus.Granted, 
                "Consent granted", "User", cancellationToken);

            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogConsentChangeAsync(request.UserId, consentRecord.Purpose, 
                    ConsentStatus.Pending, ConsentStatus.Granted, "Consent granted", cancellationToken);
            }

            _logger.LogInformation("Consent granted for user {UserId} and purpose {Purpose}", 
                request.UserId, consentRecord.Purpose);

            return consentRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant consent for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<ConsentRecord[]> GrantMultipleConsentsAsync(ConsentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var tasks = request.Purposes.Select(purpose =>
            {
                var singleRequest = new ConsentRequest
                {
                    UserId = request.UserId,
                    UserEmail = request.UserEmail,
                    Purposes = new[] { purpose },
                    LegalBasis = request.LegalBasis,
                    Source = request.Source,
                    Version = request.Version,
                    Metadata = request.Metadata,
                    ExpiryDate = request.ExpiryDate
                };
                return GrantConsentAsync(singleRequest, cancellationToken);
            });

            var results = await Task.WhenAll(tasks);
            
            _logger.LogInformation("Multiple consents granted for user {UserId}: {Purposes}", 
                request.UserId, string.Join(", ", request.Purposes));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant multiple consents for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<bool> WithdrawConsentAsync(ConsentWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var userConsents = _consentRecords.Values
                .Where(c => c.UserId == request.UserId && 
                           request.Purposes.Contains(c.Purpose) && 
                           c.Status == ConsentStatus.Granted)
                .ToArray();

            if (userConsents.Length == 0)
            {
                _logger.LogWarning("No active consents found for user {UserId} and purposes {Purposes}", 
                    request.UserId, string.Join(", ", request.Purposes));
                return false;
            }

            foreach (var consent in userConsents)
            {
                consent.Status = ConsentStatus.Withdrawn;
                consent.WithdrawnAt = DateTime.UtcNow;
                consent.WithdrawalReason = request.Reason;

                // Log consent history
                await AddConsentHistoryAsync(consent.Id, ConsentStatus.Granted, ConsentStatus.Withdrawn, 
                    request.Reason ?? "User withdrawal", "User", cancellationToken);

                // Audit log
                if (_auditService != null)
                {
                    await _auditService.LogConsentChangeAsync(request.UserId, consent.Purpose, 
                        ConsentStatus.Granted, ConsentStatus.Withdrawn, request.Reason, cancellationToken);
                }
            }

            _logger.LogInformation("Consents withdrawn for user {UserId}: {Purposes}", 
                request.UserId, string.Join(", ", request.Purposes));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consent for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<ConsentRecord?> GetConsentAsync(string userId, ConsentPurpose purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = _consentRecords.Values
                .Where(c => c.UserId == userId && c.Purpose == purpose)
                .OrderByDescending(c => c.GrantedAt)
                .FirstOrDefault();

            if (consent != null && _auditService != null)
            {
                await _auditService.LogDataAccessAsync(userId, consent.Id, DataCategory.Personal, 
                    "ConsentManagement", cancellationToken);
            }

            return consent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent for user {UserId} and purpose {Purpose}", userId, purpose);
            throw;
        }
    }

    public async Task<ConsentRecord[]> GetUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var consents = _consentRecords.Values
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.GrantedAt)
                .ToArray();

            if (consents.Length > 0 && _auditService != null)
            {
                await _auditService.LogDataAccessAsync(userId, string.Join(",", consents.Select(c => c.Id)), 
                    DataCategory.Personal, "ConsentManagement", cancellationToken);
            }

            return consents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consents for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ConsentSummary> GetConsentSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userConsents = await GetUserConsentsAsync(userId, cancellationToken);
            
            var summary = new ConsentSummary
            {
                UserId = userId,
                UserEmail = userConsents.FirstOrDefault()?.UserEmail ?? string.Empty,
                ConsentsByPurpose = userConsents.GroupBy(c => c.Purpose)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.GrantedAt).First().Status),
                LastUpdated = userConsents.Any() ? userConsents.Max(c => c.GrantedAt) : DateTime.MinValue,
                TotalConsents = userConsents.Length,
                ActiveConsents = userConsents.Count(c => c.IsActive),
                WithdrawnConsents = userConsents.Count(c => c.Status == ConsentStatus.Withdrawn),
                NextExpiration = userConsents.Where(c => c.ExpiresAt.HasValue && c.IsActive)
                    .MinBy(c => c.ExpiresAt)?.ExpiresAt
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent summary for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasValidConsentAsync(string userId, ConsentPurpose purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = await GetConsentAsync(userId, purpose, cancellationToken);
            return consent?.IsActive == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent validity for user {UserId} and purpose {Purpose}", userId, purpose);
            return false;
        }
    }

    public async Task<bool> HasValidConsentAsync(string userId, ConsentPurpose[] purposes, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = purposes.Select(purpose => HasValidConsentAsync(userId, purpose, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent validity for user {UserId} and purposes {Purposes}", 
                userId, string.Join(", ", purposes));
            return false;
        }
    }

    public async Task<ConsentRecord> UpdateConsentAsync(string consentId, ConsentStatus newStatus, 
        string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_consentRecords.TryGetValue(consentId, out var consent))
                throw new ArgumentException($"Consent record {consentId} not found", nameof(consentId));

            var oldStatus = consent.Status;
            consent.Status = newStatus;

            if (newStatus == ConsentStatus.Withdrawn)
            {
                consent.WithdrawnAt = DateTime.UtcNow;
                consent.WithdrawalReason = reason;
            }

            // Log consent history
            await AddConsentHistoryAsync(consentId, oldStatus, newStatus, 
                reason ?? "Status updated", "System", cancellationToken);

            // Audit log
            if (_auditService != null)
            {
                await _auditService.LogConsentChangeAsync(consent.UserId, consent.Purpose, 
                    oldStatus, newStatus, reason, cancellationToken);
            }

            _logger.LogInformation("Consent {ConsentId} status updated from {OldStatus} to {NewStatus}", 
                consentId, oldStatus, newStatus);

            return consent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update consent {ConsentId}", consentId);
            throw;
        }
    }

    public async Task<ConsentHistory[]> GetConsentHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userConsentIds = _consentRecords.Values
                .Where(c => c.UserId == userId)
                .Select(c => c.Id)
                .ToArray();

            var history = new List<ConsentHistory>();
            foreach (var consentId in userConsentIds)
            {
                if (_consentHistory.TryGetValue(consentId, out var consentHistoryList))
                {
                    history.AddRange(consentHistoryList);
                }
            }

            return history.OrderByDescending(h => h.ChangedAt).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> ProcessExpiredConsentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredConsents = _consentRecords.Values
                .Where(c => c.Status == ConsentStatus.Granted && 
                           c.ExpiresAt.HasValue && 
                           c.ExpiresAt <= DateTime.UtcNow)
                .ToArray();

            foreach (var consent in expiredConsents)
            {
                await UpdateConsentAsync(consent.Id, ConsentStatus.Expired, 
                    "Consent expired", cancellationToken);
            }

            _logger.LogInformation("Processed {Count} expired consents", expiredConsents.Length);
            return expiredConsents.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process expired consents");
            throw;
        }
    }

    public async Task<ConsentRecord[]> GetExpiringConsentsAsync(int daysFromNow, CancellationToken cancellationToken = default)
    {
        try
        {
            var expirationDate = DateTime.UtcNow.AddDays(daysFromNow);
            
            return _consentRecords.Values
                .Where(c => c.Status == ConsentStatus.Granted && 
                           c.ExpiresAt.HasValue && 
                           c.ExpiresAt <= expirationDate && 
                           c.ExpiresAt > DateTime.UtcNow)
                .OrderBy(c => c.ExpiresAt)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring consents");
            throw;
        }
    }

    public async Task<bool> ValidateConsentRequirementsAsync(string userId, ConsentPurpose[] purposes, CancellationToken cancellationToken = default)
    {
        if (!_settings.ConsentManagement.RequireExplicitConsent)
            return true;

        try
        {
            return await HasValidConsentAsync(userId, purposes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate consent requirements for user {UserId}", userId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetConsentStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allConsents = _consentRecords.Values.ToArray();
            
            return new Dictionary<string, object>
            {
                ["TotalConsents"] = allConsents.Length,
                ["ActiveConsents"] = allConsents.Count(c => c.IsActive),
                ["WithdrawnConsents"] = allConsents.Count(c => c.Status == ConsentStatus.Withdrawn),
                ["ExpiredConsents"] = allConsents.Count(c => c.Status == ConsentStatus.Expired),
                ["ConsentsByPurpose"] = allConsents.GroupBy(c => c.Purpose)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["ConsentsByStatus"] = allConsents.GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["ConsentsByLegalBasis"] = allConsents.GroupBy(c => c.LegalBasis)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["AverageConsentDuration"] = allConsents.Where(c => c.ExpiresAt.HasValue)
                    .Average(c => (c.ExpiresAt!.Value - c.GrantedAt).TotalDays),
                ["LastConsentGranted"] = allConsents.Any() ? allConsents.Max(c => c.GrantedAt) : (DateTime?)null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent statistics");
            throw;
        }
    }

    private async Task AddConsentHistoryAsync(string consentRecordId, ConsentStatus oldStatus, 
        ConsentStatus newStatus, string reason, string changedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var historyEntry = new ConsentHistory
            {
                ConsentRecordId = consentRecordId,
                UserId = _consentRecords[consentRecordId].UserId,
                PreviousStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                ChangedBy = changedBy,
                ChangeDetails = new Dictionary<string, string>
                {
                    ["Timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["Source"] = "ConsentManagementService"
                }
            };

            _consentHistory.AddOrUpdate(consentRecordId, 
                new List<ConsentHistory> { historyEntry },
                (key, existingList) =>
                {
                    existingList.Add(historyEntry);
                    return existingList;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add consent history for {ConsentRecordId}", consentRecordId);
        }
    }
}