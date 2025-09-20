using Castle.DynamicProxy;
using Enterprise.Shared.Caching.Attributes;
using Enterprise.Shared.Caching.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Enterprise.Shared.Caching.Interceptors;

public class CacheInterceptor : IInterceptor
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInterceptor> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public CacheInterceptor(ICacheService cacheService, ILogger<CacheInterceptor> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var cacheableAttribute = method.GetCustomAttribute<CacheableAttribute>();
        var invalidateAttribute = method.GetCustomAttribute<CacheInvalidateAttribute>();

        // Before method invalidation
        if (invalidateAttribute?.BeforeInvocation == true)
        {
            InvalidateCache(invocation, invalidateAttribute);
        }

        // Cacheable method handling
        if (cacheableAttribute != null)
        {
            HandleCacheableMethod(invocation, cacheableAttribute);
        }
        else
        {
            invocation.Proceed();
        }

        // After method invalidation
        if (invalidateAttribute?.BeforeInvocation == false || invalidateAttribute?.BeforeInvocation == null)
        {
            InvalidateCache(invocation, invalidateAttribute);
        }
    }

    private void HandleCacheableMethod(IInvocation invocation, CacheableAttribute attribute)
    {
        // Condition kontrolü
        if (!string.IsNullOrEmpty(attribute.Condition) && !EvaluateCondition(attribute.Condition, invocation.Arguments))
        {
            invocation.Proceed();
            return;
        }

        var cacheKey = BuildCacheKey(invocation, attribute);
        
        if (string.IsNullOrEmpty(cacheKey))
        {
            _logger.LogWarning("Cache key oluşturulamadı. Method: {Method}", invocation.Method.Name);
            invocation.Proceed();
            return;
        }

        // Async method kontrolü
        if (IsAsyncMethod(invocation.Method))
        {
            HandleAsyncCacheableMethod(invocation, attribute, cacheKey);
        }
        else
        {
            HandleSyncCacheableMethod(invocation, attribute, cacheKey);
        }
    }

    private void HandleAsyncCacheableMethod(IInvocation invocation, CacheableAttribute attribute, string cacheKey)
    {
        var returnType = invocation.Method.ReturnType;
        var resultType = GetTaskResultType(returnType);

        if (resultType == null)
        {
            invocation.Proceed();
            return;
        }

        // Sync kontrolü - aynı key için eş zamanlı erişimi engelle
        if (attribute.Sync)
        {
            var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            invocation.ReturnValue = CreateTypedTask(HandleAsyncWithSemaphore(invocation, attribute, cacheKey, resultType, semaphore), resultType);
        }
        else
        {
            invocation.ReturnValue = CreateTypedTask(HandleAsyncCacheable(invocation, attribute, cacheKey, resultType), resultType);
        }
    }

    private async Task<object?> HandleAsyncWithSemaphore(IInvocation invocation, CacheableAttribute attribute, 
        string cacheKey, Type resultType, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            return await HandleAsyncCacheable(invocation, attribute, cacheKey, resultType);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<object?> HandleAsyncCacheable(IInvocation invocation, CacheableAttribute attribute, 
        string cacheKey, Type resultType)
    {
        try
        {
            // Cache'den veri al
            var cachedValue = await GetFromCacheAsync(cacheKey, resultType);
            
            if (cachedValue != null)
            {
                _logger.LogDebug("Cache hit. Key: {CacheKey}, Method: {Method}", cacheKey, invocation.Method.Name);
                return cachedValue;
            }

            // Cache miss - method'u çalıştır
            _logger.LogDebug("Cache miss. Key: {CacheKey}, Method: {Method}", cacheKey, invocation.Method.Name);
            invocation.Proceed();

            var task = (Task)invocation.ReturnValue;
            await task;

            var result = GetTaskResult(task, resultType);

            // Unless condition kontrolü
            if (!string.IsNullOrEmpty(attribute.Unless) && EvaluateUnlessCondition(attribute.Unless, result))
            {
                _logger.LogDebug("Unless condition true, cache'e kaydedilmiyor. Key: {CacheKey}", cacheKey);
                return result;
            }

            // Sonucu cache'e kaydet
            if (result != null)
            {
                var ttl = TimeSpan.FromMinutes(attribute.TtlMinutes);
                await SetToCacheAsync(cacheKey, result, resultType, ttl);
                _logger.LogDebug("Sonuç cache'e kaydedildi. Key: {CacheKey}, TTL: {TTL}", cacheKey, ttl);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache işlemi sırasında hata. Key: {CacheKey}, Method: {Method}", 
                cacheKey, invocation.Method.Name);
            
            // Hata durumunda original method'u çalıştır
            invocation.Proceed();
            var task = (Task)invocation.ReturnValue;
            await task;
            return GetTaskResult(task, resultType);
        }
    }

    private void HandleSyncCacheableMethod(IInvocation invocation, CacheableAttribute attribute, string cacheKey)
    {
        try
        {
            var returnType = invocation.Method.ReturnType;
            
            // Cache'den veri al
            var cachedValue = _cacheService.GetAsync<object>(cacheKey).GetAwaiter().GetResult();
            
            if (cachedValue != null)
            {
                _logger.LogDebug("Cache hit. Key: {CacheKey}, Method: {Method}", cacheKey, invocation.Method.Name);
                invocation.ReturnValue = Convert.ChangeType(cachedValue, returnType);
                return;
            }

            // Cache miss - method'u çalıştır
            _logger.LogDebug("Cache miss. Key: {CacheKey}, Method: {Method}", cacheKey, invocation.Method.Name);
            invocation.Proceed();

            var result = invocation.ReturnValue;

            // Unless condition kontrolü
            if (!string.IsNullOrEmpty(attribute.Unless) && EvaluateUnlessCondition(attribute.Unless, result))
            {
                _logger.LogDebug("Unless condition true, cache'e kaydedilmiyor. Key: {CacheKey}", cacheKey);
                return;
            }

            // Sonucu cache'e kaydet
            if (result != null)
            {
                var ttl = TimeSpan.FromMinutes(attribute.TtlMinutes);
                SetToCacheAsync(cacheKey, result, returnType, ttl).GetAwaiter().GetResult();
                _logger.LogDebug("Sonuç cache'e kaydedildi. Key: {CacheKey}, TTL: {TTL}", cacheKey, ttl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache işlemi sırasında hata. Key: {CacheKey}, Method: {Method}", 
                cacheKey, invocation.Method.Name);
            
            // Hata durumunda original method'u çalıştır
            invocation.Proceed();
        }
    }

    private void InvalidateCache(IInvocation invocation, CacheInvalidateAttribute? attribute)
    {
        if (attribute == null) return;

        try
        {
            // Condition kontrolü
            if (!string.IsNullOrEmpty(attribute.Condition) && 
                !EvaluateCondition(attribute.Condition, invocation.Arguments))
            {
                return;
            }

            // Tüm cache'i temizle
            if (attribute.AllEntries)
            {
                if (_cacheService is IAdvancedCacheService advancedCache)
                {
                    advancedCache.FlushAsync().GetAwaiter().GetResult();
                    _logger.LogInformation("Tüm cache temizlendi. Method: {Method}", invocation.Method.Name);
                }
                return;
            }

            // Pattern'leri temizle
            foreach (var pattern in attribute.Patterns)
            {
                var resolvedPattern = ResolveKeyTemplate(pattern, invocation.Arguments);
                _cacheService.RemovePatternAsync(resolvedPattern).GetAwaiter().GetResult();
                _logger.LogDebug("Pattern temizlendi: {Pattern}, Method: {Method}", resolvedPattern, invocation.Method.Name);
            }

            // Key'leri temizle
            foreach (var key in attribute.Keys)
            {
                var resolvedKey = ResolveKeyTemplate(key, invocation.Arguments);
                _cacheService.RemoveAsync(resolvedKey).GetAwaiter().GetResult();
                _logger.LogDebug("Key temizlendi: {Key}, Method: {Method}", resolvedKey, invocation.Method.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache invalidation sırasında hata. Method: {Method}", invocation.Method.Name);
        }
    }

    private string BuildCacheKey(IInvocation invocation, CacheableAttribute attribute)
    {
        var keyTemplate = attribute.KeyTemplate;
        
        if (string.IsNullOrEmpty(keyTemplate))
        {
            // Default key pattern: ClassName.MethodName(param1,param2,...)
            var className = invocation.TargetType.Name;
            var methodName = invocation.Method.Name;
            var paramStr = string.Join(",", invocation.Arguments.Select(arg => 
                arg?.ToString() ?? "null"));
            keyTemplate = $"{className}.{methodName}({paramStr})";
        }

        var resolvedKey = ResolveKeyTemplate(keyTemplate, invocation.Arguments);
        
        // Prefix ekle
        var prefix = attribute.KeyPrefix ?? "cache:";
        if (!resolvedKey.StartsWith(prefix))
        {
            resolvedKey = prefix + resolvedKey;
        }

        return resolvedKey;
    }

    private static string ResolveKeyTemplate(string template, object[] arguments)
    {
        if (string.IsNullOrEmpty(template)) return template;

        try
        {
            return string.Format(template, arguments);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Cache key template formatı geçersiz: {template}", ex);
        }
    }

    private async Task<object?> GetFromCacheAsync(string key, Type resultType)
    {
        try
        {
            var method = typeof(ICacheService).GetMethod(nameof(ICacheService.GetAsync));
            var genericMethod = method?.MakeGenericMethod(resultType);
            var task = (Task?)genericMethod?.Invoke(_cacheService, new object[] { key, CancellationToken.None });
            
            if (task == null) return null;
            
            await task;
            return GetTaskResult(task, resultType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache'den veri alınırken hata. Key: {Key}", key);
            return null;
        }
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType.IsAssignableTo(typeof(Task));
    }

    private static Type? GetTaskResultType(Type taskType)
    {
        if (taskType == typeof(Task)) return null;
        
        if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return taskType.GetGenericArguments()[0];
        }
        
        return null;
    }

    private static object? GetTaskResult(Task task, Type resultType)
    {
        var property = task.GetType().GetProperty("Result");
        return property?.GetValue(task);
    }

    private static bool EvaluateCondition(string condition, object[] arguments)
    {
        // Basit condition evaluation - gerçek uygulamada Expression parser kullanılabilir
        try
        {
            // #{args[0] > 0} gibi basit expression'ları destekle
            if (condition.Contains("#{args[") && condition.Contains("]}"))
            {
                // Bu örnekte sadece basit case'leri handle ediyoruz
                // Gerçek implementasyonda ExpressionEvaluator kullanın
                return true; // Şimdilik her zaman true döndür
            }
            return true;
        }
        catch
        {
            return true; // Hata durumunda cache işlemini yap
        }
    }

    private static bool EvaluateUnlessCondition(string unless, object? result)
    {
        try
        {
            // #{result == null} gibi expression'ları değerlendir
            if (unless.Contains("#{result == null}"))
            {
                return result == null;
            }
            return false;
        }
        catch
        {
            return false; // Hata durumunda cache'e kaydet
        }
    }

    private async Task SetToCacheAsync(string key, object? value, Type valueType, TimeSpan ttl)
    {
        try
        {
            var method = typeof(ICacheService).GetMethod(nameof(ICacheService.SetAsync));
            var genericMethod = method?.MakeGenericMethod(valueType);
            var task = (Task?)genericMethod?.Invoke(_cacheService, new object[] { key, value!, ttl, CancellationToken.None });
            
            if (task != null)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache'e veri kaydedilirken hata. Key: {Key}", key);
        }
    }

    private static Task CreateTypedTask(Task<object?> sourceTask, Type resultType)
    {
        var taskCompletionSourceType = typeof(TaskCompletionSource<>).MakeGenericType(resultType);
        var taskCompletionSource = Activator.CreateInstance(taskCompletionSourceType);
        
        var taskProperty = taskCompletionSourceType.GetProperty("Task");
        var resultTask = taskProperty?.GetValue(taskCompletionSource);
        
        var setResultMethod = taskCompletionSourceType.GetMethod("SetResult");
        var setExceptionMethod = taskCompletionSourceType.GetMethod("SetException", new[] { typeof(Exception) });
        
        sourceTask.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                setExceptionMethod?.Invoke(taskCompletionSource, new object[] { t.Exception.InnerException ?? t.Exception });
            }
            else if (t.IsCanceled)
            {
                var setCanceledMethod = taskCompletionSourceType.GetMethod("SetCanceled");
                setCanceledMethod?.Invoke(taskCompletionSource, null);
            }
            else
            {
                setResultMethod?.Invoke(taskCompletionSource, new object?[] { t.Result });
            }
        });
        
        return (Task)resultTask!;
    }
}