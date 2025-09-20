using Identity.Core.Interfaces;
using Enterprise.Shared.Common.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string _clientId;

    public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _clientId = _configuration["GOOGLE_CLIENT_ID"] ?? throw new InvalidOperationException("Google Client ID not configured");
    }

    public async Task<Result<GoogleUserInfo>> ValidateIdTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            var userInfo = new GoogleUserInfo
            {
                Email = payload.Email,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Picture = payload.Picture,
                EmailVerified = payload.EmailVerified,
                Subject = payload.Subject
            };

            _logger.LogInformation("Google ID token validated successfully for user {Email}", payload.Email);

            return Result<GoogleUserInfo>.Success(userInfo);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token provided");
            return Result<GoogleUserInfo>.Failure("Geçersiz Google token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google ID token");
            return Result<GoogleUserInfo>.Failure("Google token doğrulama hatası");
        }
    }
}