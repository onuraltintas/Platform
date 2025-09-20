using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Caching.Memory;

namespace User.API.Configuration;

/// <summary>
/// Performance optimization extensions for the User API
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>
    /// Adds performance optimizations to the service collection
    /// </summary>
    public static IServiceCollection AddPerformanceOptimizations(this IServiceCollection services, IConfiguration configuration)
    {
        // Memory caching for frequently accessed data
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000; // Maximum number of cache entries
            options.CompactionPercentage = 0.25; // Remove 25% of entries when limit is reached
        });

        // Response caching middleware
        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 64 * 1024 * 1024; // 64MB
            options.UseCaseSensitivePaths = false;
        });

        // Output caching for static responses
        services.AddOutputCache(options =>
        {
            // Cache user profiles for 5 minutes
            options.AddPolicy("UserProfile", builder =>
            {
                builder.Expire(TimeSpan.FromMinutes(5));
                builder.Tag("userprofile");
                builder.SetVaryByQuery("userId");
            });

            // Cache user preferences for 10 minutes
            options.AddPolicy("UserPreferences", builder =>
            {
                builder.Expire(TimeSpan.FromMinutes(10));
                builder.Tag("userpreferences");
                builder.SetVaryByQuery("userId");
            });

            // Cache GDPR data for 1 hour (since it doesn't change frequently)
            options.AddPolicy("GdprData", builder =>
            {
                builder.Expire(TimeSpan.FromHours(1));
                builder.Tag("gdpr");
                builder.SetVaryByQuery("userId", "format");
            });
        });

        return services;
    }

    /// <summary>
    /// Configures performance middleware in the request pipeline
    /// </summary>
    public static IApplicationBuilder UsePerformanceOptimizations(this IApplicationBuilder app)
    {
        // Response caching
        app.UseResponseCaching();

        // Output caching
        app.UseOutputCache();

        // Custom caching headers for static content
        app.Use(async (context, next) =>
        {
            // Cache static API responses
            if (context.Request.Path.StartsWithSegments("/api/v1/gdpr/export"))
            {
                context.Response.Headers.CacheControl = "public, max-age=3600"; // 1 hour
            }
            else if (context.Request.Path.StartsWithSegments("/api/v1/userprofile") ||
                     context.Request.Path.StartsWithSegments("/api/v1/userpreferences"))
            {
                context.Response.Headers.CacheControl = "public, max-age=300"; // 5 minutes
            }

            await next();
        });

        return app;
    }
}

/// <summary>
/// Memory cache extensions for consistent caching patterns
/// </summary>
public static class MemoryCacheExtensions
{
    /// <summary>
    /// Gets or sets a cache entry with standard options
    /// </summary>
    public static async Task<T?> GetOrSetAsync<T>(this IMemoryCache cache, string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var value = await factory();
        if (value != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
                Size = 1,
                Priority = CacheItemPriority.Normal
            };

            cache.Set(key, value, options);
        }

        return value;
    }

    /// <summary>
    /// Cache keys for consistent naming
    /// </summary>
    public static class CacheKeys
    {
        /// <summary>
        /// Cache key for user profile data
        /// </summary>
        public static string UserProfile(string userId) => $"userprofile:{userId}";
        
        /// <summary>
        /// Cache key for user preferences data
        /// </summary>
        public static string UserPreferences(string userId) => $"userpreferences:{userId}";
        
        /// <summary>
        /// Cache key for GDPR export data
        /// </summary>
        public static string GdprExport(string userId, string format) => $"gdpr:{userId}:{format}";
        
        /// <summary>
        /// Cache key for email verification status
        /// </summary>
        public static string EmailVerificationStatus(string userId) => $"emailverification:{userId}";
    }
}