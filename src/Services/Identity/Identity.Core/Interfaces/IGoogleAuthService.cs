using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IGoogleAuthService
{
    Task<Result<GoogleUserInfo>> ValidateIdTokenAsync(string idToken);
}

public class GoogleUserInfo
{
    public string Email { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
    public string? Subject { get; set; }
}