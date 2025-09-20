using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;

namespace Enterprise.Shared.Privacy.Services;

public class GdprComplianceService : IGdprComplianceService
{
    private readonly PrivacySettings _settings;
    private readonly ILogger<GdprComplianceService> _logger;
    private readonly IPrivacyAuditService? _auditService;
    private readonly IDataAnonymizationService? _anonymizationService;
    private readonly IDataRetentionService? _retentionService;
    private readonly IConsentManagementService? _consentService;

    // In-memory storage for demo purposes
    private readonly List<DataExportRequest> _exportRequests = new();
    private readonly List<DataDeletionRequest> _deletionRequests = new();

    public GdprComplianceService(
        IOptions<PrivacySettings> settings,
        ILogger<GdprComplianceService> logger,
        IPrivacyAuditService? auditService = null,
        IDataAnonymizationService? anonymizationService = null,
        IDataRetentionService? retentionService = null,
        IConsentManagementService? consentService = null)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService;
        _anonymizationService = anonymizationService;
        _retentionService = retentionService;
        _consentService = consentService;
    }

    public async Task<DataExportRequest> ProcessDataExportRequestAsync(string userId, DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        // Get user email - in real implementation this would come from user service
        var userEmail = $"{userId}@example.com";
        var request = await CreateDataExportRequestAsync(userId, userEmail, categories, cancellationToken);
        return await ProcessDataExportRequestAsync(request.Id, cancellationToken);
    }

    public async Task<DataExportRequest> CreateDataExportRequestAsync(string userId, string userEmail, 
        DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var exportRequest = new DataExportRequest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserEmail = userEmail,
                RequestedCategories = categories ?? Enum.GetValues<DataCategory>(),
                Status = ProcessingStatus.Pending,
                RequestDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(_settings.GdprCompliance.ResponseTimeLimit),
                Source = "GdprComplianceService"
            };

            _exportRequests.Add(exportRequest);

            if (_auditService != null)
            {
                await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.DataPortability,
                    $"Data export request created for categories: {string.Join(", ", exportRequest.RequestedCategories)}",
                    cancellationToken);
            }

            _logger.LogInformation("Data export request created: {RequestId} for user {UserId}", 
                exportRequest.Id, userId);

            return exportRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data export request for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DataDeletionRequest> ProcessDataDeletionRequestAsync(string userId, DataSubjectRight requestType, DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        // Get user email - in real implementation this would come from user service
        var userEmail = $"{userId}@example.com";
        var request = await CreateDataDeletionRequestAsync(userId, userEmail, categories, "User exercised right to erasure", cancellationToken);
        return await ProcessDataDeletionRequestAsync(request.Id, cancellationToken);
    }

    public async Task<DataDeletionRequest> CreateDataDeletionRequestAsync(string userId, string userEmail, 
        DataCategory[]? categories = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletionRequest = new DataDeletionRequest
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserEmail = userEmail,
                RequestedCategories = categories ?? Enum.GetValues<DataCategory>(),
                Status = ProcessingStatus.Pending,
                RequestDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(_settings.GdprCompliance.ResponseTimeLimit),
                Reason = reason ?? "User requested data deletion",
                Source = "GdprComplianceService"
            };

            _deletionRequests.Add(deletionRequest);

            if (_auditService != null)
            {
                await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.Erasure,
                    $"Data deletion request created for categories: {string.Join(", ", deletionRequest.RequestedCategories)}",
                    cancellationToken);
            }

            _logger.LogInformation("Data deletion request created: {RequestId} for user {UserId}", 
                deletionRequest.Id, userId);

            return deletionRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data deletion request for user {UserId}", userId);
            throw;
        }
    }

    public async Task<byte[]> ExportUserDataAsync(string userId, DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var exportData = new Dictionary<string, object>();

            // Add user consents if consent service is available
            if (_consentService != null)
            {
                try
                {
                    var consents = await _consentService.GetUserConsentsAsync(userId, cancellationToken);
                    exportData["consents"] = consents.Select(c => new
                    {
                        c.Purpose,
                        c.Status,
                        c.GrantedAt,
                        c.ExpiresAt,
                        c.LegalBasis
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export consent data for user {UserId}", userId);
                }
            }

            // Add personal data records if retention service is available
            if (_retentionService != null)
            {
                try
                {
                    var dataRecords = await _retentionService.GetUserDataRecordsAsync(userId, cancellationToken);
                    
                    if (categories != null && categories.Length > 0)
                    {
                        dataRecords = dataRecords.Where(r => categories.Contains(r.Category)).ToArray();
                    }

                    exportData["personalData"] = dataRecords.Select(r => new
                    {
                        r.Category,
                        r.DataType,
                        Value = r.ProcessedValue ?? r.OriginalValue,
                        r.CreatedAt,
                        r.Source,
                        r.AnonymizationLevel
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export personal data for user {UserId}", userId);
                }
            }

            // Add audit events if audit service is available
            if (_auditService != null)
            {
                try
                {
                    var auditEvents = await _auditService.GetAuditEventsAsync(userId, cancellationToken: cancellationToken);
                    exportData["auditTrail"] = auditEvents.Select(a => new
                    {
                        a.EventType,
                        a.EventDescription,
                        a.Timestamp,
                        a.Source,
                        a.AffectedDataCategories
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export audit data for user {UserId}", userId);
                }
            }

            // Add export metadata
            exportData["exportMetadata"] = new
            {
                ExportDate = DateTime.UtcNow,
                UserId = userId,
                RequestedCategories = categories,
                DataController = _settings.GdprCompliance.DataControllerName,
                LegalBasis = "GDPR Article 20 - Right to data portability"
            };

            var jsonData = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (_auditService != null)
            {
                await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.DataPortability,
                    $"User data exported: {exportData.Count} data sections", cancellationToken);
            }

            _logger.LogInformation("User data exported for {UserId}: {SectionCount} sections", 
                userId, exportData.Count);

            return System.Text.Encoding.UTF8.GetBytes(jsonData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export user data for {UserId}", userId);
            throw;
        }
    }

    public async Task<int> DeleteUserDataAsync(string userId, DataCategory[]? categories = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = 0;

            // Delete from retention service if available
            if (_retentionService != null)
            {
                deletedCount += await _retentionService.DeleteUserDataAsync(userId, categories, cancellationToken);
            }

            // Withdraw consents if consent service is available
            if (_consentService != null && (categories?.Contains(DataCategory.Personal) ?? true))
            {
                try
                {
                    var consents = await _consentService.GetUserConsentsAsync(userId, cancellationToken);
                    var activeConsents = consents.Where(c => c.IsActive).ToArray();
                    
                    if (activeConsents.Length > 0)
                    {
                        var withdrawalRequest = new ConsentWithdrawalRequest
                        {
                            UserId = userId,
                            Purposes = activeConsents.Select(c => c.Purpose).ToArray(),
                            Reason = "User exercised right to erasure"
                        };

                        await _consentService.WithdrawConsentAsync(withdrawalRequest, cancellationToken);
                        deletedCount += activeConsents.Length;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to withdraw consents during data deletion for user {UserId}", userId);
                }
            }

            if (_auditService != null)
            {
                await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.Erasure,
                    $"User data deleted: {deletedCount} records across categories {string.Join(", ", categories ?? Enum.GetValues<DataCategory>())}",
                    cancellationToken);
            }

            _logger.LogInformation("Deleted {Count} data items for user {UserId}", deletedCount, userId);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user data for {UserId}", userId);
            throw;
        }
    }

    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, string reportType = "GDPR", CancellationToken cancellationToken = default)
    {
        return await GenerateComplianceReportAsync((DateTime?)startDate, (DateTime?)endDate, cancellationToken);
    }

    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var report = new ComplianceReport
            {
                Id = Guid.NewGuid().ToString(),
                ReportPeriod = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                DataController = _settings.GdprCompliance.DataControllerName
            };

            // Get export and deletion requests in period
            var exportRequests = _exportRequests
                .Where(r => r.RequestDate >= from && r.RequestDate <= to)
                .ToArray();

            var deletionRequests = _deletionRequests
                .Where(r => r.RequestDate >= from && r.RequestDate <= to)
                .ToArray();

            // Calculate metrics
            var metrics = new ComplianceMetrics
            {
                TotalDataSubjectRequests = exportRequests.Length + deletionRequests.Length,
                DataExportRequests = exportRequests.Length,
                DataDeletionRequests = deletionRequests.Length,
                RequestsProcessedOnTime = exportRequests.Count(r => r.ProcessedDate <= r.DueDate) +
                                        deletionRequests.Count(r => r.ProcessedDate <= r.DueDate),
                AverageResponseTime = CalculateAverageResponseTime(exportRequests.Cast<object>().Concat(deletionRequests.Cast<object>())),
                PendingRequests = exportRequests.Count(r => r.Status == ProcessingStatus.Pending) +
                                 deletionRequests.Count(r => r.Status == ProcessingStatus.Pending)
            };

            report.Metrics = metrics;

            // Identify compliance issues
            var issues = new List<ComplianceIssue>();

            // Check for overdue requests
            var overdueExports = exportRequests.Where(r => r.Status == ProcessingStatus.Pending && r.DueDate < DateTime.UtcNow);
            var overdueDeletions = deletionRequests.Where(r => r.Status == ProcessingStatus.Pending && r.DueDate < DateTime.UtcNow);

            foreach (var request in overdueExports)
            {
                issues.Add(new ComplianceIssue
                {
                    Type = "OverdueRequest",
                    Description = $"Data export request {request.Id} is overdue",
                    Severity = "High",
                    RequestId = request.Id,
                    UserId = request.UserId,
                    DetectedAt = DateTime.UtcNow
                });
            }

            foreach (var request in overdueDeletions)
            {
                issues.Add(new ComplianceIssue
                {
                    Type = "OverdueRequest",
                    Description = $"Data deletion request {request.Id} is overdue",
                    Severity = "High",
                    RequestId = request.Id,
                    UserId = request.UserId,
                    DetectedAt = DateTime.UtcNow
                });
            }

            report.Issues = issues.ToArray();
            report.ComplianceScore = CalculateComplianceScore(metrics, issues.Count);

            _logger.LogInformation("Compliance report generated: {Score}% compliance score", 
                report.ComplianceScore);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            throw;
        }
    }

    public async Task<DataExportRequest[]> GetDataExportRequestsAsync(string? userId = null, 
        ProcessingStatus? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var requests = _exportRequests.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                requests = requests.Where(r => r.UserId == userId);

            if (status.HasValue)
                requests = requests.Where(r => r.Status == status.Value);

            return requests.OrderByDescending(r => r.RequestDate).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data export requests");
            throw;
        }
    }

    public async Task<DataDeletionRequest[]> GetDataDeletionRequestsAsync(string? userId = null, 
        ProcessingStatus? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var requests = _deletionRequests.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                requests = requests.Where(r => r.UserId == userId);

            if (status.HasValue)
                requests = requests.Where(r => r.Status == status.Value);

            return requests.OrderByDescending(r => r.RequestDate).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data deletion requests");
            throw;
        }
    }

    public async Task<DataExportRequest> ProcessDataExportRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = _exportRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null)
                throw new ArgumentException($"Export request {requestId} not found", nameof(requestId));

            if (request.Status != ProcessingStatus.Pending)
                throw new InvalidOperationException($"Export request {requestId} is not in pending status");

            request.Status = ProcessingStatus.InProgress;
            request.ProcessingStartedAt = DateTime.UtcNow;

            try
            {
                var exportData = await ExportUserDataAsync(request.UserId, request.RequestedCategories, cancellationToken);
                
                request.ExportData = exportData;
                request.Status = ProcessingStatus.Completed;
                request.ProcessedDate = DateTime.UtcNow;
                request.CompletionNotes = $"Export completed successfully. Data size: {exportData.Length} bytes";

                _logger.LogInformation("Data export request processed: {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                request.Status = ProcessingStatus.Failed;
                request.CompletionNotes = $"Export failed: {ex.Message}";
                _logger.LogError(ex, "Failed to process data export request {RequestId}", requestId);
                throw;
            }

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data export request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<DataDeletionRequest> ProcessDataDeletionRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = _deletionRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null)
                throw new ArgumentException($"Deletion request {requestId} not found", nameof(requestId));

            if (request.Status != ProcessingStatus.Pending)
                throw new InvalidOperationException($"Deletion request {requestId} is not in pending status");

            request.Status = ProcessingStatus.InProgress;
            request.ProcessingStartedAt = DateTime.UtcNow;

            try
            {
                var deletedCount = await DeleteUserDataAsync(request.UserId, request.RequestedCategories, cancellationToken);
                
                request.DeletedRecordsCount = deletedCount;
                request.Status = ProcessingStatus.Completed;
                request.ProcessedDate = DateTime.UtcNow;
                request.CompletionNotes = $"Deletion completed successfully. {deletedCount} records deleted";

                _logger.LogInformation("Data deletion request processed: {RequestId}, deleted {Count} records", 
                    requestId, deletedCount);
            }
            catch (Exception ex)
            {
                request.Status = ProcessingStatus.Failed;
                request.CompletionNotes = $"Deletion failed: {ex.Message}";
                _logger.LogError(ex, "Failed to process data deletion request {RequestId}", requestId);
                throw;
            }

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process data deletion request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<bool> ValidateGdprComplianceAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var isCompliant = true;
            var issues = new List<string>();

            // Check consent validity if consent service is available
            if (_consentService != null)
            {
                try
                {
                    var consents = await _consentService.GetUserConsentsAsync(userId, cancellationToken);
                    var activeConsents = consents.Where(c => c.IsActive).ToArray();
                    
                    if (activeConsents.Any(c => c.ExpiresAt <= DateTime.UtcNow))
                    {
                        issues.Add("User has expired consents that should be processed");
                        isCompliant = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate consents for user {UserId}", userId);
                }
            }

            // Check data retention compliance if retention service is available
            if (_retentionService != null)
            {
                try
                {
                    var retentionCompliant = await _retentionService.ValidateRetentionComplianceAsync(cancellationToken);
                    if (!retentionCompliant)
                    {
                        issues.Add("User has data retention compliance issues");
                        isCompliant = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate data retention for user {UserId}", userId);
                }
            }

            // Check for pending requests that are overdue
            var userExportRequests = _exportRequests
                .Where(r => r.UserId == userId && r.Status == ProcessingStatus.Pending && r.DueDate < DateTime.UtcNow)
                .ToArray();

            var userDeletionRequests = _deletionRequests
                .Where(r => r.UserId == userId && r.Status == ProcessingStatus.Pending && r.DueDate < DateTime.UtcNow)
                .ToArray();

            if (userExportRequests.Length > 0 || userDeletionRequests.Length > 0)
            {
                issues.Add($"User has {userExportRequests.Length + userDeletionRequests.Length} overdue GDPR requests");
                isCompliant = false;
            }

            if (issues.Any())
            {
                _logger.LogWarning("GDPR compliance issues for user {UserId}: {Issues}", 
                    userId, string.Join("; ", issues));
            }

            return isCompliant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate GDPR compliance for user {UserId}", userId);
            return false;
        }
    }

    private static double CalculateAverageResponseTime(IEnumerable<object> requests)
    {
        var completedRequests = new List<TimeSpan>();

        foreach (var request in requests)
        {
            if (request is DataExportRequest exportReq && exportReq.ProcessedDate.HasValue)
            {
                completedRequests.Add(exportReq.ProcessedDate.Value - exportReq.RequestDate);
            }
            else if (request is DataDeletionRequest deleteReq && deleteReq.ProcessedDate.HasValue)
            {
                completedRequests.Add(deleteReq.ProcessedDate.Value - deleteReq.RequestDate);
            }
        }

        return completedRequests.Any() ? completedRequests.Average(t => t.TotalHours) : 0;
    }

    private static double CalculateComplianceScore(ComplianceMetrics metrics, int issueCount)
    {
        if (metrics.TotalDataSubjectRequests == 0) return 100.0;

        var timelyProcessingScore = metrics.TotalDataSubjectRequests > 0 
            ? (metrics.RequestsProcessedOnTime / (double)metrics.TotalDataSubjectRequests) * 70.0
            : 70.0;

        var issueScore = Math.Max(0, 30.0 - (issueCount * 5.0));

        return Math.Round(timelyProcessingScore + issueScore, 1);
    }

    public async Task<DataExportRequest> GetDataExportStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = _exportRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null)
                throw new ArgumentException($"Export request {requestId} not found", nameof(requestId));

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get export status for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<DataDeletionRequest> GetDataDeletionStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = _deletionRequests.FirstOrDefault(r => r.Id == requestId);
            if (request == null)
                throw new ArgumentException($"Deletion request {requestId} not found", nameof(requestId));

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deletion status for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<PersonalDataRecord[]> GetUserPersonalDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_retentionService != null)
            {
                return await _retentionService.GetUserDataRecordsAsync(userId, cancellationToken);
            }

            return Array.Empty<PersonalDataRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user personal data for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserDataAsync(string userId, Dictionary<string, string> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_retentionService != null)
            {
                var records = await _retentionService.GetUserDataRecordsAsync(userId, cancellationToken);
                
                foreach (var record in records)
                {
                    foreach (var update in updates)
                    {
                        if (record.DataType == update.Key)
                        {
                            record.OriginalValue = update.Value;
                            record.Metadata["UpdatedAt"] = DateTime.UtcNow.ToString("O");
                            record.Metadata["UpdatedBy"] = "GdprComplianceService";
                        }
                    }
                }

                if (_auditService != null)
                {
                    await _auditService.LogDataModificationAsync(userId, string.Join(",", records.Select(r => r.Id)),
                        DataCategory.Personal, $"User data updated: {string.Join(", ", updates.Keys)}", cancellationToken);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user data for {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RestrictDataProcessingAsync(string userId, DataCategory[] categories, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_retentionService != null)
            {
                var records = await _retentionService.GetUserDataRecordsAsync(userId, cancellationToken);
                var restrictedRecords = records.Where(r => categories.Contains(r.Category)).ToArray();

                foreach (var record in restrictedRecords)
                {
                    record.ProcessingStatus = ProcessingStatus.Restricted;
                    record.Metadata["RestrictedAt"] = DateTime.UtcNow.ToString("O");
                    record.Metadata["RestrictedBy"] = "GdprComplianceService";
                }

                if (_auditService != null)
                {
                    await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.Restriction,
                        $"Data processing restricted for categories: {string.Join(", ", categories)}", cancellationToken);
                }

                _logger.LogInformation("Data processing restricted for user {UserId} in categories {Categories}",
                    userId, string.Join(", ", categories));

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restrict data processing for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ObjectToDataProcessingAsync(string userId, ConsentPurpose[] purposes, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_consentService != null)
            {
                var withdrawalRequest = new ConsentWithdrawalRequest
                {
                    UserId = userId,
                    Purposes = purposes,
                    Reason = "User objected to data processing"
                };

                var result = await _consentService.WithdrawConsentAsync(withdrawalRequest, cancellationToken);

                if (_auditService != null)
                {
                    await _auditService.LogUserRightExercisedAsync(userId, DataSubjectRight.Objection,
                        $"User objected to processing for purposes: {string.Join(", ", purposes)}", cancellationToken);
                }

                return result;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process objection for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateDataProcessingLegalityAsync(string userId, ConsentPurpose purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_consentService != null)
            {
                return await _consentService.HasValidConsentAsync(userId, purpose, cancellationToken);
            }

            // Default to compliant if no consent service available
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate data processing legality for user {UserId} and purpose {Purpose}", userId, purpose);
            return false;
        }
    }

    public async Task<ComplianceIssue[]> IdentifyComplianceIssuesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<ComplianceIssue>();
            var now = DateTime.UtcNow;

            // Check for overdue requests
            var overdueExports = _exportRequests
                .Where(r => r.Status == ProcessingStatus.Pending && r.DueDate < now)
                .ToArray();

            var overdueDeletions = _deletionRequests
                .Where(r => r.Status == ProcessingStatus.Pending && r.DueDate < now)
                .ToArray();

            foreach (var request in overdueExports.Cast<object>().Concat(overdueDeletions.Cast<object>()))
            {
                var requestInfo = request switch
                {
                    DataExportRequest exp => new { Id = exp.Id, UserId = exp.UserId, Type = "Export" },
                    DataDeletionRequest del => new { Id = del.Id, UserId = del.UserId, Type = "Deletion" },
                    _ => null
                };

                if (requestInfo != null)
                {
                    issues.Add(new ComplianceIssue
                    {
                        Type = "OverdueRequest",
                        Description = $"{requestInfo.Type} request {requestInfo.Id} is overdue",
                        Severity = "High",
                        RequestId = requestInfo.Id,
                        UserId = requestInfo.UserId,
                        DetectedAt = now
                    });
                }
            }

            return issues.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify compliance issues");
            throw;
        }
    }

    public async Task<bool> NotifyDataBreachAsync(string description, DataCategory[] affectedCategories, string[] affectedUserIds, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_auditService != null)
            {
                foreach (var userId in affectedUserIds)
                {
                    await _auditService.LogPolicyViolationAsync(userId, description, "Critical",
                        new Dictionary<string, object>
                        {
                            ["BreachType"] = "DataBreach",
                            ["AffectedCategories"] = affectedCategories.Select(c => c.ToString()).ToArray(),
                            ["NotificationDate"] = DateTime.UtcNow
                        }, cancellationToken);
                }
            }

            _logger.LogCritical("Data breach reported: {Description}. Affected users: {UserCount}, Categories: {Categories}",
                description, affectedUserIds.Length, string.Join(", ", affectedCategories));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify data breach");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetComplianceMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await GenerateComplianceReportAsync(cancellationToken: cancellationToken);
            
            return new Dictionary<string, object>
            {
                ["ComplianceScore"] = report.ComplianceScore,
                ["TotalRequests"] = report.Metrics.TotalDataSubjectRequests,
                ["PendingRequests"] = report.Metrics.PendingRequests,
                ["AverageResponseTime"] = report.Metrics.AverageResponseTime,
                ["IssueCount"] = report.Issues?.Length ?? 0,
                ["LastReportGenerated"] = report.GeneratedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compliance metrics");
            throw;
        }
    }

    public async Task<bool> VerifyConsentLegalBasisAsync(string userId, ConsentPurpose purpose, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_consentService != null)
            {
                var consent = await _consentService.GetConsentAsync(userId, purpose, cancellationToken);
                return consent?.IsActive == true && consent.LegalBasis != LegalBasis.NotSpecified;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify consent legal basis for user {UserId} and purpose {Purpose}", userId, purpose);
            return false;
        }
    }
}