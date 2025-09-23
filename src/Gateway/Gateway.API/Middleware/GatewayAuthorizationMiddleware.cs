using Gateway.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text.Json;

namespace Gateway.API.Middleware;

/// <summary>
/// Gateway-level authorization middleware that checks permissions before proxying requests
/// </summary>
public class GatewayAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IGatewayPermissionService _permissionService;
    private readonly ILogger<GatewayAuthorizationMiddleware> _logger;

    public GatewayAuthorizationMiddleware(
        RequestDelegate next,
        IGatewayPermissionService permissionService,
        ILogger<GatewayAuthorizationMiddleware> logger)
    {
        _next = next;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip authorization for health checks and swagger
            if (ShouldSkipAuthorization(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract route information
            var route = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            _logger.LogDebug("Checking authorization for {Method} {Route}", method, route);

            // Check if user has permission for this route
            var hasPermission = await _permissionService.HasPermissionAsync(
                context.User,
                route,
                method,
                context.RequestAborted);

            if (!hasPermission)
            {
                await HandleUnauthorizedAsync(context, route, method);
                return;
            }

            // Add authorization context headers for downstream services
            AddAuthorizationHeaders(context);

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gateway authorization middleware");
            await HandleAuthorizationErrorAsync(context);
        }
    }

    private static bool ShouldSkipAuthorization(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/metrics",
            "/.well-known"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task HandleUnauthorizedAsync(HttpContext context, string route, string method)
    {
        var user = context.User;
        var userId = user.Identity?.Name ?? "anonymous";

        _logger.LogWarning("Authorization denied for user {UserId} accessing {Method} {Route}",
            userId, method, route);

        context.Response.StatusCode = user.Identity?.IsAuthenticated == true
            ? (int)HttpStatusCode.Forbidden
            : (int)HttpStatusCode.Unauthorized;

        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Authorization denied",
            message = user.Identity?.IsAuthenticated == true
                ? "You don't have permission to access this resource"
                : "Authentication required",
            route,
            method,
            timestamp = DateTime.UtcNow,
            requestId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private async Task HandleAuthorizationErrorAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Authorization error",
            message = "An error occurred while checking permissions",
            timestamp = DateTime.UtcNow,
            requestId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static void AddAuthorizationHeaders(HttpContext context)
    {
        var user = context.User;

        // Add user context headers for downstream services
        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var userEmail = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var roles = string.Join(",", user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value));

            if (!string.IsNullOrEmpty(userId))
                context.Request.Headers.Add("X-User-Id", userId);

            if (!string.IsNullOrEmpty(userName))
                context.Request.Headers.Add("X-User-Name", userName);

            if (!string.IsNullOrEmpty(userEmail))
                context.Request.Headers.Add("X-User-Email", userEmail);

            if (!string.IsNullOrEmpty(roles))
                context.Request.Headers.Add("X-User-Roles", roles);

            // Add authorization timestamp
            context.Request.Headers.Add("X-Authorization-Timestamp", DateTimeOffset.UtcNow.ToString("O"));
        }
    }
}