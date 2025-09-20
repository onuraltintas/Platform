using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Tests;

public class PrivacyTests
{
    [Fact]
    public void PrivacySettings_CanBeCreated()
    {
        // Arrange & Act
        var settings = new PrivacySettings();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.Anonymization);
        Assert.NotNull(settings.ConsentManagement);
        Assert.NotNull(settings.DataRetention);
        Assert.NotNull(settings.GdprCompliance);
        Assert.NotNull(settings.AuditLogging);
    }

    [Fact]
    public void ConsentRecord_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var record = new ConsentRecord();

        // Assert
        Assert.NotNull(record.Id);
        Assert.NotEmpty(record.Id);
        Assert.Equal(ConsentStatus.Pending, record.Status);
        Assert.True(record.GrantedAt >= DateTime.MinValue);
    }

    [Fact]
    public void PersonalDataRecord_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var record = new PersonalDataRecord();

        // Assert
        Assert.NotNull(record.Id);
        Assert.NotEmpty(record.Id);
        Assert.Equal(ProcessingStatus.Active, record.ProcessingStatus);
        Assert.Equal(AnonymizationLevel.None, record.AnonymizationLevel);
    }

    [Fact]
    public void DataCategory_EnumHasExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(DataCategory), DataCategory.Personal));
        Assert.True(Enum.IsDefined(typeof(DataCategory), DataCategory.Sensitive));
        Assert.True(Enum.IsDefined(typeof(DataCategory), DataCategory.Financial));
        Assert.True(Enum.IsDefined(typeof(DataCategory), DataCategory.Health));
    }

    [Fact]
    public void ConsentPurpose_EnumHasExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(ConsentPurpose), ConsentPurpose.Marketing));
        Assert.True(Enum.IsDefined(typeof(ConsentPurpose), ConsentPurpose.Analytics));
        Assert.True(Enum.IsDefined(typeof(ConsentPurpose), ConsentPurpose.Essential));
        Assert.True(Enum.IsDefined(typeof(ConsentPurpose), ConsentPurpose.Functional));
    }
}