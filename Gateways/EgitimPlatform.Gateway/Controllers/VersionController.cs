using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly ILogger<VersionController> _logger;
    private readonly IConfiguration _configuration;

    public VersionController(ILogger<VersionController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetVersionInfo()
    {
        var versionHeaders = new Dictionary<string, string>();
        
        foreach (var header in Request.Headers)
        {
            if (header.Key.StartsWith("X-API-") || header.Key.StartsWith("X-Original-"))
            {
                versionHeaders[header.Key] = header.Value.ToString();
            }
        }

        var apiVersioningConfig = _configuration.GetSection("ApiVersioning");
        var supportedVersions = apiVersioningConfig.GetSection("SupportedVersions").Get<string[]>() ?? Array.Empty<string>();

        return Ok(new
        {
            Gateway = "EgitimPlatform API Gateway",
            RequestPath = Request.Path.Value,
            VersionHeaders = versionHeaders,
            SupportedVersions = supportedVersions,
            DefaultVersion = apiVersioningConfig.GetValue<string>("DefaultVersion"),
            Timestamp = DateTime.UtcNow,
            Features = new[]
            {
                "Path-based versioning (/api/v1/, /api/v2/)",
                "Version header forwarding",
                "Dynamic route mapping",
                "Backward compatibility"
            }
        });
    }

    [HttpGet("supported")]
    public IActionResult GetSupportedVersions()
    {
        var apiVersioningConfig = _configuration.GetSection("ApiVersioning");
        var supportedVersions = apiVersioningConfig.GetSection("SupportedVersions").Get<string[]>() ?? Array.Empty<string>();
        var defaultVersion = apiVersioningConfig.GetValue<string>("DefaultVersion");

        return Ok(new
        {
            SupportedVersions = supportedVersions,
            DefaultVersion = defaultVersion,
            VersionMappings = apiVersioningConfig.GetSection("VersionMappings").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>()
        });
    }

    [HttpGet("test/{version}")]
    public IActionResult TestVersion(string version)
    {
        var versionHeaders = Request.Headers
            .Where(h => h.Key.StartsWith("X-API-") || h.Key.StartsWith("X-Original-"))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return Ok(new
        {
            RequestedVersion = version,
            ProcessedHeaders = versionHeaders,
            Path = Request.Path.Value,
            QueryString = Request.QueryString.Value,
            Method = Request.Method,
            Timestamp = DateTime.UtcNow
        });
    }
}