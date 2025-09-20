using Identity.Application.Services;
using Identity.Core.Entities;
using Identity.Core.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using Xunit;

namespace Identity.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Mock UserManager - requires special setup
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _userService = new UserService(_mockUserManager.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };
        var userDto = new UserDto { Id = userId, Email = "test@example.com" };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("test@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var userId = "invalid-user-id";
        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Kullanıcı bulunamadı", result.Error);
    }

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new ApplicationUser { Id = "test-id", Email = email };
        var userDto = new UserDto { Id = "test-id", Email = email };

        _mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _mockMapper.Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Email);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Password = "Password123!",
            EmailConfirmed = true
        };

        var user = new ApplicationUser
        {
            Id = "new-user-id",
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var userDto = new UserDto
        {
            Id = "new-user-id",
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<ApplicationUser>()))
            .Returns(userDto);

        // Act
        var result = await _userService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.FirstName, result.Value.FirstName);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ReturnsFailure()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "weak"
        };

        var identityErrors = new[]
        {
            new IdentityError { Description = "Password too weak" }
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _userService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Password too weak", result.Error);
    }
}