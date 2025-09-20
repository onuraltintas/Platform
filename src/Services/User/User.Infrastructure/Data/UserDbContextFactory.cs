using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace User.Infrastructure.Data;

/// <summary>
/// Design-time factory for UserDbContext to support EF migrations
/// </summary>
public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    /// <summary>
    /// Create DbContext for design-time operations (migrations)
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>UserDbContext instance</returns>
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden by the actual configuration at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=PlatformDB;Username=platform_user;Password=VForVan_40!");

        return new UserDbContext(optionsBuilder.Options);
    }
}