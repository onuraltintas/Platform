using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using System.Security.Claims;

namespace Gateway.API.Transforms;

public class GatewayRequestTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // Validation logic if needed
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // Validation logic if needed
    }

    public void Apply(TransformBuilderContext context)
    {
        // Add request ID and gateway headers
        context.AddRequestTransform(transformContext =>
        {
            var requestId = transformContext.HttpContext.TraceIdentifier;
            transformContext.ProxyRequest.Headers.Add("X-Request-Id", requestId);
            transformContext.ProxyRequest.Headers.Add("X-Gateway-Source", "API-Gateway");
            transformContext.ProxyRequest.Headers.Add("X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

            return ValueTask.CompletedTask;
        });

        // Add authorization context headers
        context.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                // Add user context for downstream services
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = user.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);

                if (!string.IsNullOrEmpty(userName))
                    transformContext.ProxyRequest.Headers.Add("X-User-Name", userName);

                if (!string.IsNullOrEmpty(userEmail))
                    transformContext.ProxyRequest.Headers.Add("X-User-Email", userEmail);

                // Add roles
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
                if (roles.Any())
                    transformContext.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));

                // Add permission context
                transformContext.ProxyRequest.Headers.Add("X-Authorization-Source", "Gateway");
                transformContext.ProxyRequest.Headers.Add("X-Authorization-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

                // Add group context if available
                var groupId = user.FindFirst("GroupId")?.Value;
                if (!string.IsNullOrEmpty(groupId))
                    transformContext.ProxyRequest.Headers.Add("X-Group-Id", groupId);
            }
            else
            {
                // Mark as anonymous request
                transformContext.ProxyRequest.Headers.Add("X-User-Type", "Anonymous");
            }

            return ValueTask.CompletedTask;
        });

        // Add response headers
        context.AddResponseTransform(transformContext =>
        {
            transformContext.HttpContext.Response.Headers.Add("X-Gateway-Processed", "true");
            transformContext.HttpContext.Response.Headers.Add("X-Request-Id", transformContext.HttpContext.TraceIdentifier);

            return ValueTask.CompletedTask;
        });

        // Enhanced logging with authorization context
        context.AddRequestTransform(async transformContext =>
        {
            var logger = transformContext.HttpContext.RequestServices
                .GetRequiredService<ILogger<GatewayRequestTransformProvider>>();

            var user = transformContext.HttpContext.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var route = transformContext.HttpContext.Request.Path.Value ?? "";
            var method = transformContext.HttpContext.Request.Method;

            logger.LogInformation("Proxying {Method} {Route} for user {UserId} to {Destination}",
                method, route, userId, transformContext.DestinationPrefix);

            await ValueTask.CompletedTask;
        });
    }
}