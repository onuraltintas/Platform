using Enterprise.Shared.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace User.API.Controllers;

[ApiController]
[Route(".well-known/permissions-manifest")] 
[AllowAnonymous] // Internal erişim varsayımı; Gateway'de kapatılabilir
public class PermissionsManifestController : ControllerBase
{
    [HttpGet]
    public ActionResult<PermissionManifest> Get()
    {
        var manifest = new PermissionManifest
        {
            Service = new ManifestServiceInfo
            {
                Name = "User",
                DisplayName = "User Service",
                Version = "1.0.0",
                Endpoint = "/api/v1"
            },
            Permissions = new List<PermissionManifestEntry>
            {
                new() { Name = "user-profiles.read", Resource = "user-profiles", Action = "read", DisplayName = "User Profiles Read", Category = "user-profiles", IsActive = true },
                new() { Name = "user-profiles.write", Resource = "user-profiles", Action = "write", DisplayName = "User Profiles Write", Category = "user-profiles", IsActive = true },
                new() { Name = "user-preferences.read", Resource = "user-preferences", Action = "read", DisplayName = "User Preferences Read", Category = "user-preferences", IsActive = true },
                new() { Name = "user-preferences.write", Resource = "user-preferences", Action = "write", DisplayName = "User Preferences Write", Category = "user-preferences", IsActive = true },
                new() { Name = "gdpr.export", Resource = "gdpr", Action = "export", DisplayName = "GDPR Export", Category = "gdpr", IsActive = true }
            }
        };

        return Ok(manifest);
    }
}

