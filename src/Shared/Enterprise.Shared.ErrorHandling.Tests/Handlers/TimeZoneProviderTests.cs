namespace Enterprise.Shared.ErrorHandling.Tests.Handlers;

public class TimeZoneProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeTurkeyTimeZone()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "Turkey Standard Time" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();

        // Act
        var provider = new TimeZoneProvider(options, logger);
        var timeZoneInfo = provider.GetTimeZoneInfo();

        // Assert
        timeZoneInfo.Should().NotBeNull();
        // UTC+3 for Turkey (either +3 hours offset or custom timezone)
        (timeZoneInfo.BaseUtcOffset.TotalHours == 3 || timeZoneInfo.DisplayName.Contains("Türkiye")).Should().BeTrue();
    }

    [Fact]
    public void GetCurrentTime_ShouldReturnTurkishTime()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "Turkey Standard Time" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();
        var provider = new TimeZoneProvider(options, logger);

        // Act
        var currentTime = provider.GetCurrentTime();

        // Assert
        currentTime.Should().BeAfter(DateTime.MinValue);
        currentTime.Should().BeBefore(DateTime.MaxValue);
        
        // Turkish time should be different from UTC (unless it's exactly midnight UTC)
        var utcNow = DateTime.UtcNow;
        var timeDifference = Math.Abs((currentTime - utcNow).TotalHours);
        
        // Allowing for small differences due to execution time
        (timeDifference >= 2.9 && timeDifference <= 3.1).Should().BeTrue();
    }

    [Fact]
    public void ConvertToLocalTime_WithUtcDateTime_ShouldConvertToTurkishTime()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "Turkey Standard Time" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();
        var provider = new TimeZoneProvider(options, logger);
        
        var utcDateTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var localTime = provider.ConvertToLocalTime(utcDateTime);

        // Assert
        var expectedTime = utcDateTime.AddHours(3); // Turkey is UTC+3
        localTime.Hour.Should().Be(expectedTime.Hour);
    }

    [Fact]
    public void ConvertToLocalTime_WithNonUtcDateTime_ShouldReturnAsIs()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "Turkey Standard Time" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();
        var provider = new TimeZoneProvider(options, logger);
        
        var localDateTime = new DateTime(2024, 6, 15, 15, 30, 0, DateTimeKind.Local);

        // Act
        var result = provider.ConvertToLocalTime(localDateTime);

        // Assert
        result.Should().Be(localDateTime);
    }

    [Fact]
    public void Constructor_WithInvalidTimeZone_ShouldFallbackToCustom()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "InvalidTimeZone" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();

        // Act
        var provider = new TimeZoneProvider(options, logger);
        var timeZone = provider.GetTimeZoneInfo();

        // Assert
        timeZone.Should().NotBeNull();
        
        // Should either be a custom timezone with +3 offset or one of the fallback timezones
        var isValidFallback = timeZone.BaseUtcOffset.TotalHours == 3 || 
                             timeZone.Id.Contains("Turkey") ||
                             timeZone.Id.Contains("TurkeyCustom") ||
                             timeZone.Id.Contains("+03");
        
        isValidFallback.Should().BeTrue();
        
        // Verify that warning logs were called (multiple calls expected due to fallback attempts)
        logger.ReceivedWithAnyArgs().LogWarning(default(Exception), default(string), default(object));
    }

    [Fact]
    public void GetTimeZoneInfo_ShouldReturnConfiguredTimeZone()
    {
        // Arrange
        var settings = new ErrorHandlingSettings 
        { 
            TimeZoneId = "Turkey Standard Time" 
        };
        var options = Options.Create(settings);
        var logger = Substitute.For<ILogger<TimeZoneProvider>>();
        var provider = new TimeZoneProvider(options, logger);

        // Act
        var timeZone = provider.GetTimeZoneInfo();

        // Assert
        timeZone.Should().NotBeNull();
        timeZone.BaseUtcOffset.TotalHours.Should().Be(3);
    }
}