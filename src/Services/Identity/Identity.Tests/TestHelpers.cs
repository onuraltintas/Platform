using System;
using AutoMapper;
using Identity.Application.Services;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Identity.Tests;

public static class TestHelpers
{
    public static TokenService CreateTokenService(
        UserManager<Identity.Core.Entities.ApplicationUser>? userManager = null,
        IUserService? userService = null,
        IGroupService? groupService = null,
        IPermissionService? permissionService = null,
        Enterprise.Shared.Security.Interfaces.ITokenService? enterpriseTokenService = null,
        Enterprise.Shared.Caching.Interfaces.ICacheService? cacheService = null,
        IMapper? mapper = null,
        ILogger<TokenService>? logger = null,
        IConfiguration? configuration = null,
        IRefreshTokenRepository? refreshTokenRepository = null)
    {
        userManager ??= Mock.Of<UserManager<Identity.Core.Entities.ApplicationUser>>(MockBehavior.Loose);
        userService ??= Mock.Of<IUserService>();
        groupService ??= Mock.Of<IGroupService>();
        permissionService ??= Mock.Of<IPermissionService>();
        enterpriseTokenService ??= Mock.Of<Enterprise.Shared.Security.Interfaces.ITokenService>();
        cacheService ??= Mock.Of<Enterprise.Shared.Caching.Interfaces.ICacheService>();
        mapper ??= Mock.Of<IMapper>();
        logger ??= Mock.Of<ILogger<TokenService>>();

        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
        {
            { "JWT_ACCESS_TOKEN_EXPIRY", "15" },
            { "JWT_REFRESH_TOKEN_EXPIRY", "10080" },
        };
        configuration ??= new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();
        refreshTokenRepository ??= Mock.Of<IRefreshTokenRepository>();

        return new TokenService(
            userManager,
            userService,
            groupService,
            permissionService,
            enterpriseTokenService,
            cacheService,
            mapper,
            logger,
            configuration,
            refreshTokenRepository);
    }
}