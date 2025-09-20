using Identity.Application.Services;
using Identity.Core.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using Xunit;

namespace Identity.Tests.Services;

public class PermissionServiceTests
{
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PermissionService>>();
        _permissionService = new PermissionService(_mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenPermissionNotFound()
    {
        // Arrange
        var permissionId = Guid.NewGuid();

        // Act
        var result = await _permissionService.GetByIdAsync(permissionId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("İzin bulunamadı", result.Error);
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsEmptyPagedResult()
    {
        // Act
        var result = await _permissionService.GetPermissionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Data);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task CheckPermissionAsync_ReturnsNotAllowed()
    {
        // Arrange
        var request = new PermissionCheckRequest
        {
            UserId = "test-user",
            Permission = "test.permission"
        };

        // Act
        var result = await _permissionService.CheckPermissionAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAllowed);
        Assert.Equal("Permission check not implemented", result.Value.Reason);
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user";
        var permission = "test.permission";

        // Act
        var result = await _permissionService.HasPermissionAsync(userId, permission);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task GetUserPermissionNamesAsync_ReturnsDefaultPermissions()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _permissionService.GetUserPermissionNamesAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("profile.read", result.Value);
        Assert.Contains("profile.write", result.Value);
    }

    [Fact]
    public async Task CreatePermissionAsync_ReturnsFailure()
    {
        // Arrange
        var request = new CreatePermissionRequest
        {
            Name = "test.permission",
            Resource = "test",
            Action = "read"
        };

        // Act
        var result = await _permissionService.CreatePermissionAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("İzin oluşturulamadı", result.Error);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsSuccess()
    {
        // Arrange
        var permissionId = Guid.NewGuid();

        // Act
        var result = await _permissionService.DeletePermissionAsync(permissionId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }
}