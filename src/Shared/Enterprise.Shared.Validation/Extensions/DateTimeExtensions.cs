using System.Globalization;

namespace Enterprise.Shared.Validation.Extensions;

/// <summary>
/// DateTime extension methods for Turkish timezone and formatting
/// </summary>
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo TurkeyTimeZone = GetTurkeyTimeZone();
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch
        {
            // Fallback for Linux systems
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch
            {
                // Create custom timezone as last resort
                return TimeZoneInfo.CreateCustomTimeZone("Turkey", TimeSpan.FromHours(3), "Turkey Time", "TRT");
            }
        }
    }

    /// <summary>
    /// Converts UTC time to Turkey time
    /// </summary>
    public static DateTime ToTurkeyTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
    }

    /// <summary>
    /// Gets current Turkey time
    /// </summary>
    public static DateTime GetTurkeyNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);
    }

    /// <summary>
    /// Converts to ISO 8601 string format
    /// </summary>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    /// <summary>
    /// Formats date in Turkish format
    /// </summary>
    public static string ToTurkishDateString(this DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy", TurkishCulture);
    }

    /// <summary>
    /// Formats date and time in Turkish format
    /// </summary>
    public static string ToTurkishDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy HH:mm", TurkishCulture);
    }

    /// <summary>
    /// Formats date in Turkish long format
    /// </summary>
    public static string ToTurkishLongDateString(this DateTime dateTime)
    {
        return dateTime.ToString("dd MMMM yyyy dddd", TurkishCulture);
    }

    /// <summary>
    /// Checks if date is weekend
    /// </summary>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek == DayOfWeek.Saturday || 
               dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Checks if date is weekday
    /// </summary>
    public static bool IsWeekday(this DateTime dateTime)
    {
        return !dateTime.IsWeekend();
    }

    /// <summary>
    /// Gets start of day (00:00:00)
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>
    /// Gets end of day (23:59:59.999)
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999, dateTime.Kind);
    }

    /// <summary>
    /// Gets start of week (Monday by default for Turkish calendar)
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).StartOfDay();
    }

    /// <summary>
    /// Gets end of week
    /// </summary>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        return dateTime.StartOfWeek(startOfWeek).AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Gets start of month
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>
    /// Gets end of month
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddDays(-1).EndOfDay();
    }

    /// <summary>
    /// Calculates age in years
    /// </summary>
    public static int Age(this DateTime birthDate)
    {
        var today = GetTurkeyNow().Date;
        var age = today.Year - birthDate.Year;
        
        if (birthDate.Date > today.AddYears(-age)) age--;
        
        return age;
    }

    /// <summary>
    /// Converts to relative time string in Turkish
    /// </summary>
    public static string ToRelativeString(this DateTime dateTime)
    {
        var now = GetTurkeyNow();
        var timeSpan = now - dateTime;

        if (timeSpan.TotalDays >= 365)
        {
            var years = (int)(timeSpan.TotalDays / 365);
            return $"{years} {(years == 1 ? "yıl" : "yıl")} önce";
        }

        if (timeSpan.TotalDays >= 30)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return $"{months} {(months == 1 ? "ay" : "ay")} önce";
        }

        if (timeSpan.TotalDays >= 7)
        {
            var weeks = (int)(timeSpan.TotalDays / 7);
            return $"{weeks} {(weeks == 1 ? "hafta" : "hafta")} önce";
        }

        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            return $"{days} {(days == 1 ? "gün" : "gün")} önce";
        }

        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            return $"{hours} {(hours == 1 ? "saat" : "saat")} önce";
        }

        if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"{minutes} {(minutes == 1 ? "dakika" : "dakika")} önce";
        }

        return "Şimdi";
    }

    /// <summary>
    /// Checks if date is Turkish national holiday
    /// </summary>
    public static bool IsTurkishHoliday(this DateTime dateTime)
    {
        var date = dateTime.Date;
        var year = dateTime.Year;
        
        // Fixed holidays
        var fixedHolidays = new[]
        {
            new DateTime(year, 1, 1),   // Yılbaşı
            new DateTime(year, 4, 23),  // Ulusal Egemenlik ve Çocuk Bayramı
            new DateTime(year, 5, 1),   // İşçi Bayramı
            new DateTime(year, 5, 19),  // Atatürk'ü Anma Gençlik ve Spor Bayramı
            new DateTime(year, 7, 15),  // Demokrasi ve Milli Birlik Günü
            new DateTime(year, 8, 30),  // Zafer Bayramı
            new DateTime(year, 10, 29)  // Cumhuriyet Bayramı
        };

        return fixedHolidays.Contains(date);
    }

    /// <summary>
    /// Checks if date is business day (not weekend or holiday)
    /// </summary>
    public static bool IsBusinessDay(this DateTime dateTime)
    {
        return dateTime.IsWeekday() && !dateTime.IsTurkishHoliday();
    }

    /// <summary>
    /// Gets next business day
    /// </summary>
    public static DateTime NextBusinessDay(this DateTime dateTime)
    {
        var nextDay = dateTime.AddDays(1);
        while (!nextDay.IsBusinessDay())
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    /// <summary>
    /// Gets previous business day
    /// </summary>
    public static DateTime PreviousBusinessDay(this DateTime dateTime)
    {
        var prevDay = dateTime.AddDays(-1);
        while (!prevDay.IsBusinessDay())
        {
            prevDay = prevDay.AddDays(-1);
        }
        return prevDay;
    }

    /// <summary>
    /// Checks if year is leap year
    /// </summary>
    public static bool IsLeapYear(this DateTime dateTime)
    {
        return DateTime.IsLeapYear(dateTime.Year);
    }

    /// <summary>
    /// Gets quarter of the year (1-4)
    /// </summary>
    public static int Quarter(this DateTime dateTime)
    {
        return (dateTime.Month + 2) / 3;
    }

    /// <summary>
    /// Gets first day of quarter
    /// </summary>
    public static DateTime StartOfQuarter(this DateTime dateTime)
    {
        var quarter = dateTime.Quarter();
        var month = (quarter - 1) * 3 + 1;
        return new DateTime(dateTime.Year, month, 1, 0, 0, 0, dateTime.Kind);
    }

    /// <summary>
    /// Gets last day of quarter
    /// </summary>
    public static DateTime EndOfQuarter(this DateTime dateTime)
    {
        return dateTime.StartOfQuarter().AddMonths(3).AddDays(-1).EndOfDay();
    }
}