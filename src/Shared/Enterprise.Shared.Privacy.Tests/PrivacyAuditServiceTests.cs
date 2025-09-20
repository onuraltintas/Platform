using Enterprise.Shared.Privacy.Models;
using Enterprise.Shared.Privacy.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Enterprise.Shared.Privacy.Tests;

public class PrivacyAuditServiceTests
{
    private readonly Mock<ILogger<PrivacyAuditService>> _loggerMock;
    private readonly Mock<IOptions<PrivacySettings>> _optionsMock;
    private readonly PrivacyAuditService _service;
    private readonly PrivacySettings _settings;

    public PrivacyAuditServiceTests()
    {
        _loggerMock = new Mock<ILogger<PrivacyAuditService>>();
        _optionsMock = new Mock<IOptions<PrivacySettings>>();
        
        _settings = new PrivacySettings
        {
            AuditLogging = new AuditLoggingSettings
            {
                EnableAuditLogging = true,
                EnableStructuredLogging = true,
                AuditLogRetentionDays = 365
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _service = new PrivacyAuditService(_optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task LogAuditEventAsync_WithValidEvent_LogsSuccessfully()
    {
        // Arrange
        var auditEvent = new PrivacyAuditEvent
        {
            UserId = "user123",
            EventType = AuditEventType.DataAccess,
            EventDescription = "User accessed personal data",
            Source = "TestService"
        };

        // Act
        await _service.LogAuditEventAsync(auditEvent);

        // Assert
        Assert.True(auditEvent.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task LogAuditEventAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.LogAuditEventAsync(null!));
    }

    [Fact]
    public async Task LogDataAccessAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var dataRecordId = "record456";
        var category = DataCategory.Personal;
        var source = "TestService";

        // Act
        await _service.LogDataAccessAsync(userId, dataRecordId, category, source);

        // Assert - Verify event was created by checking we can retrieve it
        var events = await _service.GetAuditEventsAsync(userId);
        var dataAccessEvent = events.FirstOrDefault();
        
        Assert.NotNull(dataAccessEvent);
        Assert.Equal(AuditEventType.DataAccess, dataAccessEvent.EventType);
        Assert.Equal(userId, dataAccessEvent.UserId);
        Assert.Equal(dataRecordId, dataAccessEvent.DataRecordIds);
        Assert.Contains(category, dataAccessEvent.AffectedDataCategories!);
    }

    [Fact]
    public async Task LogDataModificationAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var dataRecordId = "record456";
        var category = DataCategory.Personal;
        var modificationDetails = "Updated email address";

        // Act
        await _service.LogDataModificationAsync(userId, dataRecordId, category, modificationDetails);

        // Assert
        var events = await _service.GetAuditEventsAsync(userId);
        var modificationEvent = events.FirstOrDefault();
        
        Assert.NotNull(modificationEvent);
        Assert.Equal(AuditEventType.DataModified, modificationEvent.EventType);
        Assert.Equal(modificationDetails, modificationEvent.EventDescription);
    }

    [Fact]
    public async Task LogConsentChangeAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var purpose = ConsentPurpose.Marketing;
        var oldStatus = ConsentStatus.Pending;
        var newStatus = ConsentStatus.Granted;
        var reason = "User granted consent";

        // Act
        await _service.LogConsentChangeAsync(userId, purpose, oldStatus, newStatus, reason);

        // Assert
        var events = await _service.GetAuditEventsAsync(userId);
        var consentEvent = events.FirstOrDefault();
        
        Assert.NotNull(consentEvent);
        Assert.Equal(AuditEventType.ConsentGranted, consentEvent.EventType);
        Assert.Contains(purpose, consentEvent.AffectedPurposes!);
        Assert.Contains("OldStatus", consentEvent.EventData.Keys);
        Assert.Contains("NewStatus", consentEvent.EventData.Keys);
    }

    [Fact]
    public async Task LogDataDeletionAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var categories = new[] { DataCategory.Personal, DataCategory.Financial };
        var recordsDeleted = 5;

        // Act
        await _service.LogDataDeletionAsync(userId, categories, recordsDeleted);

        // Assert
        var events = await _service.GetAuditEventsAsync(userId);
        var deletionEvent = events.FirstOrDefault();
        
        Assert.NotNull(deletionEvent);
        Assert.Equal(AuditEventType.DataDeleted, deletionEvent.EventType);
        Assert.Equal(categories, deletionEvent.AffectedDataCategories);
        Assert.Equal(recordsDeleted, deletionEvent.EventData["RecordsDeleted"]);
    }

    [Fact]
    public async Task LogUserRightExercisedAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var right = DataSubjectRight.DataPortability;
        var details = "User requested data export";

        // Act
        await _service.LogUserRightExercisedAsync(userId, right, details);

        // Assert
        var events = await _service.GetAuditEventsAsync(userId);
        var rightEvent = events.FirstOrDefault();
        
