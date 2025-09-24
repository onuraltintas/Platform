using System.Threading.Tasks;
using Identity.Application.Services;
using Identity.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Identity.Tests;

public class RefreshTokenTests
{
    [Fact]
    public async Task ValidateRefreshToken_ShouldReturnFalse_WhenTokenNotFound()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashedTokenAsync(It.IsAny<string>(), default)).ReturnsAsync((Identity.Core.Entities.RefreshToken?)null);

        var tokenService = TestHelpers.CreateTokenService(refreshTokenRepository: repo.Object);

        var result = await tokenService.ValidateRefreshTokenAsync("nonexistent");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ValidateRefreshToken_ShouldReturnFalse_WhenRevoked()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashedTokenAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new Identity.Core.Entities.RefreshToken
            {
                IsRevoked = true,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

        var tokenService = TestHelpers.CreateTokenService(refreshTokenRepository: repo.Object);

        var result = await tokenService.ValidateRefreshTokenAsync("revoked");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ValidateRefreshToken_ShouldReturnFalse_WhenUsed()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashedTokenAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new Identity.Core.Entities.RefreshToken
            {
                IsRevoked = false,
                IsUsed = true,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

        var tokenService = TestHelpers.CreateTokenService(refreshTokenRepository: repo.Object);

        var result = await tokenService.ValidateRefreshTokenAsync("used");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ValidateRefreshToken_ShouldReturnFalse_WhenExpired()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashedTokenAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new Identity.Core.Entities.RefreshToken
            {
                IsRevoked = false,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });

        var tokenService = TestHelpers.CreateTokenService(refreshTokenRepository: repo.Object);

        var result = await tokenService.ValidateRefreshTokenAsync("expired");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ValidateRefreshToken_ShouldReturnTrue_WhenValid()
    {
        var repo = new Mock<IRefreshTokenRepository>();
        repo.Setup(r => r.GetByHashedTokenAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new Identity.Core.Entities.RefreshToken
            {
                IsRevoked = false,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });

        var tokenService = TestHelpers.CreateTokenService(refreshTokenRepository: repo.Object);

        var result = await tokenService.ValidateRefreshTokenAsync("valid");
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }
}

