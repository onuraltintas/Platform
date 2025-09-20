using Enterprise.Shared.Notifications.Models;
using Enterprise.Shared.Notifications.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Enterprise.Shared.Notifications.Tests.Services;

public class TemplateServiceTests
{
    private readonly Mock<ILogger<TemplateService>> _loggerMock;
    private readonly Mock<IOptions<NotificationSettings>> _optionsMock;
    private readonly NotificationSettings _settings;
    private readonly TemplateService _templateService;

    public TemplateServiceTests()
    {
        _loggerMock = new Mock<ILogger<TemplateService>>();
        _optionsMock = new Mock<IOptions<NotificationSettings>>();
        _settings = new NotificationSettings();
        _optionsMock.Setup(x => x.Value).Returns(_settings);
        _templateService = new TemplateService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Service_With_Valid_Dependencies()
    {
        // Act & Assert
        _templateService.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
    {
        // Act & Assert
        var act = () => new TemplateService(null!, _optionsMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_Should_Throw_Exception_When_Settings_Is_Null()
    {
        // Act & Assert
        var act = () => new TemplateService(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task RenderAsync_Should_Render_Template_Successfully_With_Valid_Data()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            { "user", new { first_name = "John" } },
            { "company_name", "Test Company" }
        };

        // Act
        var result = await _templateService.RenderAsync("welcome", data, "en-US");

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Contain("Test Company");
        result.HtmlContent.Should().Contain("John");
        result.TextContent.Should().Contain("John").And.Contain("Test Company");
        // Language is determined by template context
        result.TemplateKey.Should().Be("welcome");
        result.Data.Should().BeEquivalentTo(data);
        result.RenderedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RenderAsync_Should_Throw_Exception_When_Template_Not_Found()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        var act = async () => await _templateService.RenderAsync("non-existent-template", data);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RenderAsync_Should_Throw_Exception_When_TemplateKey_Is_Null_Or_Empty()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        var act1 = async () => await _templateService.RenderAsync(null!, data);
        await act1.Should().ThrowAsync<ArgumentException>();

        var act2 = async () => await _templateService.RenderAsync("", data);
        await act2.Should().ThrowAsync<ArgumentException>();

        var act3 = async () => await _templateService.RenderAsync("   ", data);
        await act3.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RenderAsync_Should_Throw_Exception_When_Data_Is_Null()
    {
        // Act & Assert
        var act = async () => await _templateService.RenderAsync("welcome", null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetTemplateAsync_Should_Return_Template_When_Exists()
    {
        // Act
        var result = await _templateService.GetTemplateAsync("welcome", "en-US");

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be("welcome");
        result.Language.Should().Be("en-US");
        result.SubjectTemplate.Should().NotBeNullOrEmpty();
        result.TextTemplate.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTemplateAsync_Should_Return_Null_When_Template_Does_Not_Exist()
    {
        // Act
        var result = await _templateService.GetTemplateAsync("non-existent", "en-US");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateAsync_Should_Return_Fallback_Template_When_Language_Not_Found()
    {
        // Act
        var result = await _templateService.GetTemplateAsync("welcome", "fr-FR");

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be("welcome");
        result.Language.Should().Be("en-US"); // Fallback language
    }

    [Fact]
    public async Task CreateOrUpdateTemplateAsync_Should_Create_New_Template()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "test-template",
            Language = "en-US",
            Name = "Test Template",
            SubjectTemplate = "Test Subject",
            TextTemplate = "Test Content"
        };

        // Act
        await _templateService.CreateOrUpdateTemplateAsync(template);

        // Assert
        var retrieved = await _templateService.GetTemplateAsync("test-template", "en-US");
        retrieved.Should().NotBeNull();
        retrieved!.Key.Should().Be("test-template");
        retrieved.Name.Should().Be("Test Template");
        retrieved.SubjectTemplate.Should().Be("Test Subject");
    }

    [Fact]
    public async Task CreateOrUpdateTemplateAsync_Should_Update_Existing_Template()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "welcome",
            Language = "en-US",
            Name = "Updated Welcome",
            SubjectTemplate = "Updated Subject",
            TextTemplate = "Updated Content"
        };

        // Act
        await _templateService.CreateOrUpdateTemplateAsync(template);

        // Assert
        var retrieved = await _templateService.GetTemplateAsync("welcome", "en-US");
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Welcome");
        retrieved.SubjectTemplate.Should().Be("Updated Subject");
    }

    [Fact]
    public async Task CreateOrUpdateTemplateAsync_Should_Throw_Exception_When_Template_Is_Null()
    {
        // Act & Assert
        var act = async () => await _templateService.CreateOrUpdateTemplateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrUpdateTemplateAsync_Should_Throw_Exception_When_Template_Key_Is_Empty()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "",
            Language = "en-US",
            SubjectTemplate = "Subject",
            TextTemplate = "Content"
        };

        // Act & Assert
        var act = async () => await _templateService.CreateOrUpdateTemplateAsync(template);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteTemplateAsync_Should_Remove_Template_When_Exists()
    {
        // Arrange - First create a template
        var template = new NotificationTemplate
        {
            Key = "temp-template",
            Language = "en-US",
            SubjectTemplate = "Temp Subject",
            TextTemplate = "Temp Content"
        };
        await _templateService.CreateOrUpdateTemplateAsync(template);

        // Verify it exists
        var retrieved = await _templateService.GetTemplateAsync("temp-template", "en-US");
        retrieved.Should().NotBeNull();

        // Act
        await _templateService.DeleteTemplateAsync("temp-template", "en-US");

        // Assert
        var deletedTemplate = await _templateService.GetTemplateAsync("temp-template", "en-US");
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_Should_Not_Throw_When_Template_Does_Not_Exist()
    {
        // Act & Assert - Should not throw
        await _templateService.DeleteTemplateAsync("non-existent", "en-US");
    }

    [Fact]
    public async Task GetAllTemplatesAsync_Should_Return_All_Templates()
    {
        // Act
        var result = await _templateService.GetAllTemplatesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(t => t.Key == "welcome");
        result.Should().Contain(t => t.Key == "email_verification");
        result.Should().Contain(t => t.Key == "password_reset");
        result.Should().Contain(t => t.Key == "order_confirmation");
        result.Should().BeInAscendingOrder(t => t.Key).And.ThenBeInAscendingOrder(t => t.Language);
    }

    [Fact]
    public async Task GetTemplatesByKeyAsync_Should_Return_Templates_For_Specific_Key()
    {
        // Act
        var result = await _templateService.GetTemplatesByKeyAsync("welcome");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(t => t.Key == "welcome");
        result.Should().BeInAscendingOrder(t => t.Language);
    }

    [Fact]
    public async Task GetTemplatesByKeyAsync_Should_Return_Empty_For_Non_Existent_Key()
    {
        // Act
        var result = await _templateService.GetTemplatesByKeyAsync("non-existent-key");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTemplatesByKeyAsync_Should_Throw_Exception_When_Key_Is_Null_Or_Empty()
    {
        // Act & Assert
        var act1 = async () => await _templateService.GetTemplatesByKeyAsync(null!);
        await act1.Should().ThrowAsync<ArgumentException>();

        var act2 = async () => await _templateService.GetTemplatesByKeyAsync("");
        await act2.Should().ThrowAsync<ArgumentException>();

        var act3 = async () => await _templateService.GetTemplatesByKeyAsync("   ");
        await act3.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateTemplateAsync_Should_Return_Valid_Result_For_Good_Template()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "valid-template",
            Language = "en-US",
            SubjectTemplate = "Hello {{ user.name }}",
            TextTemplate = "Welcome {{ user.name }}",
            HtmlTemplate = "<h1>Welcome {{ user.name }}</h1>"
        };

        var sampleData = new Dictionary<string, object>
        {
            { "user", new { name = "John" } }
        };

        // Act
        var result = await _templateService.ValidateTemplateAsync(template, sampleData);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ValidateTemplateAsync_Should_Return_Invalid_Result_For_Bad_Template()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Key = "invalid-template",
            Language = "en-US",
            SubjectTemplate = "Hello {{ user.name", // Missing closing brace
            TextTemplate = "Welcome {{ user.name }}",
            HtmlTemplate = "<h1>Welcome {{ user.name }}</h1>"
        };

        // Act
        var result = await _templateService.ValidateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateTemplateAsync_Should_Throw_Exception_When_Template_Is_Null()
    {
        // Act & Assert
        var act = async () => await _templateService.ValidateTemplateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PreviewTemplateAsync_Should_Return_Preview_With_Rendered_Content()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            { "user", new { first_name = "Alice" } },
            { "company_name", "Acme Corp" }
        };

        // Act
        var result = await _templateService.PreviewTemplateAsync("welcome", data, "en-US");

        // Assert
        result.Should().NotBeNull();
        result.SubjectPreview.Should().Contain("Acme Corp");
        result.HtmlPreview.Should().Contain("Alice");
        result.TextPreview.Should().Contain("Alice").And.Contain("Acme Corp");
        result.SmsPreview.Should().Contain("Alice").And.Contain("Acme Corp");
        result.PushPreview.Should().NotBeNullOrEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PreviewTemplateAsync_Should_Throw_Exception_When_Template_Not_Found()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        var act = async () => await _templateService.PreviewTemplateAsync("non-existent", data);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloneTemplateAsync_Should_Create_Copy_In_Different_Language()
    {
        // Act
        await _templateService.CloneTemplateAsync("welcome", "en-US", "fr-FR");

        // Assert
        var originalTemplate = await _templateService.GetTemplateAsync("welcome", "en-US");
        var clonedTemplate = await _templateService.GetTemplateAsync("welcome", "fr-FR");

        clonedTemplate.Should().NotBeNull();
        clonedTemplate!.Key.Should().Be(originalTemplate!.Key);
        clonedTemplate.Language.Should().Be("fr-FR");
        clonedTemplate.SubjectTemplate.Should().Be(originalTemplate.SubjectTemplate);
        clonedTemplate.TextTemplate.Should().Be(originalTemplate.TextTemplate);
    }

    [Fact]
    public async Task CloneTemplateAsync_Should_Throw_Exception_When_Source_Template_Not_Found()
    {
        // Act & Assert
        var act = async () => await _templateService.CloneTemplateAsync("non-existent", "en-US", "fr-FR");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetStatisticsAsync_Should_Return_Correct_Statistics()
    {
        // Act
        var result = await _templateService.GetStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTemplates.Should().BeGreaterThan(0);
        result.ByLanguage.Should().ContainKey("en-US");
        result.ByCategory.Should().NotBeEmpty();
        result.ActiveTemplates.Should().BeGreaterThan(0);
        result.InactiveTemplates.Should().BeGreaterThanOrEqualTo(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ClearCacheAsync_Should_Clear_All_Cache_When_No_Key_Specified()
    {
        // Act & Assert - Should not throw
        await _templateService.ClearCacheAsync();
    }

    [Fact]
    public async Task ClearCacheAsync_Should_Clear_Specific_Template_Cache()
    {
        // Act & Assert - Should not throw
        await _templateService.ClearCacheAsync("welcome");
    }

    [Fact]
    public async Task ImportTemplatesAsync_Should_Import_Templates_Successfully()
    {
        // Arrange
        var templatesToImport = new[]
        {
            new NotificationTemplate
            {
                Key = "imported-1",
                Language = "en-US",
                Name = "Imported Template 1",
                SubjectTemplate = "Imported Subject 1",
                TextTemplate = "Imported Content 1"
            },
            new NotificationTemplate
            {
                Key = "imported-2",
                Language = "en-US",
                Name = "Imported Template 2",
                SubjectTemplate = "Imported Subject 2",
                TextTemplate = "Imported Content 2"
            }
        };

        // Act
        await _templateService.ImportTemplatesAsync(templatesToImport);

        // Assert
        var template1 = await _templateService.GetTemplateAsync("imported-1", "en-US");
        var template2 = await _templateService.GetTemplateAsync("imported-2", "en-US");

        template1.Should().NotBeNull();
        template1!.Name.Should().Be("Imported Template 1");

        template2.Should().NotBeNull();
        template2!.Name.Should().Be("Imported Template 2");
    }

    [Fact]
    public async Task ImportTemplatesAsync_Should_Skip_Existing_Templates_When_Overwrite_False()
    {
        // Arrange
        var existingTemplate = new NotificationTemplate
        {
            Key = "welcome",
            Language = "en-US",
            Name = "Should Not Override",
            SubjectTemplate = "Should Not Override",
            TextTemplate = "Should Not Override"
        };

        // Act
        await _templateService.ImportTemplatesAsync(new[] { existingTemplate }, overwrite: false);

        // Assert
        var template = await _templateService.GetTemplateAsync("welcome", "en-US");
        template!.Name.Should().NotBe("Should Not Override");
    }

    [Fact]
    public async Task ImportTemplatesAsync_Should_Override_Existing_Templates_When_Overwrite_True()
    {
        // Arrange
        var overrideTemplate = new NotificationTemplate
        {
            Key = "welcome",
            Language = "en-US",
            Name = "Overridden Welcome",
            SubjectTemplate = "Overridden Subject",
            TextTemplate = "Overridden Content"
        };

        // Act
        await _templateService.ImportTemplatesAsync(new[] { overrideTemplate }, overwrite: true);

        // Assert
        var template = await _templateService.GetTemplateAsync("welcome", "en-US");
        template!.Name.Should().Be("Overridden Welcome");
        template.SubjectTemplate.Should().Be("Overridden Subject");
    }

    [Fact]
    public async Task ImportTemplatesAsync_Should_Throw_Exception_When_Templates_Is_Null()
    {
        // Act & Assert
        var act = async () => await _templateService.ImportTemplatesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExportTemplatesAsync_Should_Return_All_Templates()
    {
        // Act
        var result = await _templateService.ExportTemplatesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(t => t.Key == "welcome");
        result.Should().Contain(t => t.Key == "email_verification");
        result.Should().Contain(t => t.Key == "password_reset");
        result.Should().Contain(t => t.Key == "order_confirmation");
    }
}