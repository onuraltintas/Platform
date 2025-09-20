namespace Enterprise.Shared.Common.Utilities;

/// <summary>
/// Helper class for timezone operations
/// </summary>
public static class TimeZoneHelper
{
    private static TimeZoneInfo? _turkeyTimeZone;

    /// <summary>
    /// Gets the Turkey timezone info (cross-platform compatible)
    /// </summary>
    public static TimeZoneInfo TurkeyTimeZone
    {
        get
        {
            if (_turkeyTimeZone != null) return _turkeyTimeZone;

            // Try different timezone identifiers for cross-platform compatibility
            var timeZoneIds = new[]
            {
                "Europe/Istanbul",      // Linux/macOS
                "Turkey Standard Time", // Windows
                "GTB Standard Time"     // Windows alternative
            };

            foreach (var id in timeZoneIds)
            {
                try
                {
                    _turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return _turkeyTimeZone;
                }
                catch (TimeZoneNotFoundException)
                {
                    // Try next identifier
                    continue;
                }
            }

            // Fallback to UTC+3 if no system timezone found
            _turkeyTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "Turkey", 
                TimeSpan.FromHours(3), 
                "Turkey Standard Time", 
                "Turkey Standard Time");

            return _turkeyTimeZone;
        }
    }

    /// <summary>
    /// Converts UTC DateTime to Turkey timezone
    /// </summary>
    /// <param name="utcDateTime">UTC DateTime</param>
    /// <returns>DateTime in Turkey timezone</returns>
    public static DateTime ToTurkeyTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
    }

    /// <summary>
    /// Converts any DateTime to Turkey timezone
    /// </summary>
    /// <param name="dateTime">Source DateTime</param>
    /// <returns>DateTime in Turkey timezone</returns>
    public static DateTime ConvertToTurkeyTime(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, TurkeyTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, TurkeyTimeZone),
            _ => TimeZoneInfo.ConvertTime(dateTime, TurkeyTimeZone)
        };
    }
}