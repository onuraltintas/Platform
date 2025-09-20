using Enterprise.Shared.ErrorHandling.Models;

namespace Enterprise.Shared.ErrorHandling.Handlers;

public interface ITimeZoneProvider
{
    DateTime GetCurrentTime();
    DateTime ConvertToLocalTime(DateTime utcDateTime);
    TimeZoneInfo GetTimeZoneInfo();
}

public class TimeZoneProvider : ITimeZoneProvider
{
    private readonly TimeZoneInfo _timeZoneInfo;
    private readonly ILogger<TimeZoneProvider> _logger;

    public TimeZoneProvider(IOptions<ErrorHandlingSettings> settings, ILogger<TimeZoneProvider> logger)
    {
        _logger = logger;
        
        try
        {
            // Try to get Turkish time zone
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.TimeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogWarning(ex, "Türkiye saat dilimi bulunamadı: {TimeZoneId}. UTC kullanılıyor.", settings.Value.TimeZoneId);
            
            // Fallback: try alternative names for Turkish timezone
            try
            {
                _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Turkey");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("+03");
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Türkiye saat dilimi alternatifi bulunamadı, UTC+3 manuel olarak oluşturuluyor");
                    _timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("TurkeyCustom", TimeSpan.FromHours(3), "Türkiye Saati", "TRT");
                }
            }
        }

        _logger.LogInformation("Saat dilimi ayarlandı: {TimeZoneName} (Offset: {Offset})", 
            _timeZoneInfo.DisplayName, _timeZoneInfo.BaseUtcOffset);
    }

    public DateTime GetCurrentTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZoneInfo);
    }

    public DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZoneInfo);
        }
        
        return utcDateTime;
    }

    public TimeZoneInfo GetTimeZoneInfo()
    {
        return _timeZoneInfo;
    }
}