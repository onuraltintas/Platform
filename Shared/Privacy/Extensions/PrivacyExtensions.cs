using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Services;
using EgitimPlatform.Shared.Privacy.Middleware;
using EgitimPlatform.Shared.Privacy.Validation;

namespace EgitimPlatform.Shared.Privacy.Extensions;

public static class PrivacyExtensions
{
    public static IServiceCollection AddPrivacyCompliance(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure privacy options
        services.Configure<PrivacyOptions>(configuration.GetSection(PrivacyOptions.SectionName));

        // Register core privacy services
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<IDataSubjectRightsService, DataSubjectRightsService>();
        services.AddScoped<IPersonalDataInventoryService, PersonalDataInventoryService>();
        services.AddScoped<IDataProcessingActivityService, DataProcessingActivityService>();
        
        // Register validation services
        services.AddScoped<IPrivacyValidator, PrivacyValidator>();
        
        // Register HTTP context accessor for IP address tracking
        services.AddHttpContextAccessor();

        return services;
    }

    public static IApplicationBuilder UsePrivacyCompliance(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PrivacyComplianceMiddleware>();
    }

    public static IServiceCollection AddPrivacyValidation(this IServiceCollection services)
    {
        services.AddScoped<IPrivacyValidator, PrivacyValidator>();
        return services;
    }

    // Extension methods for HttpContext to work with privacy data
    public static void SetCookieConsent(this HttpContext context, bool essential, bool analytical, 
        bool marketing, int expiryDays = 365)
    {
        var consent = new
        {
            Essential = essential,
            Analytical = analytical,
            Marketing = marketing,
            ConsentDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(expiryDays),
            Version = "1.0"
        };

        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(expiryDays),
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        };

        context.Response.Cookies.Append("privacy-consent", 
            System.Text.Json.JsonSerializer.Serialize(consent), cookieOptions);
    }

    public static string? GetClientIpAddress(this HttpContext context)
    {
        var request = context.Request;
        
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public static string? GetUserId(this HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? 
              context.User.FindFirst("userId")?.Value ??
              context.User.FindFirst("id")?.Value
            : null;
    }

    public static string GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    // Extension methods for working with personal data attributes
    public static bool HasPersonalData(this Type type)
    {
        return type.GetProperties()
            .Any(p => p.GetCustomAttributes(typeof(Attributes.PersonalDataAttribute), true).Any());
    }

    public static IEnumerable<System.Reflection.PropertyInfo> GetPersonalDataProperties(this Type type)
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(Attributes.PersonalDataAttribute), true).Any());
    }

    public static IEnumerable<System.Reflection.PropertyInfo> GetSensitiveDataProperties(this Type type)
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(Attributes.SensitivePersonalDataAttribute), true).Any());
    }
}

// Helper class for cookie consent management
public static class CookieConsentHelper
{
    public static void SetAnalyticalCookies(HttpContext context, bool allowed)
    {
        if (!allowed)
        {
            // Remove analytical cookies if consent withdrawn
            var analyticalCookies = new[] { "_ga", "_gid", "_gat", "_gtag" };
            foreach (var cookie in analyticalCookies)
            {
                context.Response.Cookies.Delete(cookie);
            }
        }
    }

    public static void SetMarketingCookies(HttpContext context, bool allowed)
    {
        if (!allowed)
        {
            // Remove marketing cookies if consent withdrawn
            var marketingCookies = new[] { "_fbp", "_fbc", "fr" };
            foreach (var cookie in marketingCookies)
            {
                context.Response.Cookies.Delete(cookie);
            }
        }
    }

    public static bool HasValidConsent(HttpContext context, string cookieType)
    {
        var consentCookie = context.Request.Cookies["privacy-consent"];
        if (string.IsNullOrEmpty(consentCookie))
            return false;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(consentCookie);
            var root = doc.RootElement;

            if (root.TryGetProperty("ExpiryDate", out var expiryElement) &&
                expiryElement.TryGetDateTime(out var expiryDate) &&
                expiryDate < DateTime.UtcNow)
            {
                return false;
            }

            return cookieType.ToLowerInvariant() switch
            {
                "essential" => true, // Essential cookies are always allowed
                "analytical" => root.TryGetProperty("Analytical", out var analyticalElement) && 
                               analyticalElement.GetBoolean(),
                "marketing" => root.TryGetProperty("Marketing", out var marketingElement) && 
                              marketingElement.GetBoolean(),
                _ => false
            };
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}