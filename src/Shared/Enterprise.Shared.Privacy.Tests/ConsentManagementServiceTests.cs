using Enterprise.Shared.Privacy.Consent;
using Enterprise.Shared.Privacy.Interfaces;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Enterprise.Shared.Privacy.Tests;

public class ConsentManagementServiceTests
{
    private readonly Mock<ILogger<ConsentManagementService>> _loggerMock;
    private readonly Mock<IOptions<PrivacySettings>> _optionsMock;
    private readonly Mock<IPrivacyAuditService> _auditServiceMock;
    private readonly ConsentManagementService _service;
    private readonly PrivacySettings _settings;

    public ConsentManagementServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConsentManagementService>>();
        _optionsMock = new Mock<IOptions<PrivacySettings>>();
        _auditServiceMock = new Mock<IPrivacyAuditService>();
        
        _settings = new PrivacySettings
        {
            ConsentManagement = new ConsentManagementSettings
            {
                EnableConsentTracking = true,
                ConsentExpirationDays = 365,
                RequireExplicitConsent = true
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _service = new ConsentManagementService(_optionsMock.Object, _loggerMock.Object, _auditServiceMock.Object);
    }

    [Fact]
    public async Task GrantConsentAsync_WithValidRequest_ReturnsConsentRecord()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent,
            Source = "Web"
        };

