using Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class DemoUserSeedingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoUserSeedingService> _logger;

    public DemoUserSeedingService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IHostEnvironment environment,
        ILogger<DemoUserSeedingService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedDemoUsersAsync()
    {
        // Only seed demo users in Development environment
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation("Skipping demo user seeding in {Environment} environment", _environment.EnvironmentName);
            return;
        }

        var demoUsers = GetDemoUsers();

        foreach (var demoUser in demoUsers)
        {
            await CreateDemoUserIfNotExistsAsync(demoUser);
        }
    }

    private List<DemoUserInfo> GetDemoUsers()
    {
        return new List<DemoUserInfo>
        {
            new("superadmin@demo.com", "SuperAdmin Demo", "SuperAdmin", "SuperAdmin_2024!",
                "Demonstration super administrator account with full system access"),

            new("admin@demo.com", "Admin Demo", "Admin", "Admin_2024!",
                "Demonstration administrator account with broad service management access"),

            new("manager@demo.com", "Manager Demo", "Manager", "Manager_2024!",
                "Demonstration manager account with team and departmental resource access"),

            new("user@demo.com", "Standard User Demo", "User", "User_2024!",
                "Demonstration standard user account with basic application features"),

            new("student@demo.com", "Student Demo", "Student", "Student_2024!",
                "Demonstration student account with access to learning resources"),

            new("guest@demo.com", "Guest Demo", "Guest", "Guest_2024!",
                "Demonstration guest account with limited read-only access")
        };
    }

    private async Task CreateDemoUserIfNotExistsAsync(DemoUserInfo demoUserInfo)
    {
        var existingUser = await _userManager.FindByEmailAsync(demoUserInfo.Email);
        if (existingUser != null)
        {
            _logger.LogDebug("Demo user {Email} already exists", demoUserInfo.Email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = demoUserInfo.Email,
            Email = demoUserInfo.Email,
            EmailConfirmed = true,
            FirstName = demoUserInfo.DisplayName.Split(' ')[0],
            LastName = demoUserInfo.DisplayName.Split(' ').Skip(1).FirstOrDefault() ?? "Demo",
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ProfilePictureUrl = null,
            About = demoUserInfo.Description
        };

        var result = await _userManager.CreateAsync(user, demoUserInfo.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Created demo user: {Email}", demoUserInfo.Email);

            // Assign role
            var roleExists = await _roleManager.RoleExistsAsync(demoUserInfo.RoleName);
            if (roleExists)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, demoUserInfo.RoleName);
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Assigned role {Role} to demo user {Email}",
                        demoUserInfo.RoleName, demoUserInfo.Email);
                }
                else
                {
                    _logger.LogError("Failed to assign role {Role} to demo user {Email}: {Errors}",
                        demoUserInfo.RoleName, demoUserInfo.Email,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogWarning("Role {Role} does not exist for demo user {Email}",
                    demoUserInfo.RoleName, demoUserInfo.Email);
            }
        }
        else
        {
            _logger.LogError("Failed to create demo user {Email}: {Errors}",
                demoUserInfo.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private record DemoUserInfo(
        string Email,
        string DisplayName,
        string RoleName,
        string Password,
        string Description);
}