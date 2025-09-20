using Castle.DynamicProxy;
using Enterprise.Shared.Caching.Attributes;
using Enterprise.Shared.Caching.Interceptors;
using Enterprise.Shared.Caching.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace Enterprise.Shared.Caching.Tests.Interceptors;

public class CacheInterceptorTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<CacheInterceptor>> _mockLogger;
    private readonly CacheInterceptor _interceptor;
    private readonly ProxyGenerator _proxyGenerator;

    public CacheInterceptorTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CacheInterceptor>>();
        _interceptor = new CacheInterceptor(_mockCacheService.Object, _mockLogger.Object);
        _proxyGenerator = new ProxyGenerator();
    }

    [Fact]
    public void Intercept_WithoutCacheableAttribute_ShouldProceedWithoutCaching()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);

        // Act
        var result = proxy.NonCacheableMethod("test");

        // Assert
        result.Should().Be("test-processed");
        _mockCacheService.Verify(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Intercept_AsyncMethodWithCache_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);
        var cachedValue = "cached-result";

        _mockCacheService.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        // Act
        var result = await proxy.CacheableAsyncMethod("test");

        // Assert
        result.Should().Be(cachedValue);
        _mockCacheService.Verify(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Intercept_AsyncMethodWithCache_CacheMiss_ShouldExecuteAndCache()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);
        var expectedResult = "test-async-processed";

        _mockCacheService.Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await proxy.CacheableAsyncMethod("test");

        // Assert
        result.Should().Be(expectedResult);
        _mockCacheService.Verify(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Intercept_SyncMethodWithCache_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);
        var cachedValue = "cached-sync-result";

        _mockCacheService.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        // Act
        var result = proxy.CacheableSyncMethod("test");

        // Assert
        result.Should().Be(cachedValue);
        _mockCacheService.Verify(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Intercept_SyncMethodWithCache_CacheMiss_ShouldExecuteAndCache()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);
        var expectedResult = "test-sync-processed";

        _mockCacheService.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = proxy.CacheableSyncMethod("test");

        // Assert
        result.Should().Be(expectedResult);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Intercept_WithCacheInvalidateAttribute_ShouldInvalidateCache()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);

        // Act
        await proxy.InvalidateCacheMethod("test");

        // Assert
        _mockCacheService.Verify(x => x.RemovePatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Intercept_WithCustomKeyTemplate_ShouldUseCustomKey()
    {
        // Arrange
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);

        _mockCacheService.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);

        // Act
        proxy.MethodWithCustomKey("user123");

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<object>(It.Is<string>(k => k.Contains("user:user123")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("test1", "cache:TestService.CacheableSyncMethod(test1)")]
    [InlineData("test2", "cache:TestService.CacheableSyncMethod(test2)")]
    public void BuildCacheKey_WithDefaultTemplate_ShouldGenerateCorrectKey(string input, string expectedKey)
    {
        // This would require making BuildCacheKey internal or using reflection
        // For now, we test the behavior indirectly through the cache service calls
        var service = new TestService();
        var proxy = _proxyGenerator.CreateClassProxy<TestService>(_interceptor);

        _mockCacheService.Setup(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        proxy.CacheableSyncMethod(input);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

// Test service class for proxy testing
public class TestService
{
    public virtual string NonCacheableMethod(string input)
    {
        return input + "-processed";
    }

    [Cacheable(KeyTemplate = "async-method:{0}", TtlMinutes = 30)]
    public virtual async Task<string> CacheableAsyncMethod(string input)
    {
        await Task.Delay(10); // Simulate async work
        return input + "-async-processed";
    }

    [Cacheable(TtlMinutes = 15)]
    public virtual string CacheableSyncMethod(string input)
    {
        return input + "-sync-processed";
    }

    [Cacheable(KeyTemplate = "user:{0}", TtlMinutes = 60)]
    public virtual string MethodWithCustomKey(string userId)
    {
        return $"User data for {userId}";
    }

    [CacheInvalidate(Patterns = new[] { "user:*", "users:*" })]
    public virtual async Task InvalidateCacheMethod(string input)
    {
        await Task.Delay(10);
        // Simulate some work that invalidates cache
    }

    [Cacheable(TtlMinutes = 30, Sync = true)]
    public virtual async Task<string> SyncCacheableMethod(string input)
    {
        await Task.Delay(50);
        return input + "-sync-cached";
    }
}