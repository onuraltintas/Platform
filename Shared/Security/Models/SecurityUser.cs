using System.Security.Claims;

namespace EgitimPlatform.Shared.Security.Models;

public class SecurityUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? TenantId { get; set; }
    public string? SessionId { get; set; }
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? SecurityStamp { get; set; }
    
    public static SecurityUser FromClaims(ClaimsPrincipal principal)
    {
        return new SecurityUser
        {
            Id = principal.FindFirst(ClaimTypes.UserId)?.Value ?? string.Empty,
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            UserName = principal.FindFirst(ClaimTypes.UserName)?.Value ?? string.Empty,
            FirstName = principal.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty,
            LastName = principal.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty,
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Categories = principal.FindAll(ClaimTypes.Category).Select(c => c.Value).ToList(),
            Permissions = principal.FindAll(ClaimTypes.Permission).Select(c => c.Value).ToList(),
            TenantId = principal.FindFirst(ClaimTypes.TenantId)?.Value,
            SessionId = principal.FindFirst(ClaimTypes.SessionId)?.Value,
            DeviceId = principal.FindFirst(ClaimTypes.DeviceId)?.Value,
            IpAddress = principal.FindFirst(ClaimTypes.IpAddress)?.Value,
            IsEmailConfirmed = bool.Parse(principal.FindFirst(ClaimTypes.IsEmailConfirmed)?.Value ?? "false"),
            IsActive = bool.Parse(principal.FindFirst(ClaimTypes.IsActive)?.Value ?? "false"),
            LastLoginAt = DateTime.TryParse(principal.FindFirst(ClaimTypes.LastLoginAt)?.Value, out var lastLogin) ? lastLogin : null,
            SecurityStamp = principal.FindFirst(ClaimTypes.SecurityStamp)?.Value
        };
    }
    
    public List<Claim> ToClaims()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserId, Id),
            new(ClaimTypes.Email, Email),
            new(ClaimTypes.UserName, UserName),
            new(ClaimTypes.GivenName, FirstName),
            new(ClaimTypes.Surname, LastName),
            new(ClaimTypes.IsEmailConfirmed, IsEmailConfirmed.ToString()),
            new(ClaimTypes.IsActive, IsActive.ToString())
        };
        
        claims.AddRange(Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(Categories.Select(category => new Claim(ClaimTypes.Category, category)));
        claims.AddRange(Permissions.Select(permission => new Claim(ClaimTypes.Permission, permission)));
        
        if (!string.IsNullOrEmpty(TenantId))
            claims.Add(new Claim(ClaimTypes.TenantId, TenantId));
            
        if (!string.IsNullOrEmpty(SessionId))
            claims.Add(new Claim(ClaimTypes.SessionId, SessionId));
            
        if (!string.IsNullOrEmpty(DeviceId))
            claims.Add(new Claim(ClaimTypes.DeviceId, DeviceId));
            
        if (!string.IsNullOrEmpty(IpAddress))
            claims.Add(new Claim(ClaimTypes.IpAddress, IpAddress));
            
        if (LastLoginAt.HasValue)
            claims.Add(new Claim(ClaimTypes.LastLoginAt, LastLoginAt.Value.ToString("O")));
            
        if (!string.IsNullOrEmpty(SecurityStamp))
            claims.Add(new Claim(ClaimTypes.SecurityStamp, SecurityStamp));
            
        return claims;
    }
    
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    
    public bool HasCategory(string category) => Categories.Contains(category, StringComparer.OrdinalIgnoreCase);
    
    public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    
    public bool HasAnyRole(params string[] roles) => roles.Any(HasRole);
    
    public bool HasAnyCategory(params string[] categories) => categories.Any(HasCategory);
    
    public bool HasAnyPermission(params string[] permissions) => permissions.Any(HasPermission);
    
    public bool HasRoleAndCategory(string role, string category) => HasRole(role) && HasCategory(category);
    
    public bool HasAnyRoleWithCategory(string category, params string[] roles) => HasCategory(category) && HasAnyRole(roles);
    
    public bool HasAnyCategoryWithRole(string role, params string[] categories) => HasRole(role) && HasAnyCategory(categories);
}