        Assert.NotNull(rightEvent);
        Assert.Equal(AuditEventType.UserRightExercised, rightEvent.EventType);
        Assert.Equal(details, rightEvent.EventDescription);
        Assert.Equal(right.ToString(), rightEvent.EventData["Right"]);
    }

    [Fact]
    public async Task LogPolicyViolationAsync_CreatesCorrectAuditEvent()
    {
        // Arrange
        var userId = "user123";
        var violation = "Unauthorized data access attempt";
        var severity = "High";
        var details = new Dictionary<string, object> { ["Source"] = "API", ["Endpoint"] = "/sensitive-data" };

        // Act
        await _service.LogPolicyViolationAsync(userId, violation, severity, details);

        // Assert
        var events = await _service.GetAuditEventsAsync(userId);
        var violationEvent = events.FirstOrDefault();
        
        Assert.NotNull(violationEvent);
        Assert.Equal(AuditEventType.PolicyViolation, violationEvent.EventType);
        Assert.Equal(violation, violationEvent.EventDescription);
        Assert.Equal(severity, violationEvent.Severity);
        Assert.True(violationEvent.RequiresNotification); // High severity should require notification
    }

    [Fact]
    public async Task GetAuditEventsAsync_WithDateFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var userId = "user123";
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        // Create some test events
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        await _service.LogDataAccessAsync(userId, "record2", DataCategory.Personal, "Test");

        // Act
        var events = await _service.GetAuditEventsAsync(userId, fromDate, toDate);

        // Assert
        Assert.Equal(2, events.Length);
        Assert.All(events, e => Assert.True(e.Timestamp >= fromDate && e.Timestamp <= toDate));
    }

    [Fact]
    public async Task GetAuditEventsByTypeAsync_FiltersCorrectly()
    {
        // Arrange
        var userId = "user123";
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        await _service.LogConsentChangeAsync(userId, ConsentPurpose.Marketing, ConsentStatus.Pending, ConsentStatus.Granted);

        // Act
        var dataAccessEvents = await _service.GetAuditEventsByTypeAsync(AuditEventType.DataAccess);
        var consentEvents = await _service.GetAuditEventsByTypeAsync(AuditEventType.ConsentGranted);

        // Assert
        Assert.Single(dataAccessEvents);
        Assert.Single(consentEvents);
        Assert.Equal(AuditEventType.DataAccess, dataAccessEvents[0].EventType);
        Assert.Equal(AuditEventType.ConsentGranted, consentEvents[0].EventType);
    }

    [Fact]
    public async Task GeneratePrivacyMetricsAsync_ReturnsCorrectMetrics()
    {
        // Arrange
        var userId1 = "user123";
        var userId2 = "user456";
        
        await _service.LogConsentChangeAsync(userId1, ConsentPurpose.Marketing, ConsentStatus.Pending, ConsentStatus.Granted);
        await _service.LogConsentChangeAsync(userId2, ConsentPurpose.Analytics, ConsentStatus.Pending, ConsentStatus.Granted);
        await _service.LogDataAccessAsync(userId1, "record1", DataCategory.Personal, "Test");

        // Act
        var metrics = await _service.GeneratePrivacyMetricsAsync();

        // Assert
        Assert.Equal(3, metrics.TotalAuditEvents);
        Assert.Equal(2, metrics.ConsentsByPurpose.Values.Sum());
        Assert.Equal(1, metrics.DataRecordsByCategory.Values.Sum());
    }

    [Fact]
    public async Task GetAuditEventSummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var userId = "user123";
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        await _service.LogDataAccessAsync(userId, "record2", DataCategory.Personal, "Test");
        await _service.LogConsentChangeAsync(userId, ConsentPurpose.Marketing, ConsentStatus.Pending, ConsentStatus.Granted);

        // Act
        var summary = await _service.GetAuditEventSummaryAsync();

        // Assert
        Assert.Equal(2, summary[AuditEventType.DataAccess.ToString()]);
        Assert.Equal(1, summary[AuditEventType.ConsentGranted.ToString()]);
    }

    [Fact]
    public async Task SearchAuditEventsAsync_WithSearchTerm_ReturnsMatchingEvents()
    {
        // Arrange
        var userId1 = "user123";
        var userId2 = "user456";
        
        await _service.LogDataAccessAsync(userId1, "record1", DataCategory.Personal, "Test");
        await _service.LogUserRightExercisedAsync(userId2, DataSubjectRight.Access, "Special request");

        // Act
        var searchResults = await _service.SearchAuditEventsAsync("Special");

        // Assert
        Assert.Single(searchResults);
        Assert.Contains("Special", searchResults[0].EventDescription);
    }

    [Fact]
    public async Task SearchAuditEventsAsync_WithEventTypeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var userId = "user123";
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        await _service.LogConsentChangeAsync(userId, ConsentPurpose.Marketing, ConsentStatus.Pending, ConsentStatus.Granted);

        // Act
        var searchResults = await _service.SearchAuditEventsAsync("", new[] { AuditEventType.DataAccess });

        // Assert
        Assert.Single(searchResults);
        Assert.Equal(AuditEventType.DataAccess, searchResults[0].EventType);
    }

    [Fact]
    public async Task CleanupOldAuditEventsAsync_ReturnsExpectedCount()
    {
        // Arrange
        var userId = "user123";
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        await _service.LogDataAccessAsync(userId, "record2", DataCategory.Personal, "Test");

        // Act
        var cleanupCount = await _service.CleanupOldAuditEventsAsync(30);

        // Assert
        // Since events were just created, they shouldn't be cleaned up
        Assert.Equal(0, cleanupCount);
    }

    [Fact]
    public async Task ArchiveAuditEventsAsync_ReturnsSuccess()
    {
        // Arrange
        var userId = "user123";
        await _service.LogDataAccessAsync(userId, "record1", DataCategory.Personal, "Test");
        
        var beforeDate = DateTime.UtcNow.AddDays(1); // Future date to include current events
        var archivePath = "/tmp/archive.json";

        // Act
        var result = await _service.ArchiveAuditEventsAsync(beforeDate, archivePath);

        // Assert
        Assert.True(result);
    }
}