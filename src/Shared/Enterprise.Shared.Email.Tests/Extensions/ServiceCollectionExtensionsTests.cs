namespace Enterprise.Shared.Email.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly ServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailService:DefaultSender:Email"] = "test@example.com",
                ["EmailService:DefaultSender:Name"] = "Test Sender",
                ["EmailService:Smtp:Host"] = "smtp.example.com",
                ["EmailService:Smtp:Port"] = "587",
                ["EmailService:Smtp:EnableSsl"] = "true",
                ["EmailService:Templates:Provider"] = "FileSystem",
                ["EmailService:Templates:DirectoryPath"] = "Templates/Email"
            });
        
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public void AddEmailService_WithConfiguration_ShouldRegisterServices()
    {
        // Act
        _services.AddEmailService(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
        serviceProvider.GetService<IEmailTemplateService>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<EmailConfiguration>>().Should().NotBeNull();
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
    }

    [Fact]
    public void AddEmailService_WithConfigurationSection_ShouldRegisterServices()
    {
        // Arrange
        var section = _configuration.GetSection("EmailService");

        // Act
        _services.AddEmailService(section);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
        serviceProvider.GetService<IEmailTemplateService>().Should().NotBeNull();
        
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig.Should().NotBeNull();
        emailConfig!.Value.DefaultSender.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void AddEmailService_WithConfigureOptions_ShouldRegisterServices()
    {
        // Act
        _services.AddEmailService(options =>
        {
            options.DefaultSender.Email = "configured@example.com";
            options.DefaultSender.Name = "Configured Sender";
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig.Should().NotBeNull();
        emailConfig!.Value.DefaultSender.Email.Should().Be("configured@example.com");
        emailConfig.Value.DefaultSender.Name.Should().Be("Configured Sender");
    }

    [Fact]
    public void AddEmailService_WithEmailConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var configuration = new EmailConfiguration
        {
            DefaultSender = new EmailSenderOptions
            {
                Email = "direct@example.com",
                Name = "Direct Sender"
            }
        };

        // Act
        _services.AddEmailService(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig.Should().NotBeNull();
        emailConfig!.Value.DefaultSender.Email.Should().Be("direct@example.com");
    }

    [Fact]
    public void AddSmtpEmailService_WithConfiguration_ShouldRegisterFluentEmail()
    {
        // Act
        _services.AddSmtpEmailService(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IFluentEmail>().Should().NotBeNull();
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
    }

    [Fact]
    public void AddSmtpEmailService_WithManualConfiguration_ShouldRegisterServices()
    {
        // Act
        _services.AddSmtpEmailService(
            smtpHost: "smtp.manual.com",
            smtpPort: 587,
            username: "user",
            password: "pass",
            enableSsl: true,
            defaultFromEmail: "manual@example.com",
            defaultFromName: "Manual Sender");
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IFluentEmail>().Should().NotBeNull();
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
        
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.Smtp.Host.Should().Be("smtp.manual.com");
        emailConfig.Value.DefaultSender.Email.Should().Be("manual@example.com");
    }

    [Fact]
    public void AddFileSystemTemplates_ShouldConfigureTemplateOptions()
    {
        // Arrange
        const string templateDirectory = "/custom/templates";

        // Act
        _services.AddEmailService(options => { })
                .AddFileSystemTemplates(templateDirectory);
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.Templates.Provider.Should().Be(TemplateProvider.FileSystem);
        emailConfig.Value.Templates.DirectoryPath.Should().Be(templateDirectory);
    }

    [Fact]
    public void AddMemoryTemplates_ShouldConfigureTemplateOptions()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            new() { Name = "template1", Subject = "Subject1", Body = "Body1" },
            new() { Name = "template2", Subject = "Subject2", Body = "Body2" }
        };

        // Act
        _services.AddEmailService(options => { })
                .AddMemoryTemplates(templates);
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.Templates.Provider.Should().Be(TemplateProvider.Memory);
        
        var templateCollection = serviceProvider.GetService<IEnumerable<EmailTemplate>>();
        templateCollection.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public void ConfigureBulkProcessing_ShouldSetBulkOptions()
    {
        // Act
        _services.AddEmailService(options => { })
                .ConfigureBulkProcessing(
                    defaultBatchSize: 100,
                    defaultMaxConcurrency: 20,
                    defaultDelayBetweenBatchesMs: 2000);
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.BulkProcessing.DefaultBatchSize.Should().Be(100);
        emailConfig.Value.BulkProcessing.DefaultMaxConcurrency.Should().Be(20);
        emailConfig.Value.BulkProcessing.DefaultDelayBetweenBatchesMs.Should().Be(2000);
    }

    [Fact]
    public void ConfigureRetryPolicy_ShouldSetRetryOptions()
    {
        // Act
        _services.AddEmailService(options => { })
                .ConfigureRetryPolicy(
                    maxAttempts: 5,
                    delayMs: 2000,
                    useExponentialBackoff: false);
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.Retry.Enabled.Should().BeTrue();
        emailConfig.Value.Retry.MaxAttempts.Should().Be(5);
        emailConfig.Value.Retry.DelayMs.Should().Be(2000);
        emailConfig.Value.Retry.UseExponentialBackoff.Should().BeFalse();
    }

    [Fact]
    public void ConfigureRateLimit_ShouldSetRateLimitOptions()
    {
        // Act
        _services.AddEmailService(options => { })
                .ConfigureRateLimit(
                    emailsPerMinute: 200,
                    emailsPerHour: 2000,
                    emailsPerDay: 20000);
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.RateLimit.Enabled.Should().BeTrue();
        emailConfig.Value.RateLimit.EmailsPerMinute.Should().Be(200);
        emailConfig.Value.RateLimit.EmailsPerHour.Should().Be(2000);
        emailConfig.Value.RateLimit.EmailsPerDay.Should().Be(20000);
    }

    [Fact]
    public void AddDevelopmentEmailService_ShouldConfigureForDevelopment()
    {
        // Act
        _services.AddDevelopmentEmailService();
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IFluentEmail>().Should().NotBeNull();
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
        
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig!.Value.DefaultSender.Email.Should().Be("dev@localhost");
        emailConfig.Value.DefaultSender.Name.Should().Be("Development");
    }

    [Fact]
    public void AddEmailService_ShouldRegisterHealthCheck()
    {
        // Act
        _services.AddEmailService(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void ServiceRegistration_ShouldSupportMultipleConfigurations()
    {
        // Act
        _services.AddEmailService(_configuration)
                .ConfigureBulkProcessing(defaultBatchSize: 200)
                .ConfigureRetryPolicy(maxAttempts: 5)
                .ConfigureRateLimit(emailsPerMinute: 50)
                .AddFileSystemTemplates("/custom/path");
        
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var emailConfig = serviceProvider.GetService<IOptions<EmailConfiguration>>();
        emailConfig.Should().NotBeNull();
        emailConfig!.Value.BulkProcessing.DefaultBatchSize.Should().Be(200);
        emailConfig.Value.Retry.MaxAttempts.Should().Be(5);
        emailConfig.Value.RateLimit.EmailsPerMinute.Should().Be(50);
        emailConfig.Value.Templates.DirectoryPath.Should().Be("/custom/path");
    }
}

public class EmailServiceHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenEmailServiceIsHealthy()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Success());

        var healthCheck = new EmailServiceHealthCheck(mockEmailService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Email service is healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenEmailServiceFails()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Failure("Connection failed"));

        var healthCheck = new EmailServiceHealthCheck(mockEmailService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenExceptionThrown()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Test exception"));

        var healthCheck = new EmailServiceHealthCheck(mockEmailService.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Test exception");
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenEmailServiceIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new EmailServiceHealthCheck(null!));
        exception.ParamName.Should().Be("emailService");
    }
}