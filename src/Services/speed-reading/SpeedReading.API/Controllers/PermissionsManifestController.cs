using Enterprise.Shared.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpeedReading.API.Controllers;

[ApiController]
[Route(".well-known/permissions-manifest")] 
[AllowAnonymous]
public class PermissionsManifestController : ControllerBase
{
    [HttpGet]
    public ActionResult<PermissionManifest> Get()
    {
        var manifest = new PermissionManifest
        {
            Service = new ManifestServiceInfo
            {
                Name = "SpeedReading",
                DisplayName = "Speed Reading Service",
                Version = "1.0.0",
                Endpoint = "/api/v1"
            },
            Permissions = new List<PermissionManifestEntry>
            {
                new() { Name = "exercises.read", Resource = "exercises", Action = "read", DisplayName = "Exercises Read", Category = "exercises", IsActive = true },
                new() { Name = "exercises.write", Resource = "exercises", Action = "write", DisplayName = "Exercises Write", Category = "exercises", IsActive = true },
                new() { Name = "reading-texts.read", Resource = "reading-texts", Action = "read", DisplayName = "Reading Texts Read", Category = "reading-texts", IsActive = true },
                new() { Name = "reading-texts.write", Resource = "reading-texts", Action = "write", DisplayName = "Reading Texts Write", Category = "reading-texts", IsActive = true },
                new() { Name = "user-reading-profiles.read", Resource = "user-reading-profiles", Action = "read", DisplayName = "User Reading Profiles Read", Category = "user-reading-profiles", IsActive = true }
            }
        };

        return Ok(manifest);
    }
}

