using System.Text.RegularExpressions;

namespace EgitimPlatform.Gateway.Middleware;

public class ApiVersioningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersioningMiddleware> _logger;
    private readonly Dictionary<string, string> _versionMappings;

    public ApiVersioningMiddleware(RequestDelegate next, ILogger<ApiVersioningMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _versionMappings = new Dictionary<string, string>();

        // Load version mappings from configuration
        var versioningSection = configuration.GetSection("ApiVersioning:VersionMappings");
        foreach (var child in versioningSection.GetChildren())
        {
            _versionMappings[child.Key] = child.Value ?? string.Empty;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalPath = context.Request.Path.Value;
        if (string.IsNullOrEmpty(originalPath))
        {
            await _next(context);
            return;
        }

        var versionInfo = ExtractVersionFromPath(originalPath);
        if (versionInfo != null)
        {
            // Add version headers for downstream services
            context.Request.Headers["X-API-Version"] = versionInfo.Version;
            context.Request.Headers["X-Original-Path"] = originalPath;

            // Rewrite path based on version mapping
            var newPath = RewritePathForVersion(originalPath, versionInfo);
            if (newPath != originalPath)
            {
                context.Request.Path = newPath;
                _logger.LogDebug("API versioning: Rewrote path from {OriginalPath} to {NewPath} for version {Version}",
                    originalPath, newPath, versionInfo.Version);
            }

            // Add version info to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-API-Version"] = versionInfo.Version;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private ApiVersionInfo? ExtractVersionFromPath(string path)
    {
        // Pattern 1: /api/v{version}/... (e.g., /api/v1/users, /api/v2/notifications)
        var versionPattern = @"^/api/v(\d+(?:\.\d+)?)(?:/(.+))?$";
        var match = Regex.Match(path, versionPattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            return new ApiVersionInfo
            {
                Version = $"v{match.Groups[1].Value}",
                Resource = match.Groups[2].Value,
                VersionType = ApiVersionType.Path
            };
        }

        // Pattern 2: Check for version in query string
        // This would be handled in a different way, but for completeness
        
        // Pattern 3: Check for version in headers (X-API-Version)
        if (TryGetVersionFromHeaders(path, out var headerVersion))
        {
            return headerVersion;
        }

        return null;
    }

    private bool TryGetVersionFromHeaders(string path, out ApiVersionInfo? versionInfo)
    {
        versionInfo = null;
        return false;
    }

    private string RewritePathForVersion(string originalPath, ApiVersionInfo versionInfo)
    {
        if (versionInfo.VersionType != ApiVersionType.Path)
            return originalPath;

        // Check if we have a specific mapping for this version
        var versionKey = versionInfo.Version.ToLowerInvariant();
        if (_versionMappings.ContainsKey(versionKey))
        {
            var targetVersion = _versionMappings[versionKey];
            return originalPath.Replace($"/{versionInfo.Version}/", $"/{targetVersion}/");
        }

        // Default behavior: map to service-specific endpoints
        return RewriteToServiceEndpoint(originalPath, versionInfo);
    }

    private string RewriteToServiceEndpoint(string originalPath, ApiVersionInfo versionInfo)
    {
        // Map versioned paths to appropriate service endpoints
        var pathParts = originalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (pathParts.Length < 3) // Should have at least: api, version, service
            return originalPath;

        var version = pathParts[1]; // v1, v2, etc.
        var service = pathParts[2]; // users, notifications, etc.

        // Map to service-specific patterns based on version
        return version.ToLowerInvariant() switch
        {
            "v1" => MapV1Path(originalPath, service),
            "v2" => MapV2Path(originalPath, service),
            "v3" => MapV3Path(originalPath, service),
            _ => originalPath
        };
    }

    private string MapV1Path(string originalPath, string service)
    {
        // V1 mappings - direct mapping to services
        return service.ToLowerInvariant() switch
        {
            "identity" or "auth" => originalPath.Replace("/api/v1/identity", "/api/identity")
                                                 .Replace("/api/v1/auth", "/api/identity"),
            "users" or "user" => originalPath.Replace("/api/v1/users", "/api/users")
                                           .Replace("/api/v1/user", "/api/users"),
            "notifications" or "notification" => originalPath.Replace("/api/v1/notifications", "/api/notifications")
                                                              .Replace("/api/v1/notification", "/api/notifications"),
            "features" or "feature" => originalPath.Replace("/api/v1/features", "/api/features")
                                                  .Replace("/api/v1/feature", "/api/features"),
            _ => originalPath
        };
    }

    private string MapV2Path(string originalPath, string service)
    {
        // V2 mappings - might include different endpoint structures
        return service.ToLowerInvariant() switch
        {
            "identity" or "auth" => originalPath.Replace("/api/v2/identity", "/api/identity")
                                                 .Replace("/api/v2/auth", "/api/identity"),
            "users" or "user" => originalPath.Replace("/api/v2/users", "/api/users")
                                           .Replace("/api/v2/user", "/api/users"),
            "notifications" or "notification" => originalPath.Replace("/api/v2/notifications", "/api/notifications")
                                                              .Replace("/api/v2/notification", "/api/notifications"),
            "features" or "feature" => originalPath.Replace("/api/v2/features", "/api/features")
                                                  .Replace("/api/v2/feature", "/api/features"),
            _ => originalPath
        };
    }

    private string MapV3Path(string originalPath, string service)
    {
        // V3 mappings - future version support
        return service.ToLowerInvariant() switch
        {
            "identity" or "auth" => originalPath.Replace("/api/v3/identity", "/api/identity")
                                                 .Replace("/api/v3/auth", "/api/identity"),
            "users" or "user" => originalPath.Replace("/api/v3/users", "/api/users")
                                           .Replace("/api/v3/user", "/api/users"),
            "notifications" or "notification" => originalPath.Replace("/api/v3/notifications", "/api/notifications")
                                                              .Replace("/api/v3/notification", "/api/notifications"),
            "features" or "feature" => originalPath.Replace("/api/v3/features", "/api/features")
                                                  .Replace("/api/v3/feature", "/api/features"),
            _ => originalPath
        };
    }
}

public class ApiVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public ApiVersionType VersionType { get; set; }
}

public enum ApiVersionType
{
    Path,
    Header,
    QueryString
}