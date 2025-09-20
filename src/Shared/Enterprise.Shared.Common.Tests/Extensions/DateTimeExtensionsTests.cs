using Enterprise.Shared.Common.Extensions;
using FluentAssertions;

namespace Enterprise.Shared.Common.Tests.Extensions;

[TestFixture]
public class DateTimeExtensionsTests
{
    #region Age and Time Span Tests

    [Test]
    public void CalculateAge_WithBirthDate25YearsAgo_Returns25()
    {
        // Arrange
        var birthDate = DateTime.Today.AddYears(-25);

        // Act
        var age = birthDate.CalculateAge();

        // Assert
        age.Should().Be(25);
    }

    [Test]
    public void CalculateAge_WithBirthdayNotYetThisYear_ReturnsCorrectAge()
    {
        // Arrange
        var birthDate = DateTime.Today.AddYears(-25).AddDays(1); // Tomorrow's date 25 years ago

        // Act
        var age = birthDate.CalculateAge();

        // Assert
        age.Should().Be(24); // Should be 24 if birthday hasn't occurred this year
    }

    [Test]
    public void CalculateAge_WithFutureDate_ReturnsZero()
    {
        // Arrange
        var futureDate = DateTime.Today.AddDays(1);

        // Act
        var age = futureDate.CalculateAge();

        // Assert
        age.Should().Be(0);
    }

    [Test]
    public void TimeAgo_WithDateOneHourAgo_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var timeAgo = dateTime.TimeAgo();

