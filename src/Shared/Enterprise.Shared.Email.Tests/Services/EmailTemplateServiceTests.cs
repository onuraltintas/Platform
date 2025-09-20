namespace Enterprise.Shared.Email.Tests.Services;

public class EmailTemplateServiceTests
{
    private readonly Mock<IOptions<EmailConfiguration>> _configurationMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<EmailTemplateService>> _loggerMock;
    private readonly EmailConfiguration _configuration;
    private readonly EmailTemplateService _templateService;
    private readonly string _tempDirectory;

    public EmailTemplateServiceTests()
    {
        _configurationMock = new Mock<IOptions<EmailConfiguration>>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<EmailTemplateService>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        _configuration = new EmailConfiguration
        {
            Templates = new TemplateOptions
            {
                Provider = TemplateProvider.FileSystem,
                DirectoryPath = _tempDirectory,
                FileExtension = ".liquid",
                EnableCaching = true,
                CacheExpirationMinutes = 60,
                WatchFileChanges = false // Disabled for testing
            }
        };

        _configurationMock.Setup(x => x.Value).Returns(_configuration);

        // Setup cache mock
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupAllProperties();
        _cacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

        _templateService = new EmailTemplateService(
            _configurationMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);

        Directory.CreateDirectory(_tempDirectory);
    }

