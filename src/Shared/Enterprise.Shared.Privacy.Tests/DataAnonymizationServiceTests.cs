using Enterprise.Shared.Privacy.Anonymization;
using Enterprise.Shared.Privacy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Enterprise.Shared.Privacy.Tests;

public class DataAnonymizationServiceTests
{
    private readonly Mock<ILogger<DataAnonymizationService>> _loggerMock;
    private readonly Mock<IOptions<PrivacySettings>> _optionsMock;
    private readonly DataAnonymizationService _service;
    private readonly PrivacySettings _settings;

    public DataAnonymizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataAnonymizationService>>();
        _optionsMock = new Mock<IOptions<PrivacySettings>>();
        
        _settings = new PrivacySettings
        {
            Anonymization = new AnonymizationSettings
            {
                EnableAnonymization = true,
                HashingSalt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-salt-12345")),
                HashingIterations = 10000,
                EncryptionKey = Convert.ToBase64String(new byte[32]) // 256-bit key
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _service = new DataAnonymizationService(_optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HashDataAsync_WithValidData_ReturnsConsistentHash()
    {
        // Arrange
        var testData = "test@example.com";

        // Act
        var hash1 = await _service.HashDataAsync(testData);
        var hash2 = await _service.HashDataAsync(testData);

        // Assert
        Assert.NotEmpty(hash1);
        Assert.Equal(hash1, hash2); // Same input should produce same hash
    }

    [Fact]
    public async Task HashDataAsync_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = await _service.HashDataAsync("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task EncryptDataAsync_WithValidData_ReturnsEncryptedData()
    {
        // Arrange
        var testData = "sensitive information";

        // Act
        var encrypted = await _service.EncryptDataAsync(testData);

        // Assert
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(testData, encrypted);
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_ReturnsOriginalData()
    {
        // Arrange
        var originalData = "This is sensitive data that needs encryption";

        // Act
        var encrypted = await _service.EncryptDataAsync(originalData);
        var decrypted = await _service.DecryptDataAsync(encrypted);

        // Assert
        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public async Task MaskDataAsync_WithEmail_MasksCorrectly()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var masked = await _service.MaskDataAsync(email, DataCategory.Personal);

        // Assert
        Assert.NotEqual(email, masked);
        Assert.Contains("@example.com", masked);
        Assert.Contains("*", masked);
    }

    [Fact]
    public async Task PseudonymizeAsync_WithSameInputAndUser_ReturnsConsistentResult()
    {
        // Arrange
        var data = "John Doe";
        var userId = "user123";

        // Act
        var pseudonym1 = await _service.PseudonymizeAsync(data, userId);
        var pseudonym2 = await _service.PseudonymizeAsync(data, userId);

        // Assert
        Assert.Equal(pseudonym1, pseudonym2);
        Assert.StartsWith("USER_", pseudonym1);
    }

    [Fact]
    public async Task AnonymizeAsync_WithDifferentLevels_ReturnsAppropriateResults()
    {
        // Arrange
        var testData = "test@example.com";

        // Act & Assert
        var noneResult = await _service.AnonymizeAsync(testData, AnonymizationLevel.None);
        Assert.Equal(testData, noneResult);

        var maskedResult = await _service.AnonymizeAsync(testData, AnonymizationLevel.Masked);
        Assert.NotEqual(testData, maskedResult);

        var hashedResult = await _service.AnonymizeAsync(testData, AnonymizationLevel.Hashed);
        Assert.NotEqual(testData, hashedResult);

        var deletedResult = await _service.AnonymizeAsync(testData, AnonymizationLevel.Deleted);
        Assert.Equal(string.Empty, deletedResult);
    }

    [Fact]
    public async Task AnonymizePersonalDataRecordAsync_UpdatesRecordCorrectly()
    {
        // Arrange
        var record = new PersonalDataRecord
        {
            UserId = "user123",
            Category = DataCategory.Personal,
            DataType = "Email",
            OriginalValue = "test@example.com"
        };

        // Act
        var anonymizedRecord = await _service.AnonymizePersonalDataRecordAsync(
            record, AnonymizationLevel.Masked);

        // Assert
        Assert.Equal(record.Id, anonymizedRecord.Id);
        Assert.Equal(record.UserId, anonymizedRecord.UserId);
        Assert.Equal(AnonymizationLevel.Masked, anonymizedRecord.AnonymizationLevel);
        Assert.NotEqual(record.OriginalValue, anonymizedRecord.ProcessedValue);
        Assert.Contains("AnonymizedAt", anonymizedRecord.Metadata);
    }

    [Fact]
    public async Task BulkAnonymizeAsync_WithMultipleRecords_ProcessesAll()
    {
        // Arrange
        var records = new[]
        {
            new PersonalDataRecord { UserId = "user1", OriginalValue = "data1" },
            new PersonalDataRecord { UserId = "user2", OriginalValue = "data2" },
            new PersonalDataRecord { UserId = "user3", OriginalValue = "data3" }
        };

        // Act
        var results = await _service.BulkAnonymizeAsync(records, AnonymizationLevel.Hashed);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(AnonymizationLevel.Hashed, r.AnonymizationLevel));
        Assert.All(results, r => Assert.NotNull(r.ProcessedValue));
    }

    [Fact]
    public void IsDataAnonymized_WithVariousInputs_ReturnsCorrectResults()
    {
        // Act & Assert
        Assert.False(_service.IsDataAnonymized("normal text"));
        Assert.True(_service.IsDataAnonymized("USER_12345"));
        Assert.True(_service.IsDataAnonymized("data with * masks"));
        Assert.True(_service.IsDataAnonymized("ANON_12345"));
    }

    [Theory]
    [InlineData(AnonymizationLevel.None, true)]
    [InlineData(AnonymizationLevel.Encrypted, true)]
    [InlineData(AnonymizationLevel.Masked, false)]
    [InlineData(AnonymizationLevel.Hashed, false)]
    [InlineData(AnonymizationLevel.Pseudonymized, false)]
    [InlineData(AnonymizationLevel.Anonymized, false)]
    [InlineData(AnonymizationLevel.Deleted, false)]
    public void CanReverseAnonymization_WithDifferentLevels_ReturnsExpectedResults(
        AnonymizationLevel level, bool expected)
    {
        // Act
        var result = _service.CanReverseAnonymization(level);

        // Assert
        Assert.Equal(expected, result);
    }
}