using Enterprise.Shared.Notifications.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Models;

public class NotificationTemplateTests
{
    [Fact]
    public void NotificationTemplate_Should_Initialize_With_Default_Values()
    {
        // Act
        var template = new NotificationTemplate();

        // Assert
        template.Id.Should().Be(0);
        template.Key.Should().BeEmpty();
        template.Language.Should().BeEmpty();
        template.Name.Should().BeEmpty();
        template.Description.Should().BeEmpty();
        template.SubjectTemplate.Should().BeEmpty();
        template.HtmlTemplate.Should().BeEmpty();
        template.TextTemplate.Should().BeEmpty();
        template.SmsTemplate.Should().BeNull();
        template.PushTitleTemplate.Should().BeNull();
        template.PushBodyTemplate.Should().BeNull();
        template.Category.Should().Be("General");
        template.Version.Should().Be(1);
        template.IsActive.Should().BeTrue();
        template.IsDefault.Should().BeFalse();
        template.RequiredFields.Should().BeEmpty();
        template.OptionalFields.Should().BeEmpty();
        template.SampleData.Should().BeEmpty();
        template.Tags.Should().BeEmpty();
        template.Metadata.Should().BeEmpty();
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        template.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationTemplate_Should_Set_Properties_Correctly()
    {
        // Arrange
        var requiredFields = new List<string> { "user.name", "company.name" };
        var optionalFields = new List<string> { "user.email" };
        var sampleData = new Dictionary<string, object> { { "user.name", "John Doe" } };
        var tags = new List<string> { "welcome", "onboarding" };
        var metadata = new Dictionary<string, object> { { "category", "user" } };

        // Act
        var template = new NotificationTemplate
        {
            Id = 1,
            Key = "welcome-template",
            Language = "en-US",
            Name = "Welcome Template",
            Description = "Welcome new users",
            SubjectTemplate = "Welcome {{ user.name }}!",
            HtmlTemplate = "<h1>Welcome {{ user.name }}!</h1>",
            TextTemplate = "Welcome {{ user.name }}!",
            SmsTemplate = "Welcome {{ user.name }}",
            PushTitleTemplate = "Welcome!",
            PushBodyTemplate = "Hi {{ user.name }}",
            Category = "Welcome",
            Version = 2,
            IsActive = false,
            IsDefault = true,
            RequiredFields = requiredFields,
            OptionalFields = optionalFields,
            SampleData = sampleData,
            Tags = tags,
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            Metadata = metadata
        };

        // Assert
        template.Id.Should().Be(1);
        template.Key.Should().Be("welcome-template");
        template.Language.Should().Be("en-US");
        template.Name.Should().Be("Welcome Template");
        template.Description.Should().Be("Welcome new users");
        template.SubjectTemplate.Should().Be("Welcome {{ user.name }}!");
        template.HtmlTemplate.Should().Be("<h1>Welcome {{ user.name }}!</h1>");
        template.TextTemplate.Should().Be("Welcome {{ user.name }}!");
        template.SmsTemplate.Should().Be("Welcome {{ user.name }}");
        template.PushTitleTemplate.Should().Be("Welcome!");
        template.PushBodyTemplate.Should().Be("Hi {{ user.name }}");
        template.Category.Should().Be("Welcome");
        template.Version.Should().Be(2);
        template.IsActive.Should().BeFalse();
        template.IsDefault.Should().BeTrue();
        template.RequiredFields.Should().BeEquivalentTo(requiredFields);
        template.OptionalFields.Should().BeEquivalentTo(optionalFields);
        template.SampleData.Should().BeEquivalentTo(sampleData);
        template.Tags.Should().BeEquivalentTo(tags);
        template.CreatedBy.Should().NotBeNull();
        template.UpdatedBy.Should().NotBeNull();
        template.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void NotificationTemplate_Should_Fail_Validation_For_Invalid_Key(string key)
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = key,
            Language = "en-US",
            Name = "Test Template",
            SubjectTemplate = "Test Subject",
            TextTemplate = "Test Content"
        };

        // Act
        var validationResults = ValidateModel(template);

        // Assert
        validationResults.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(NotificationTemplate.Key));
    }

    [Fact]
    public void NotificationTemplate_Should_Fail_Validation_For_Too_Long_Key()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = new string('a', 101), // Exceeds 100 character limit
            Language = "en-US",
            Name = "Test Template",
            SubjectTemplate = "Test Subject",
            TextTemplate = "Test Content"
        };

        // Act
        var validationResults = ValidateModel(template);

        // Assert
        validationResults.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(NotificationTemplate.Key));
    }

    [Fact]
    public void NotificationTemplate_Should_Pass_Validation_For_Valid_Data()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "valid-template",
            Language = "en-US",
            Name = "Valid Template",
            SubjectTemplate = "Valid Subject",
            TextTemplate = "Valid Content"
        };

        // Act
        var validationResults = ValidateModel(template);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}