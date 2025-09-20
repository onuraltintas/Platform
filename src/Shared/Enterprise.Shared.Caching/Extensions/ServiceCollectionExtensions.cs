using Castle.DynamicProxy;
using Enterprise.Shared.Caching.Interceptors;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Caching.Models;
using Enterprise.Shared.Caching.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Enterprise.Shared.Caching.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enterprise Shared Caching servislerini DI container'a ekler
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configureOptions">Cache ayarlarını özelleştirmek için action</param>
    /// <returns></returns>
    public static IServiceCollection AddSharedCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheAyarlari>? configureOptions = null)
    {
        // Configuration binding
        services.Configure<CacheAyarlari>(configuration.GetSection(CacheAyarlari.ConfigSection));
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Redis connection
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CacheAyarlari>>().Value;
            var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            
            try
            {
                var connectionMultiplexer = ConnectionMultiplexer.Connect(options.RedisConnection);
                
                connectionMultiplexer.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis bağlantısı kesildi: {Exception}", args.Exception?.Message);
                };
                
                connectionMultiplexer.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis bağlantısı yeniden kuruldu");
                };
                
                logger.LogInformation("Redis bağlantısı başarıyla kuruldu: {ConnectionString}", 
                    options.RedisConnection);
                
                return connectionMultiplexer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis bağlantısı kurulamadı: {ConnectionString}", options.RedisConnection);
                throw new InvalidOperationException($"Redis bağlantısı kurulamadı: {options.RedisConnection}", ex);
            }
        });

        // Stack Exchange Redis Distributed Cache
        services.AddStackExchangeRedisCache(options =>
        {
            var config = configuration.GetSection(CacheAyarlari.ConfigSection).Get<CacheAyarlari>() ?? new CacheAyarlari();
            
            options.Configuration = config.RedisConnection;
            options.InstanceName = config.KeyPrefix.TrimEnd(':');
        });

        // Memory Cache (L1)
        services.AddMemoryCache(options =>
        {
            var config = configuration.GetSection(CacheAyarlari.ConfigSection).Get<CacheAyarlari>() ?? new CacheAyarlari();
            
            options.SizeLimit = config.L1CacheSize * 1024 * 1024; // MB to bytes
        });

        // Cache Services
        services.AddSingleton<ICacheMetricsService, CacheMetricsService>();
        services.AddSingleton<ICacheService, DistributedCacheService>();
        services.AddSingleton<IAdvancedCacheService>(provider => 
            (IAdvancedCacheService)provider.GetRequiredService<ICacheService>());
        services.AddSingleton<IBulkCacheService>(provider => 
            (IBulkCacheService)provider.GetRequiredService<ICacheService>());
        services.AddSingleton<ICacheHealthCheckService>(provider => 
            (ICacheHealthCheckService)provider.GetRequiredService<ICacheService>());

        // Castle DynamicProxy for Interceptors
        services.AddSingleton<ProxyGenerator>();
        services.AddSingleton<CacheInterceptor>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<CacheHealthCheck>("cache_health_check");

        return services;
    }

    /// <summary>
    /// Minimal cache configuration ile servis ekler (sadece Memory Cache)
    /// </summary>
    public static IServiceCollection AddMemoryCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheMetricsService, CacheMetricsService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        return services;
    }

    /// <summary>
    /// Cache interceptor ile bir service'i proxy'ler
    /// </summary>
    /// <typeparam name="TInterface">Service interface'i</typeparam>
    /// <typeparam name="TImplementation">Service implementation'ı</typeparam>
    public static IServiceCollection AddCacheableService<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddTransient<TImplementation>();
        
        services.AddTransient<TInterface>(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
            var interceptor = provider.GetRequiredService<CacheInterceptor>();
            var implementation = provider.GetRequiredService<TImplementation>();
            
            return proxyGenerator.CreateInterfaceProxyWithTarget<TInterface>(implementation, interceptor);
        });

        return services;
    }

    /// <summary>
    /// Concrete class için cache proxy oluşturur
    /// </summary>
    /// <typeparam name="TService">Service class (virtual method'ları olmalı)</typeparam>
    public static IServiceCollection AddCacheableClass<TService>(
        this IServiceCollection services)
        where TService : class
    {
        services.AddTransient(provider =>
        {
            var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
            var interceptor = provider.GetRequiredService<CacheInterceptor>();
            
            return proxyGenerator.CreateClassProxy<TService>(interceptor);
        });

        return services;
    }
}