    private void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        _templateService?.Dispose();
    }

    [Fact]
    public async Task GetTemplateAsync_ShouldReturnTemplate_WhenTemplateExists()
    {
        // Arrange
        const string templateName = "test-template";
        await CreateTestTemplateFile(templateName, "Test Subject", "Test Body");

        // Act
        var template = await _templateService.GetTemplateAsync(templateName);

        // Assert
        template.Should().NotBeNull();
        template!.Name.Should().Be(templateName);
        template.Subject.Should().Be("Test Subject");
        template.Body.Should().Be("Test Body");
    }

    [Fact]
    public async Task GetTemplateAsync_ShouldReturnNull_WhenTemplateDoesNotExist()
    {
        // Act
        var template = await _templateService.GetTemplateAsync("non-existent-template");

        // Assert
        template.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateAsync_ShouldReturnFromCache_WhenCached()
    {
        // Arrange
        const string templateName = "cached-template";
        var cachedTemplate = new EmailTemplate
        {
            Name = templateName,
            Subject = "Cached Subject",
            Body = "Cached Body"
        };

        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupAllProperties();
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
            .Returns((object key, out object? value) =>
            {
                value = cachedTemplate;
                return true;
            });

        // Act
        var template = await _templateService.GetTemplateAsync(templateName);

        // Assert
        template.Should().BeSameAs(cachedTemplate);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldCreateTemplate_WhenValid()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "new-template",
            Subject = "New Subject",
            Body = "New Body"
        };

        // Act
        var result = await _templateService.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        template.Id.Should().NotBeNullOrEmpty();
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldReturnFailure_WhenTemplateIsNull()
    {
        // Act
        var result = await _templateService.CreateTemplateAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Template cannot be null");
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldReturnFailure_WhenTemplateIsInvalid()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "", // Invalid
            Subject = "Subject",
            Body = "Body"
        };

        // Act
        var result = await _templateService.CreateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Template validation failed");
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldUpdateTemplate_WhenExists()
    {
        // Arrange
        const string templateName = "update-template";
        await CreateTestTemplateFile(templateName, "Old Subject", "Old Body");
        
        // Load the template first
        await _templateService.GetTemplateAsync(templateName);

        var updatedTemplate = new EmailTemplate
        {
            Name = templateName,
            Subject = "Updated Subject",
            Body = "Updated Body"
        };

        // Act
        var result = await _templateService.UpdateTemplateAsync(updatedTemplate);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        updatedTemplate.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldReturnFailure_WhenTemplateDoesNotExist()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "non-existent-template",
            Subject = "Subject",
            Body = "Body"
        };

        // Act
        var result = await _templateService.UpdateTemplateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("does not exist");
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldDeleteTemplate_WhenExists()
    {
        // Arrange
        const string templateName = "delete-template";
        await CreateTestTemplateFile(templateName, "Subject", "Body");
        
        // Load the template first
        await _templateService.GetTemplateAsync(templateName);

        // Act
        var result = await _templateService.DeleteTemplateAsync(templateName);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify template is deleted
        var deletedTemplate = await _templateService.GetTemplateAsync(templateName);
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldRenderTemplate_WithValidData()
    {
        // Arrange
        const string templateName = "render-template";
        await CreateTestTemplateFile(templateName, "Hello {{name}}!", "Welcome {{name}} to {{company}}!");
        
        var templateData = new { name = "John", company = "Test Corp" };

        // Act
        var result = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RenderedSubject.Should().Be("Hello John!");
        result.RenderedBody.Should().Be("Welcome John to Test Corp!");
    }

    [Fact]
    public async Task RenderTemplateAsync_ShouldReturnFailure_WhenTemplateNotFound()
    {
        // Act
        var result = await _templateService.RenderTemplateAsync("non-existent", new { });

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task RenderTemplateContentAsync_ShouldRenderContent()
    {
        // Arrange
        const string subject = "Hello {{name}}!";
        const string body = "Welcome {{name}} to {{company}}!";
        var data = new { name = "John", company = "Test Corp" };

        // Act
        var result = await _templateService.RenderTemplateContentAsync(subject, body, data);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.RenderedSubject.Should().Be("Hello John!");
        result.RenderedBody.Should().Be("Welcome John to Test Corp!");
        result.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void ExtractTemplateVariables_ShouldExtractVariables()
    {
        // Arrange
        const string content = "Hello {{name}}, your order {{order_id}} is ready! Total: {{amount}}.";

        // Act
        var variables = _templateService.ExtractTemplateVariables(content);

        // Assert
        variables.Should().Contain("name");
        variables.Should().Contain("order_id");
        variables.Should().Contain("amount");
        variables.Should().HaveCount(3);
    }

    [Fact]
    public void ValidateTemplateSyntax_ShouldReturnSuccess_ForValidSyntax()
    {
        // Arrange
        const string content = "Hello {{name}}, today is {{date}}!";

        // Act
        var result = _templateService.ValidateTemplateSyntax(content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTemplateSyntax_ShouldReturnFailure_ForInvalidSyntax()
    {
        // Arrange
        const string content = "Hello {{name, invalid syntax!";

        // Act
        var result = _templateService.ValidateTemplateSyntax(content);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_ShouldReturnTemplatesInCategory()
    {
        // Arrange
        await CreateTestTemplateFile("template1", "Subject 1", "Body 1");
        await CreateTestTemplateFile("template2", "Subject 2", "Body 2");
        
        // Load templates to trigger file system loading
        await _templateService.GetAllTemplatesAsync();

        // Act
        var templates = await _templateService.GetTemplatesByCategoryAsync("General");

        // Assert
        templates.Should().NotBeNull();
        templates.Should().HaveCount(2); // Default category is "General"
    }

    [Fact]
    public async Task SearchTemplatesAsync_ShouldReturnTemplatesWithTags()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "tagged-template",
            Subject = "Tagged Subject",
            Body = "Tagged Body",
            Tags = new List<string> { "marketing", "welcome" }
        };

        await _templateService.CreateTemplateAsync(template);

        // Act
        var templates = await _templateService.SearchTemplatesAsync(new[] { "marketing" });

        // Assert
        templates.Should().NotBeNull();
        templates.Should().ContainSingle();
        templates.First().Name.Should().Be("tagged-template");
    }

    [Fact]
    public async Task ImportTemplatesAsync_ShouldImportTemplatesFromDirectory()
    {
        // Arrange
        var importDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(importDir);

        try
        {
            var templateFile = Path.Combine(importDir, "import-template.liquid");
            await File.WriteAllTextAsync(templateFile, "Import Subject\nImport Body Content");

            // Act
            var result = await _templateService.ImportTemplatesAsync(importDir);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(importDir))
            {
                Directory.Delete(importDir, true);
            }
        }
    }

    [Fact]
    public async Task ExportTemplatesAsync_ShouldExportTemplatesToDirectory()
    {
        // Arrange
        var exportDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var template = new EmailTemplate
        {
            Name = "export-template",
            Subject = "Export Subject",
            Body = "Export Body"
        };

        await _templateService.CreateTemplateAsync(template);

        try
        {
            // Act
            var result = await _templateService.ExportTemplatesAsync(exportDir);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            Directory.Exists(exportDir).Should().BeTrue();
            File.Exists(Path.Combine(exportDir, "export-template.liquid")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(exportDir))
            {
                Directory.Delete(exportDir, true);
            }
        }
    }

    private async Task CreateTestTemplateFile(string name, string subject, string body)
    {
        var filePath = Path.Combine(_tempDirectory, $"{name}.liquid");
        var content = $"{subject}\n{body}";
        await File.WriteAllTextAsync(filePath, content);
    }
}