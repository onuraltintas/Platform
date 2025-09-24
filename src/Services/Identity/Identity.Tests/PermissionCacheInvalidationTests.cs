using System.Threading.Tasks;
using Identity.Application.Services;
using Identity.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Identity.Tests;

public class PermissionCacheInvalidationTests
{
    [Fact]
    public async Task InvalidateRolePermissions_ShouldNotifyGateway()
    {
        var cache = Mock.Of<Enterprise.Shared.Caching.Interfaces.ICacheService>();
        var bulkCache = Mock.Of<Enterprise.Shared.Caching.Interfaces.IBulkCacheService>();
        var metrics = Mock.Of<Enterprise.Shared.Caching.Interfaces.ICacheMetricsService>();
        var context = Mocks.CreateDbContext();
        var logger = Mock.Of<ILogger<PermissionCacheService>>();
        var gateway = new Mock<IGatewayCacheInvalidationClient>();

        var service = new PermissionCacheService(cache, bulkCache, metrics, context, logger, gateway.Object);

        await service.InvalidateRolePermissionsAsync("role-id");

        gateway.Verify(g => g.BulkInvalidateAsync(It.IsAny<IEnumerable<string>>(), default), Times.Once);
    }
}

