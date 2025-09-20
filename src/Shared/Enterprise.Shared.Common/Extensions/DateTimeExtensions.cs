using System.Globalization;

namespace Enterprise.Shared.Common.Extensions;

/// <summary>
/// Extension methods for DateTime manipulation and formatting
/// </summary>
public static class DateTimeExtensions
{
    #region Age and Time Span Extensions

    /// <summary>
    /// Calculates age in years from the given date to now
    /// </summary>
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        // Subtract one year if birthday hasn't occurred this year
        if (birthDate.Date > today.AddYears(-age))
            age--;

        return Math.Max(0, age);
    }

    /// <summary>
    /// Calculates age in years from the given date to the specified date
    /// </summary>
    public static int CalculateAge(this DateTime birthDate, DateTime asOfDate)
    {
        var age = asOfDate.Year - birthDate.Year;

        if (birthDate.Date > asOfDate.AddYears(-age))
            age--;

        return Math.Max(0, age);
    }

    /// <summary>
    /// Gets the time elapsed since the specified date (Turkey timezone)
    /// </summary>
    public static TimeSpan TimeAgo(this DateTime dateTime)
    {
        var nowInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(DateTime.UtcNow);
        var dateInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(dateTime);
            
        return nowInTurkey - dateInTurkey;
    }

    /// <summary>
    /// Gets a human-readable "time ago" string
    /// </summary>
    public static string ToTimeAgoString(this DateTime dateTime, bool includeSeconds = false)
    {
        var timeSpan = dateTime.TimeAgo();

        return timeSpan.TotalDays switch
        {
            >= 365 => $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago",
            >= 30 => $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago",
            >= 7 => $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) == 1 ? "" : "s")} ago",
            >= 1 => $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago",
            _ => timeSpan.TotalHours switch
            {
                >= 1 => $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago",
                _ => timeSpan.TotalMinutes switch
                {
                    >= 1 => $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago",
                    _ => includeSeconds ? $"{(int)timeSpan.TotalSeconds} second{((int)timeSpan.TotalSeconds == 1 ? "" : "s")} ago" : "Just now"
                }
            }
        };
    }

    #endregion

    #region Business Day Calculations

    /// <summary>
    /// Checks if the date is a weekend (Saturday or Sunday)
    /// </summary>
    public static bool IsWeekend(this DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>
    /// Checks if the date is a weekday (Monday through Friday)
    /// </summary>
    public static bool IsWeekday(this DateTime date)
    {
        return !date.IsWeekend();
    }

    /// <summary>
    /// Gets the next business day (excluding weekends)
    /// </summary>
    public static DateTime NextBusinessDay(this DateTime date)
    {
        var nextDay = date.AddDays(1);
        while (nextDay.IsWeekend())
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    /// <summary>
    /// Gets the previous business day (excluding weekends)
    /// </summary>
    public static DateTime PreviousBusinessDay(this DateTime date)
    {
        var prevDay = date.AddDays(-1);
        while (prevDay.IsWeekend())
        {
            prevDay = prevDay.AddDays(-1);
        }
        return prevDay;
    }

    /// <summary>
    /// Adds business days to the date (excluding weekends)
    /// </summary>
    public static DateTime AddBusinessDays(this DateTime date, int businessDays)
    {
        if (businessDays == 0) return date;

        var direction = businessDays > 0 ? 1 : -1;
        var remainingDays = Math.Abs(businessDays);
        var currentDate = date;

        while (remainingDays > 0)
        {
            currentDate = currentDate.AddDays(direction);
            if (currentDate.IsWeekday())
            {
                remainingDays--;
            }
        }

        return currentDate;
    }

    #endregion

    #region Date Range Extensions

    /// <summary>
    /// Gets the start of the day (00:00:00)
    /// </summary>
    public static DateTime StartOfDay(this DateTime date)
    {
        return date.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999)
    /// </summary>
    public static DateTime EndOfDay(this DateTime date)
    {
        return date.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday)
    /// </summary>
    public static DateTime StartOfWeek(this DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gets the end of the week (Sunday)
    /// </summary>
    public static DateTime EndOfWeek(this DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        return date.StartOfWeek(startOfWeek).AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Gets the start of the month
    /// </summary>
    public static DateTime StartOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month
    /// </summary>
    public static DateTime EndOfMonth(this DateTime date)
    {
        return date.StartOfMonth().AddMonths(1).AddDays(-1).EndOfDay();
    }

    /// <summary>
    /// Gets the start of the year
    /// </summary>
    public static DateTime StartOfYear(this DateTime date)
    {
        return new DateTime(date.Year, 1, 1);
    }

    /// <summary>
    /// Gets the end of the year
    /// </summary>
    public static DateTime EndOfYear(this DateTime date)
    {
        return new DateTime(date.Year, 12, 31).EndOfDay();
    }

    #endregion

    #region Quarter Extensions

    /// <summary>
    /// Gets the quarter number (1-4) for the date
    /// </summary>
    public static int GetQuarter(this DateTime date)
    {
        return (date.Month - 1) / 3 + 1;
    }

    /// <summary>
    /// Gets the start of the quarter
    /// </summary>
    public static DateTime StartOfQuarter(this DateTime date)
    {
        var quarter = date.GetQuarter();
        var month = (quarter - 1) * 3 + 1;
        return new DateTime(date.Year, month, 1);
    }

    /// <summary>
    /// Gets the end of the quarter
    /// </summary>
    public static DateTime EndOfQuarter(this DateTime date)
    {
        return date.StartOfQuarter().AddMonths(3).AddDays(-1).EndOfDay();
    }

    #endregion

    #region Validation and Comparison Extensions

    /// <summary>
    /// Checks if the date is between two dates (inclusive)
    /// </summary>
    public static bool IsBetween(this DateTime date, DateTime startDate, DateTime endDate)
    {
        return date >= startDate && date <= endDate;
    }

    /// <summary>
    /// Checks if the date is in the past
    /// </summary>
    public static bool IsInPast(this DateTime date)
    {
        return date < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the date is in the future
    /// </summary>
    public static bool IsInFuture(this DateTime date)
    {
        return date > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the date is today (Turkey timezone)
    /// </summary>
    public static bool IsToday(this DateTime date)
    {
        var todayInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(DateTime.UtcNow).Date;
        var dateInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(date).Date;
            
        return dateInTurkey == todayInTurkey;
    }

    /// <summary>
    /// Checks if the date was yesterday (Turkey timezone)
    /// </summary>
    public static bool IsYesterday(this DateTime date)
    {
        var todayInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(DateTime.UtcNow).Date;
        var dateInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(date).Date;
            
        return dateInTurkey == todayInTurkey.AddDays(-1);
    }

    /// <summary>
    /// Checks if the date is tomorrow (Turkey timezone)
    /// </summary>
    public static bool IsTomorrow(this DateTime date)
    {
        var todayInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(DateTime.UtcNow).Date;
        var dateInTurkey = Utilities.TimeZoneHelper.ConvertToTurkeyTime(date).Date;
            
        return dateInTurkey == todayInTurkey.AddDays(1);
    }

    #endregion

    #region Formatting Extensions

    /// <summary>
    /// Formats date as ISO 8601 string (Turkey timezone)
    /// </summary>
    public static string ToIso8601String(this DateTime date)
    {
        var turkeyDateTime = Utilities.TimeZoneHelper.ConvertToTurkeyTime(date);
        return turkeyDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats date for file names (no invalid characters)
    /// </summary>
    public static string ToFileNameString(this DateTime date)
    {
        return date.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats date as a sortable string (yyyyMMddHHmmss)
    /// </summary>
    public static string ToSortableString(this DateTime date)
    {
        return date.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats date in a user-friendly way
    /// </summary>
    public static string ToFriendlyString(this DateTime date, bool includeTime = true)
    {
        if (date.IsToday())
        {
            return includeTime ? $"Today at {date:HH:mm}" : "Today";
        }

        if (date.IsYesterday())
        {
            return includeTime ? $"Yesterday at {date:HH:mm}" : "Yesterday";
        }

        if (date.IsTomorrow())
        {
            return includeTime ? $"Tomorrow at {date:HH:mm}" : "Tomorrow";
        }

        // Within current week
        if (Math.Abs((date.Date - DateTime.Today).TotalDays) < 7)
        {
            return includeTime 
                ? $"{date:dddd} at {date:HH:mm}" 
                : date.ToString("dddd");
        }

        // Within current year
        if (date.Year == DateTime.Now.Year)
        {
            return includeTime 
                ? date.ToString("MMM d 'at' HH:mm") 
                : date.ToString("MMM d");
        }

        // Different year
        return includeTime 
            ? date.ToString("MMM d, yyyy 'at' HH:mm") 
            : date.ToString("MMM d, yyyy");
    }

    #endregion

    #region Unix Timestamp Extensions

    /// <summary>
    /// Converts DateTime to Unix timestamp (seconds since epoch)
    /// </summary>
    public static long ToUnixTimestamp(this DateTime date)
    {
        return ((DateTimeOffset)date.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts DateTime to Unix timestamp in milliseconds
    /// </summary>
    public static long ToUnixTimestampMilliseconds(this DateTime date)
    {
        return ((DateTimeOffset)date.ToUniversalTime()).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
    }

    /// <summary>
    /// Converts Unix timestamp in milliseconds to DateTime
    /// </summary>
    public static DateTime FromUnixTimestampMilliseconds(long unixTimestampMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampMilliseconds).DateTime;
    }

    #endregion

    #region Time Zone Extensions

    /// <summary>
    /// Safely converts DateTime to UTC
    /// </summary>
    public static DateTime ToSafeUtc(this DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Converts UTC DateTime to specified time zone
    /// </summary>
    public static DateTime ToTimeZone(this DateTime utcDate, TimeZoneInfo timeZoneInfo)
    {
        if (utcDate.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC", nameof(utcDate));

        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, timeZoneInfo);
    }

    #endregion

    #region Holiday and Special Date Extensions

    /// <summary>
    /// Checks if the date is New Year's Day
    /// </summary>
    public static bool IsNewYear(this DateTime date)
    {
        return date.Month == 1 && date.Day == 1;
    }

    /// <summary>
    /// Checks if the date is Christmas Day
    /// </summary>
    public static bool IsChristmas(this DateTime date)
    {
        return date.Month == 12 && date.Day == 25;
    }

    /// <summary>
    /// Checks if the date is leap year
    /// </summary>
    public static bool IsLeapYear(this DateTime date)
    {
        return DateTime.IsLeapYear(date.Year);
    }

    /// <summary>
    /// Gets the number of days in the month
    /// </summary>
    public static int GetDaysInMonth(this DateTime date)
    {
        return DateTime.DaysInMonth(date.Year, date.Month);
    }

    #endregion
}