        // Act
        var result = await _service.GrantConsentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.UserId, result.UserId);
        Assert.Equal(request.UserEmail, result.UserEmail);
        Assert.Equal(ConsentStatus.Granted, result.Status);
        Assert.True(result.IsActive);
        
        // Verify audit service was called
        _auditServiceMock.Verify(x => x.LogConsentChangeAsync(
            request.UserId, 
            request.Purposes[0], 
            ConsentStatus.Pending, 
            ConsentStatus.Granted, 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantConsentAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.GrantConsentAsync(null!));
    }

    [Fact]
    public async Task GrantConsentAsync_WithEmptyPurposes_ThrowsArgumentException()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = Array.Empty<ConsentPurpose>(),
            LegalBasis = LegalBasis.Consent
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GrantConsentAsync(request));
    }

    [Fact]
    public async Task GrantMultipleConsentsAsync_WithMultiplePurposes_ReturnsMultipleRecords()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing, ConsentPurpose.Analytics, ConsentPurpose.Functional },
            LegalBasis = LegalBasis.Consent
        };

        // Act
        var results = await _service.GrantMultipleConsentsAsync(request);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(ConsentStatus.Granted, r.Status));
        Assert.All(results, r => Assert.Equal(request.UserId, r.UserId));
    }

    [Fact]
    public async Task WithdrawConsentAsync_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var grantRequest = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent
        };
        
        await _service.GrantConsentAsync(grantRequest);

        var withdrawalRequest = new ConsentWithdrawalRequest
        {
            UserId = "user123",
            Purposes = new[] { ConsentPurpose.Marketing },
            Reason = "User request"
        };

        // Act
        var result = await _service.WithdrawConsentAsync(withdrawalRequest);

        // Assert
        Assert.True(result);
        
        // Verify consent status changed
        var consent = await _service.GetConsentAsync("user123", ConsentPurpose.Marketing);
        Assert.Equal(ConsentStatus.Withdrawn, consent!.Status);
        Assert.False(consent.IsActive);
    }

    [Fact]
    public async Task WithdrawConsentAsync_WithNoActiveConsents_ReturnsFalse()
    {
        // Arrange
        var withdrawalRequest = new ConsentWithdrawalRequest
        {
            UserId = "user123",
            Purposes = new[] { ConsentPurpose.Marketing },
            Reason = "User request"
        };

        // Act
        var result = await _service.WithdrawConsentAsync(withdrawalRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetConsentAsync_WithExistingConsent_ReturnsConsent()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Analytics },
            LegalBasis = LegalBasis.Consent
        };
        
        await _service.GrantConsentAsync(request);

        // Act
        var result = await _service.GetConsentAsync("user123", ConsentPurpose.Analytics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user123", result.UserId);
        Assert.Equal(ConsentPurpose.Analytics, result.Purpose);
        
        // Verify audit service was called for data access
        _auditServiceMock.Verify(x => x.LogDataAccessAsync(
            "user123", 
            result.Id, 
            DataCategory.Personal, 
            "ConsentManagement", 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConsentAsync_WithNonExistentConsent_ReturnsNull()
    {
        // Act
        var result = await _service.GetConsentAsync("nonexistent", ConsentPurpose.Marketing);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HasValidConsentAsync_WithActiveConsent_ReturnsTrue()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Essential },
            LegalBasis = LegalBasis.Consent
        };
        
        await _service.GrantConsentAsync(request);

        // Act
        var result = await _service.HasValidConsentAsync("user123", ConsentPurpose.Essential);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasValidConsentAsync_WithExpiredConsent_ReturnsFalse()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent,
            ExpiryDate = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };
        
        await _service.GrantConsentAsync(request);

        // Act
        var result = await _service.HasValidConsentAsync("user123", ConsentPurpose.Marketing);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetConsentSummaryAsync_WithMultipleConsents_ReturnsCorrectSummary()
    {
        // Arrange
        var requests = new[]
        {
            new ConsentRequest
            {
                UserId = "user123",
                UserEmail = "test@example.com",
                Purposes = new[] { ConsentPurpose.Marketing },
                LegalBasis = LegalBasis.Consent
            },
            new ConsentRequest
            {
                UserId = "user123",
                UserEmail = "test@example.com",
                Purposes = new[] { ConsentPurpose.Analytics },
                LegalBasis = LegalBasis.Consent
            }
        };

        foreach (var request in requests)
        {
            await _service.GrantConsentAsync(request);
        }

        // Act
        var summary = await _service.GetConsentSummaryAsync("user123");

        // Assert
        Assert.Equal("user123", summary.UserId);
        Assert.Equal(2, summary.TotalConsents);
        Assert.Equal(2, summary.ActiveConsents);
        Assert.Equal(0, summary.WithdrawnConsents);
        Assert.Equal(2, summary.ConsentsByPurpose.Count);
    }

    [Fact]
    public async Task ProcessExpiredConsentsAsync_WithExpiredConsents_ProcessesCorrectly()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent,
            ExpiryDate = DateTime.UtcNow.AddDays(-1) // Expired
        };
        
        await _service.GrantConsentAsync(request);

        // Act
        var processedCount = await _service.ProcessExpiredConsentsAsync();

        // Assert
        Assert.Equal(1, processedCount);
        
        // Verify consent is now expired
        var consent = await _service.GetConsentAsync("user123", ConsentPurpose.Marketing);
        Assert.Equal(ConsentStatus.Expired, consent!.Status);
    }

    [Fact]
    public async Task GetExpiringConsentsAsync_WithExpiringConsents_ReturnsCorrectConsents()
    {
        // Arrange
        var expiringRequest = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent,
            ExpiryDate = DateTime.UtcNow.AddDays(15) // Expires in 15 days
        };
        
        var nonExpiringRequest = new ConsentRequest
        {
            UserId = "user456",
            UserEmail = "test2@example.com",
            Purposes = new[] { ConsentPurpose.Analytics },
            LegalBasis = LegalBasis.Consent,
            ExpiryDate = DateTime.UtcNow.AddDays(60) // Expires in 60 days
        };

        await _service.GrantConsentAsync(expiringRequest);
        await _service.GrantConsentAsync(nonExpiringRequest);

        // Act
        var expiringConsents = await _service.GetExpiringConsentsAsync(30);

        // Assert
        Assert.Single(expiringConsents);
        Assert.Equal("user123", expiringConsents[0].UserId);
    }

    [Fact]
    public async Task ValidateConsentRequirementsAsync_WhenRequiredAndValid_ReturnsTrue()
    {
        // Arrange
        _settings.ConsentManagement.RequireExplicitConsent = true;
        
        var request = new ConsentRequest
        {
            UserId = "user123",
            UserEmail = "test@example.com",
            Purposes = new[] { ConsentPurpose.Marketing },
            LegalBasis = LegalBasis.Consent
        };
        
        await _service.GrantConsentAsync(request);

        // Act
        var result = await _service.ValidateConsentRequirementsAsync("user123", new[] { ConsentPurpose.Marketing });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateConsentRequirementsAsync_WhenNotRequired_ReturnsTrue()
    {
        // Arrange
        _settings.ConsentManagement.RequireExplicitConsent = false;

        // Act
        var result = await _service.ValidateConsentRequirementsAsync("user123", new[] { ConsentPurpose.Marketing });

        // Assert
        Assert.True(result);
    }
}