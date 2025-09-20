using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Identity.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // Load .env file from the root directory
        var currentDirectory = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(currentDirectory, "..", ".env");
        
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        var connectionString = Environment.GetEnvironmentVariable("IDENTITY_DB_CONNECTION")
            ?? "Host=localhost;Database=PlatformDB;Username=platform_user;Password=VForVan_40!";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}