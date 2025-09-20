namespace Enterprise.Shared.Email.Tests.Configuration;

public class EmailConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new EmailConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.DefaultSender.Should().NotBeNull();
        config.Smtp.Should().NotBeNull();
        config.Templates.Should().NotBeNull();
        config.BulkProcessing.Should().NotBeNull();
        config.Retry.Should().NotBeNull();
        config.RateLimit.Should().NotBeNull();
        config.Logging.Should().NotBeNull();
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Assert
        EmailConfiguration.SectionName.Should().Be("EmailService");
    }
}

public class EmailSenderOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new EmailSenderOptions();

        // Assert
        options.Email.Should().BeEmpty();
        options.Name.Should().BeEmpty();
        options.ReplyTo.Should().BeNull();
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    public void Email_ShouldAcceptValidEmailAddresses(string email)
    {
        // Arrange & Act
        var options = new EmailSenderOptions
        {
            Email = email
        };

        // Assert
        options.Email.Should().Be(email);
    }
}

public class SmtpOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new SmtpOptions();

        // Assert
        options.Host.Should().BeEmpty();
        options.Port.Should().Be(587);
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.EnableSsl.Should().BeTrue();
        options.UseDefaultCredentials.Should().BeFalse();
        options.TimeoutMs.Should().Be(30000);
        options.MaxConnections.Should().Be(10);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(587)]
    [InlineData(465)]
    [InlineData(2525)]
    public void Port_ShouldAcceptValidPorts(int port)
    {
        // Arrange & Act
        var options = new SmtpOptions
        {
            Port = port
        };

        // Assert
        options.Port.Should().Be(port);
    }

    [Theory]
    [InlineData("smtp.gmail.com")]
    [InlineData("localhost")]
    [InlineData("mail.company.com")]
    public void Host_ShouldAcceptValidHostnames(string host)
    {
        // Arrange & Act
        var options = new SmtpOptions
        {
            Host = host
        };

        // Assert
        options.Host.Should().Be(host);
    }
}

public class TemplateOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new TemplateOptions();

        // Assert
        options.Provider.Should().Be(TemplateProvider.FileSystem);
        options.DirectoryPath.Should().Be("Templates/Email");
        options.FileExtension.Should().Be(".liquid");
        options.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(60);
        options.WatchFileChanges.Should().BeTrue();
    }

    [Theory]
    [InlineData(TemplateProvider.FileSystem)]
    [InlineData(TemplateProvider.Database)]
    [InlineData(TemplateProvider.Memory)]
    public void Provider_ShouldAcceptAllValidProviders(TemplateProvider provider)
    {
        // Arrange & Act
        var options = new TemplateOptions
        {
            Provider = provider
        };

        // Assert
        options.Provider.Should().Be(provider);
    }

    [Theory]
    [InlineData(".liquid")]
    [InlineData(".html")]
    [InlineData(".txt")]
    public void FileExtension_ShouldAcceptValidExtensions(string extension)
    {
        // Arrange & Act
        var options = new TemplateOptions
        {
            FileExtension = extension
        };

        // Assert
        options.FileExtension.Should().Be(extension);
    }
}

public class BulkProcessingOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new BulkProcessingOptions();

        // Assert
        options.DefaultBatchSize.Should().Be(50);
        options.DefaultMaxConcurrency.Should().Be(10);
        options.DefaultDelayBetweenBatchesMs.Should().Be(1000);
        options.MaxBatchSize.Should().Be(1000);
        options.MaxConcurrency.Should().Be(50);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public void DefaultBatchSize_ShouldAcceptValidValues(int batchSize)
    {
        // Arrange & Act
        var options = new BulkProcessingOptions
        {
            DefaultBatchSize = batchSize
        };

        // Assert
        options.DefaultBatchSize.Should().Be(batchSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void DefaultMaxConcurrency_ShouldAcceptValidValues(int concurrency)
    {
        // Arrange & Act
        var options = new BulkProcessingOptions
        {
            DefaultMaxConcurrency = concurrency
        };

        // Assert
        options.DefaultMaxConcurrency.Should().Be(concurrency);
    }
}

public class RetryOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new RetryOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.MaxAttempts.Should().Be(3);
        options.DelayMs.Should().Be(1000);
        options.UseExponentialBackoff.Should().BeTrue();
        options.MaxDelayMs.Should().Be(30000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(10)]
    public void MaxAttempts_ShouldAcceptValidValues(int maxAttempts)
    {
        // Arrange & Act
        var options = new RetryOptions
        {
            MaxAttempts = maxAttempts
        };

        // Assert
        options.MaxAttempts.Should().Be(maxAttempts);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void DelayMs_ShouldAcceptValidValues(int delay)
    {
        // Arrange & Act
        var options = new RetryOptions
        {
            DelayMs = delay
        };

        // Assert
        options.DelayMs.Should().Be(delay);
    }
}

public class RateLimitOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new RateLimitOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.EmailsPerMinute.Should().Be(100);
        options.EmailsPerHour.Should().Be(1000);
        options.EmailsPerDay.Should().Be(10000);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void EmailsPerMinute_ShouldAcceptValidValues(int emailsPerMinute)
    {
        // Arrange & Act
        var options = new RateLimitOptions
        {
            EmailsPerMinute = emailsPerMinute
        };

        // Assert
        options.EmailsPerMinute.Should().Be(emailsPerMinute);
    }
}

public class LoggingOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new LoggingOptions();

        // Assert
        options.LogSuccessfulSends.Should().BeTrue();
        options.LogFailedSends.Should().BeTrue();
        options.LogEmailContent.Should().BeFalse();
        options.LogTemplateRendering.Should().BeFalse();
        options.LogPerformanceMetrics.Should().BeTrue();
    }

    [Fact]
    public void AllLoggingOptions_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new LoggingOptions
        {
            LogSuccessfulSends = false,
            LogFailedSends = false,
            LogEmailContent = true,
            LogTemplateRendering = true,
            LogPerformanceMetrics = false
        };

        // Assert
        options.LogSuccessfulSends.Should().BeFalse();
        options.LogFailedSends.Should().BeFalse();
        options.LogEmailContent.Should().BeTrue();
        options.LogTemplateRendering.Should().BeTrue();
        options.LogPerformanceMetrics.Should().BeFalse();
    }
}