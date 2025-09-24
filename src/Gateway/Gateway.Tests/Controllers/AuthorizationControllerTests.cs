using System.Security.Claims;
using Gateway.API.Controllers;
using Gateway.Core.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gateway.Tests.Controllers;

public class AuthorizationControllerTests
{
    [Fact]
    public async Task InvalidateUserPermissionsInternal_ShouldReturnUnauthorized_WhenApiKeyInvalid()
    {
        var svc = new Mock<IGatewayPermissionService>();
        var logger = Mock.Of<ILogger<AuthorizationController>>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            { "GATEWAY_INTERNAL_API_KEY", "secret" }
        }).Build();

        var controller = new AuthorizationController(svc.Object, logger, config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.InvalidateUserPermissionsInternal("user1");

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}

