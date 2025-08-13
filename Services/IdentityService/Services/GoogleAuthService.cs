using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Google.Apis.Auth;
using System.Text.Json;
using EgitimPlatform.Services.IdentityService.Data;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Models.Entities;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Errors.Exceptions;
using EgitimPlatform.Shared.Security.Services;
using EgitimPlatform.Shared.Security.Models;
using EgitimPlatform.Shared.Logging.Services;
using Microsoft.Extensions.Configuration;

namespace EgitimPlatform.Services.IdentityService.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IdentityDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly IStructuredLogger _logger;
    private readonly IConfiguration _configuration;
    private readonly string _googleClientId;
    private readonly string _googleClientSecret;
    private readonly string _googleRedirectUri;
    private readonly string _googleAuthorizationEndpoint;
    private readonly string _googleTokenEndpoint;
    private readonly string _googleUserInfoEndpoint;
    private readonly string _googleScope;
    
    public GoogleAuthService(
        IdentityDbContext context,
        ITokenService tokenService,
        IPasswordService passwordService,
        IMapper mapper,
        IStructuredLogger logger,
        IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        
        // Load Google OAuth configuration from environment variables
        _googleClientId = _configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId not configured");
        _googleClientSecret = _configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret not configured");
        _googleRedirectUri = _configuration["Google:RedirectUri"] ?? "http://localhost:5002/api/auth/google/callback";
        _googleAuthorizationEndpoint = _configuration["Google:AuthorizationEndpoint"] ?? "https://accounts.google.com/o/oauth2/v2/auth";
        _googleTokenEndpoint = _configuration["Google:TokenEndpoint"] ?? "https://oauth2.googleapis.com/token";
        _googleUserInfoEndpoint = _configuration["Google:UserInfoEndpoint"] ?? "https://www.googleapis.com/oauth2/v2/userinfo";
        _googleScope = _configuration["Google:Scope"] ?? "openid email profile";
    }
    
    public async Task<ApiResponse<AuthResponse>> LoginAsync(GoogleLoginRequest request, string? ipAddress = null)
    {
        try
        {
            var googleUser = await ValidateGoogleTokenAsync(request.IdToken);
            if (googleUser == null)
            {
                _logger.LogSecurityEvent("GoogleLoginFailed", null, new { Reason = "InvalidToken", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid Google token");
            }
            
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email);
            
            if (user == null)
            {
                _logger.LogSecurityEvent("GoogleLoginFailed", null, new { Email = googleUser.Email, Reason = "UserNotFound", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.NOT_FOUND, "User not found. Please register first.");
            }
            
            if (!user.IsActive)
            {
                _logger.LogSecurityEvent("GoogleLoginFailed", user.Id, new { Email = googleUser.Email, Reason = "UserInactive", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.ACCOUNT_LOCKED, "Account is deactivated");
            }
            
            if (user.IsLocked && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogSecurityEvent("GoogleLoginFailed", user.Id, new { Email = googleUser.Email, Reason = "AccountLocked", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.ACCOUNT_LOCKED, "Account is temporarily locked");
            }
            
            // Update last login and user info from Google
            user.LastLoginAt = DateTime.UtcNow;
            user.IsEmailConfirmed = googleUser.EmailVerified;
            user.UpdatedAt = DateTime.UtcNow;
            
            // Reset any failed login attempts
            user.AccessFailedCount = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            
            await _context.SaveChangesAsync();
            
            // Create security user
            var securityUser = await CreateSecurityUser(user);
            
            // Generate tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(securityUser, request.DeviceId, ipAddress);
            
            // Save refresh token
            await SaveRefreshToken(user.Id, tokenResult.RefreshToken, request.DeviceId, ipAddress, "Google");
            
            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto
            };
            
            _logger.LogUserAction(user.Id, "GoogleLogin", new { Email = googleUser.Email, IpAddress = ipAddress });
            
            return ApiResponse<AuthResponse>.Ok(response, "Google login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return ApiResponse<AuthResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during Google login");
        }
    }
    
    public async Task<ApiResponse<RegisterResponse>> RegisterAsync(GoogleRegisterRequest request, string? ipAddress = null)
    {
        try
        {
            var googleUser = await ValidateGoogleTokenAsync(request.IdToken);
            if (googleUser == null)
            {
                _logger.LogSecurityEvent("GoogleRegisterFailed", null, new { Reason = "InvalidToken", IpAddress = ipAddress });
                return ApiResponse<RegisterResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid Google token");
            }
            
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email);
            
            if (existingUser != null)
            {
                return ApiResponse<RegisterResponse>.Fail(ErrorCodes.User.EMAIL_ALREADY_EXISTS, "User already exists with this email address");
            }
            
            // Generate username from email or name
            var userName = await GenerateUniqueUserName(googleUser);
            
            // Create new user
            var user = new User
            {
                UserName = userName,
                Email = googleUser.Email,
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString()), // Random password for Google users
                IsEmailConfirmed = googleUser.EmailVerified,
                IsActive = true
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Assign default Student role
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
            if (studentRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = studentRole.Id,
                    AssignedBy = "Google"
                };
                _context.UserRoles.Add(userRole);
            }
            
            // Assign categories if provided
            if (request.Categories.Any())
            {
                var categories = await _context.Categories
                    .Where(c => request.Categories.Contains(c.Name))
                    .ToListAsync();
                
                foreach (var category in categories)
                {
                    var userCategory = new UserCategory
                    {
                        UserId = user.Id,
                        CategoryId = category.Id,
                        AssignedBy = "Google"
                    };
                    _context.UserCategories.Add(userCategory);
                }
            }
            
            await _context.SaveChangesAsync();
            
            var response = new RegisterResponse
            {
                UserId = user.Id,
                Message = "Google registration successful.",
                RequiresEmailConfirmation = false // Google users are pre-verified
            };
            
            _logger.LogUserAction(user.Id, "GoogleRegister", new { Email = googleUser.Email, IpAddress = ipAddress });
            
            return ApiResponse<RegisterResponse>.Ok(response, "User registered successfully with Google");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google registration");
            return ApiResponse<RegisterResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during Google registration");
        }
    }
    
    public async Task<ApiResponse<AuthResponse>> HandleCallbackAsync(string code, string state)
    {
        try
        {
            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(code);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                _logger.LogWarning("Google callback: Failed to exchange code for tokens or missing ID token.");
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Failed to exchange code for tokens with Google.");
            }
            
            // Validate and get user info
            var googleUser = await ValidateGoogleTokenAsync(tokenResponse.IdToken);
            if (googleUser == null)
            {
                _logger.LogWarning("Google callback: Failed to validate Google token {IdToken}", tokenResponse.IdToken);
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid Google token. Could not validate user information.");
            }
            
            // Check if user exists, if not create them
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email);
            
            if (user == null)
            {
                // Auto-register user since they don't exist
                _logger.LogInformation("User with email {Email} not found. Registering new user via Google.", googleUser.Email);
                
                var registerResult = await RegisterAsync(new GoogleRegisterRequest { IdToken = tokenResponse.IdToken });
                if (!registerResult.Success || string.IsNullOrEmpty(registerResult.Data?.UserId))
                {
                    _logger.LogError("Failed to auto-register user with Google. Reason: {ErrorMessage}", registerResult.Error?.Message ?? "Unknown registration error");
                    return ApiResponse<AuthResponse>.Fail(registerResult.Error?.Code ?? ErrorCodes.INTERNAL_SERVER_ERROR, registerResult.Error?.Message ?? "Failed to register new user with Google.");
                }
                
                // Fetch the newly created user
                user = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                    .AsNoTracking() // Use AsNoTracking for read-only operation
                    .FirstOrDefaultAsync(u => u.Id == registerResult.Data.UserId);
            }
            
            if (user == null)
            {
                _logger.LogError("Could not find user with email {Email} after login/registration attempt.", googleUser.Email);
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to create or retrieve user after Google authentication.");
            }
            
            // Create security user and generate tokens
            var securityUser = await CreateSecurityUser(user);
            var tokenResult = await _tokenService.GenerateTokenAsync(securityUser);
            
            // Save refresh token
            await SaveRefreshToken(user.Id, tokenResult.RefreshToken, null, null, "GoogleCallback");
            
            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto
            };
            
            return ApiResponse<AuthResponse>.Ok(response, "Google authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google callback handling");
            return ApiResponse<AuthResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during Google authentication");
        }
    }
    
    public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            });
            
            return new GoogleUserInfo
            {
                Sub = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                GivenName = payload.GivenName,
                FamilyName = payload.FamilyName,
                Picture = payload.Picture,
                EmailVerified = payload.EmailVerified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }
    
    public string GetAuthorizationUrl(string state)
    {
        return $"{_googleAuthorizationEndpoint}?" +
               $"client_id={_googleClientId}&" +
               $"redirect_uri={Uri.EscapeDataString(_googleRedirectUri)}&" +
               $"scope={Uri.EscapeDataString(_googleScope)}&" +
               $"response_type=code&" +
               $"state={state}&" +
               $"access_type=offline&" +
               $"prompt=consent";
    }
    
    private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code)
    {
        try
        {
            using var httpClient = new HttpClient();
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _googleClientId,
                ["client_secret"] = _googleClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _googleRedirectUri
            };
            
            var response = await httpClient.PostAsync(_googleTokenEndpoint, 
                new FormUrlEncodedContent(tokenRequest));
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for tokens");
            return null;
        }
    }
    
    private async Task<string> GenerateUniqueUserName(GoogleUserInfo googleUser)
    {
        var baseUserName = googleUser.Email.Split('@')[0];
        var userName = baseUserName;
        var counter = 1;
        
        while (await _context.Users.AnyAsync(u => u.UserName == userName))
        {
            userName = $"{baseUserName}{counter}";
            counter++;
        }
        
        return userName;
    }
    
    private async Task<SecurityUser> CreateSecurityUser(User user)
    {
        var roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList();
        var categories = user.UserCategories.Where(uc => uc.IsActive).Select(uc => uc.Category.Name).ToList();
        
        // Get permissions from roles
        var roleIds = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToList();
        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsActive)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
        
        return new SecurityUser
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Roles = roles,
            Categories = categories,
            Permissions = permissions,
            IsEmailConfirmed = user.IsEmailConfirmed,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            SecurityStamp = user.SecurityStamp
        };
    }
    
    private async Task SaveRefreshToken(string userId, string token, string? deviceId, string? ipAddress, string? userAgent = null)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            DeviceId = deviceId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }
}

public class GoogleTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}