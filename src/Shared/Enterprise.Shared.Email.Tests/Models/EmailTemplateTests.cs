namespace Enterprise.Shared.Email.Tests.Models;

public class EmailTemplateTests
{
    [Fact]
    public void GetTemplateVariables_ShouldExtractVariables_FromSubjectAndBody()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Subject = "Hello {{name}}, your order {{orderId}} is ready!",
            Body = "Dear {{name}}, your order with ID {{orderId}} has been processed. Total: {{amount}}."
        };

        // Act
        var variables = template.GetTemplateVariables();

        // Assert
        variables.Should().Contain("name");
        variables.Should().Contain("orderId");
        variables.Should().Contain("amount");
        variables.Should().HaveCount(3);
    }

    [Fact]
    public void GetTemplateVariables_ShouldReturnEmpty_WhenNoVariables()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Subject = "Static subject",
            Body = "Static body with no variables"
        };

        // Act
        var variables = template.GetTemplateVariables();

        // Assert
        variables.Should().BeEmpty();
    }

    [Fact]
    public void GetTemplateVariables_ShouldHandleWhitespace_InVariableNames()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Subject = "Hello {{ name }}, welcome!",
            Body = "Your code is {{  verification_code  }}"
        };

        // Act
        var variables = template.GetTemplateVariables();

        // Assert
        variables.Should().Contain("name");
        variables.Should().Contain("verification_code");
        variables.Should().HaveCount(2);
    }

    [Fact]
    public void GetTemplateVariables_ShouldNotDuplicateVariables()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Subject = "Hello {{name}}!",
            Body = "Dear {{name}}, this is for {{name}}."
        };

        // Act
        var variables = template.GetTemplateVariables();

        // Assert
        variables.Should().ContainSingle("name");
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidTemplate()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "welcome-email",
            Subject = "Welcome {{name}}!",
            Body = "Hello {{name}}, welcome to our platform!"
        };

        // Act
        var isValid = template.IsValid(out var errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNameIsEmpty()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        // Act
        var isValid = template.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Template name is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenSubjectIsEmpty()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "test-template",
            Subject = "",
            Body = "Test Body"
        };

        // Act
        var isValid = template.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Template subject is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenBodyIsEmpty()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "test-template",
            Subject = "Test Subject",
            Body = ""
        };

        // Act
        var isValid = template.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("Template body is required");
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForMultipleErrors()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Name = "",
            Subject = "",
            Body = ""
        };

        // Act
        var isValid = template.IsValid(out var errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().HaveCountGreaterThan(1);
        errors.Should().Contain("Template name is required");
        errors.Should().Contain("Template subject is required");
        errors.Should().Contain("Template body is required");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var template = new EmailTemplate();

        // Assert
        template.Id.Should().BeEmpty();
        template.Name.Should().BeEmpty();
        template.DisplayName.Should().BeEmpty();
        template.Subject.Should().BeEmpty();
        template.Body.Should().BeEmpty();
        template.IsHtml.Should().BeTrue();
        template.Category.Should().Be("General");
        template.Language.Should().Be("en-US");
        template.Version.Should().Be("1.0.0");
        template.IsActive.Should().BeTrue();
        template.Tags.Should().NotBeNull().And.BeEmpty();
        template.Variables.Should().NotBeNull().And.BeEmpty();
        template.Metadata.Should().NotBeNull().And.BeEmpty();
        template.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        template.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("General")]
    [InlineData("Marketing")]
    [InlineData("Transactional")]
    [InlineData("Notifications")]
    public void Category_ShouldAcceptValidCategories(string category)
    {
        // Arrange & Act
        var template = new EmailTemplate
        {
            Category = category
        };

        // Assert
        template.Category.Should().Be(category);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void Language_ShouldAcceptValidLanguageCodes(string language)
    {
        // Arrange & Act
        var template = new EmailTemplate
        {
            Language = language
        };

        // Assert
        template.Language.Should().Be(language);
    }

    [Fact]
    public void TemplateVariable_ShouldInitializeWithDefaults()
    {
        // Act
        var variable = new TemplateVariable();

        // Assert
        variable.Name.Should().BeEmpty();
        variable.DisplayName.Should().BeEmpty();
        variable.Description.Should().BeNull();
        variable.Type.Should().Be(TemplateVariableType.String);
        variable.IsRequired.Should().BeTrue();
        variable.DefaultValue.Should().BeNull();
        variable.ValidationRules.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(TemplateVariableType.String)]
    [InlineData(TemplateVariableType.Number)]
    [InlineData(TemplateVariableType.Boolean)]
    [InlineData(TemplateVariableType.Date)]
    [InlineData(TemplateVariableType.DateTime)]
    [InlineData(TemplateVariableType.Array)]
    [InlineData(TemplateVariableType.Object)]
    public void TemplateVariableType_ShouldSupportAllTypes(TemplateVariableType type)
    {
        // Arrange & Act
        var variable = new TemplateVariable
        {
            Type = type
        };

        // Assert
        variable.Type.Should().Be(type);
    }
}