        // Assert
        timeAgo.TotalHours.Should().BeApproximately(1, 0.1);
    }

    [Test]
    public void ToTimeAgoString_WithDateOneMinuteAgo_ReturnsMinuteAgo()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = dateTime.ToTimeAgoString();

        // Assert
        result.Should().Be("1 minute ago");
    }

    [Test]
    public void ToTimeAgoString_WithDateTwoHoursAgo_ReturnsHoursAgo()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddHours(-2);

        // Act
        var result = dateTime.ToTimeAgoString();

        // Assert
        result.Should().Be("2 hours ago");
    }

    [Test]
    public void ToTimeAgoString_WithDateOneDayAgo_ReturnsDayAgo()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = dateTime.ToTimeAgoString();

        // Assert
        result.Should().Be("1 day ago");
    }

    [Test]
    public void ToTimeAgoString_WithRecentDate_ReturnsJustNow()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddSeconds(-30);

        // Act
        var result = dateTime.ToTimeAgoString();

        // Assert
        result.Should().Be("Just now");
    }

    #endregion

    #region Business Day Tests

    [TestCase("2024-01-06", true)] // Saturday
    [TestCase("2024-01-07", true)] // Sunday
    [TestCase("2024-01-08", false)] // Monday
    [TestCase("2024-01-09", false)] // Tuesday
    [TestCase("2024-01-10", false)] // Wednesday
    [TestCase("2024-01-11", false)] // Thursday
    [TestCase("2024-01-12", false)] // Friday
    public void IsWeekend_WithVariousDays_ReturnsExpectedResult(string dateString, bool expected)
    {
        // Arrange
        var date = DateTime.Parse(dateString);

        // Act
        var result = date.IsWeekend();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public void NextBusinessDay_FromFriday_ReturnsMonday()
    {
        // Arrange
        var friday = new DateTime(2024, 1, 12); // Friday

        // Act
        var nextBusinessDay = friday.NextBusinessDay();

        // Assert
        nextBusinessDay.Should().Be(new DateTime(2024, 1, 15)); // Monday
    }

    [Test]
    public void PreviousBusinessDay_FromMonday_ReturnsFriday()
    {
        // Arrange
        var monday = new DateTime(2024, 1, 15); // Monday

        // Act
        var previousBusinessDay = monday.PreviousBusinessDay();

        // Assert
        previousBusinessDay.Should().Be(new DateTime(2024, 1, 12)); // Friday
    }

    [Test]
    public void AddBusinessDays_WithPositiveDays_ReturnsCorrectDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 10); // Wednesday
        var businessDaysToAdd = 5;

        // Act
        var result = startDate.AddBusinessDays(businessDaysToAdd);

        // Assert
        result.Should().Be(new DateTime(2024, 1, 17)); // Next Wednesday (skipping weekend)
    }

    [Test]
    public void AddBusinessDays_WithNegativeDays_ReturnsCorrectDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 17); // Wednesday
        var businessDaysToSubtract = -5;

        // Act
        var result = startDate.AddBusinessDays(businessDaysToSubtract);

        // Assert
        result.Should().Be(new DateTime(2024, 1, 10)); // Previous Wednesday (skipping weekend)
    }

    #endregion

    #region Date Range Tests

    [Test]
    public void StartOfDay_WithDateTime_ReturnsDateWithMidnight()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 14, 30, 45);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 15, 0, 0, 0));
    }

    [Test]
    public void EndOfDay_WithDateTime_ReturnsDateWithEndOfDay()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 14, 30, 45);

        // Act
        var result = dateTime.EndOfDay();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 15, 23, 59, 59, 999));
    }

    [Test]
    public void StartOfWeek_WithWednesday_ReturnsMonday()
    {
        // Arrange
        var wednesday = new DateTime(2024, 1, 17); // Wednesday

        // Act
        var result = wednesday.StartOfWeek();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 15)); // Monday
    }

    [Test]
    public void EndOfWeek_WithWednesday_ReturnsSunday()
    {
        // Arrange
        var wednesday = new DateTime(2024, 1, 17); // Wednesday

        // Act
        var result = wednesday.EndOfWeek();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 21, 23, 59, 59, 999)); // Sunday end of day
    }

    [Test]
    public void StartOfMonth_WithMidMonth_ReturnsFirstOfMonth()
    {
        // Arrange
        var midMonth = new DateTime(2024, 1, 15);

        // Act
        var result = midMonth.StartOfMonth();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 1));
    }

    [Test]
    public void EndOfMonth_WithMidMonth_ReturnsLastOfMonth()
    {
        // Arrange
        var midMonth = new DateTime(2024, 1, 15);

        // Act
        var result = midMonth.EndOfMonth();

        // Assert
        result.Should().Be(new DateTime(2024, 1, 31, 23, 59, 59, 999));
    }

    #endregion

    #region Quarter Tests

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 1)]
    [TestCase(4, 2)]
    [TestCase(5, 2)]
    [TestCase(6, 2)]
    [TestCase(7, 3)]
    [TestCase(8, 3)]
    [TestCase(9, 3)]
    [TestCase(10, 4)]
    [TestCase(11, 4)]
    [TestCase(12, 4)]
    public void GetQuarter_WithVariousMonths_ReturnsExpectedQuarter(int month, int expectedQuarter)
    {
        // Arrange
        var date = new DateTime(2024, month, 1);

        // Act
        var result = date.GetQuarter();

        // Assert
        result.Should().Be(expectedQuarter);
    }

    [Test]
    public void StartOfQuarter_WithJulyDate_ReturnsJuly1st()
    {
        // Arrange
        var julyDate = new DateTime(2024, 7, 15);

        // Act
        var result = julyDate.StartOfQuarter();

        // Assert
        result.Should().Be(new DateTime(2024, 7, 1));
    }

    [Test]
    public void EndOfQuarter_WithJulyDate_ReturnsSeptember30th()
    {
        // Arrange
        var julyDate = new DateTime(2024, 7, 15);

        // Act
        var result = julyDate.EndOfQuarter();

        // Assert
        result.Should().Be(new DateTime(2024, 9, 30, 23, 59, 59, 999));
    }

    #endregion

    #region Validation Tests

    [Test]
    public void IsBetween_WithDateInRange_ReturnsTrue()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = date.IsBetween(startDate, endDate);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsBetween_WithDateOutOfRange_ReturnsFalse()
    {
        // Arrange
        var date = new DateTime(2024, 2, 15);
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = date.IsBetween(startDate, endDate);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsToday_WithTodaysDate_ReturnsTrue()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var result = today.IsToday();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsYesterday_WithYesterdaysDate_ReturnsTrue()
    {
        // Arrange
        var yesterday = DateTime.Today.AddDays(-1);

        // Act
        var result = yesterday.IsYesterday();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsTomorrow_WithTomorrowsDate_ReturnsTrue()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);

        // Act
        var result = tomorrow.IsTomorrow();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Formatting Tests

    [Test]
    public void ToIso8601String_WithDateTime_ReturnsCorrectFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45, 123, DateTimeKind.Utc);

        // Act
        var result = dateTime.ToIso8601String();

        // Assert
        // Turkey is UTC+3, so 10:30 UTC becomes 13:30 Turkey time
        result.Should().Be("2024-01-15T13:30:45.123+03:00");
    }

    [Test]
    public void ToFileNameString_WithDateTime_ReturnsFileNameFriendlyFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);

        // Act
        var result = dateTime.ToFileNameString();

        // Assert
        result.Should().Be("2024-01-15_10-30-45");
    }

    [Test]
    public void ToSortableString_WithDateTime_ReturnsSortableFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);

        // Act
        var result = dateTime.ToSortableString();

        // Assert
        result.Should().Be("20240115103045");
    }

    #endregion

    #region Unix Timestamp Tests

    [Test]
    public void ToUnixTimestamp_WithEpochTime_ReturnsZero()
    {
        // Arrange
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = epoch.ToUnixTimestamp();

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public void FromUnixTimestamp_WithZero_ReturnsEpochTime()
    {
        // Arrange
        var unixTimestamp = 0L;

        // Act
        var result = DateTimeExtensions.FromUnixTimestamp(unixTimestamp);

        // Assert
        result.Should().Be(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region Time Zone Tests

    [Test]
    public void ToSafeUtc_WithLocalDateTime_ReturnsUtcDateTime()
    {
        // Arrange
        var localDateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Local);

        // Act
        var result = localDateTime.ToSafeUtc();

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void ToSafeUtc_WithUtcDateTime_ReturnsUnchanged()
    {
        // Arrange
        var utcDateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var result = utcDateTime.ToSafeUtc();

        // Assert
        result.Should().Be(utcDateTime);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region Holiday Tests

    [Test]
    public void IsNewYear_WithNewYearsDay_ReturnsTrue()
    {
        // Arrange
        var newYear = new DateTime(2024, 1, 1);

        // Act
        var result = newYear.IsNewYear();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsChristmas_WithChristmasDay_ReturnsTrue()
    {
        // Arrange
        var christmas = new DateTime(2024, 12, 25);

        // Act
        var result = christmas.IsChristmas();

        // Assert
        result.Should().BeTrue();
    }

    [TestCase(2024, true)] // 2024 is a leap year
    [TestCase(2023, false)] // 2023 is not a leap year
    [TestCase(2000, true)] // 2000 is a leap year (divisible by 400)
    [TestCase(1900, false)] // 1900 is not a leap year (divisible by 100 but not 400)
    public void IsLeapYear_WithVariousYears_ReturnsExpectedResult(int year, bool expected)
    {
        // Arrange
        var date = new DateTime(year, 1, 1);

        // Act
        var result = date.IsLeapYear();

        // Assert
        result.Should().Be(expected);
    }

    [TestCase(1, 31)] // January
    [TestCase(2, 29)] // February in leap year 2024
    [TestCase(4, 30)] // April
    [TestCase(12, 31)] // December
    public void GetDaysInMonth_WithVariousMonths_ReturnsExpectedResult(int month, int expectedDays)
    {
        // Arrange
        var date = new DateTime(2024, month, 1); // 2024 is a leap year

        // Act
        var result = date.GetDaysInMonth();

        // Assert
        result.Should().Be(expectedDays);
    }

    #endregion
}