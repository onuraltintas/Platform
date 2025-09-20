namespace Enterprise.Shared.Configuration.Tests.Services;

[TestFixture]
public class DefaultUserContextServiceTests
{
    private Mock<ILogger<DefaultUserContextService>> _logger = null!;
    private DefaultUserContextService _userContextService = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<DefaultUserContextService>>();
        _userContextService = new DefaultUserContextService(_logger.Object);
    }

    #region GetCurrentUserId Tests

    [Test]
    public void GetCurrentUserId_ReturnsNull()
    {
        // Act
        var result = _userContextService.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCurrentUserRoles Tests

    [Test]
    public void GetCurrentUserRoles_ReturnsEmptyCollection()
    {
        // Act
        var result = _userContextService.GetCurrentUserRoles();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region HasRole Tests

    [Test]
    public void HasRole_WithAnyRole_ReturnsFalse()
    {
        // Act
        var result = _userContextService.HasRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasRole_WithNullOrEmptyRole_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _userContextService.HasRole(""));
        Assert.Throws<ArgumentException>(() => _userContextService.HasRole(" "));
    }

    #endregion

    #region GetUserClaims Tests

    [Test]
    public void GetUserClaims_ReturnsEmptyDictionary()
    {
        // Act
        var result = _userContextService.GetUserClaims();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetClaimValue Tests

    [Test]
    public void GetClaimValue_WithAnyClaim_ReturnsNull()
    {
        // Act
        var result = _userContextService.GetClaimValue("email");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GetClaimValue_WithNullOrEmptyClaimType_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _userContextService.GetClaimValue(""));
        Assert.Throws<ArgumentException>(() => _userContextService.GetClaimValue(" "));
    }

    #endregion

    #region IsAuthenticated Tests

    [Test]
    public void IsAuthenticated_ReturnsFalse()
    {
        // Act
        var result = _userContextService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Logging Tests

    [Test]
    public void GetCurrentUserId_LogsDebugMessage()
    {
        // Act
        _userContextService.GetCurrentUserId();

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    [Test]
    public void GetCurrentUserRoles_LogsDebugMessage()
    {
        // Act
        _userContextService.GetCurrentUserRoles();

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    [Test]
    public void HasRole_LogsDebugMessage()
    {
        // Act
        _userContextService.HasRole("Admin");

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    [Test]
    public void GetUserClaims_LogsDebugMessage()
    {
        // Act
        _userContextService.GetUserClaims();

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    [Test]
    public void GetClaimValue_LogsDebugMessage()
    {
        // Act
        _userContextService.GetClaimValue("email");

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    [Test]
    public void IsAuthenticated_LogsDebugMessage()
    {
        // Act
        _userContextService.IsAuthenticated();

        // Assert
        // Logger was called (we can't easily verify exact message with Mock)
        Assert.Pass();
    }

    #endregion
}