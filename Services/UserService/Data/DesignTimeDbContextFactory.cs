using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EgitimPlatform.Services.UserService.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();

        // Load .env from repo root if present
        TryLoadDotEnv();

        // Compose connection string from environment variables
        var connectionString = BuildConnectionStringFromEnv();

        optionsBuilder.UseSqlServer(connectionString);
        return new UserDbContext(optionsBuilder.Options);
    }

    private static void TryLoadDotEnv()
    {
        try
        {
            var current = Directory.GetCurrentDirectory();
            var root = Path.GetFullPath(Path.Combine(current, "..", ".."));
            var envPath = Path.Combine(root, ".env");
            if (!File.Exists(envPath)) return;

            foreach (var raw in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.TrimStart().StartsWith("#")) continue;
                var idx = raw.IndexOf('=');
                if (idx <= 0) continue;
                var key = raw[..idx].Trim();
                var value = raw[(idx + 1)..].Trim();
                // Do not overwrite existing environment variables
                if (Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
        catch
        {
            // best-effort: ignore .env load errors at design-time
        }
    }

    private static string BuildConnectionStringFromEnv()
    {
        // Allow full override
        var full = Environment.GetEnvironmentVariable("USER_SERVICE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(full)) return full!;

        var server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong@Passw0rd";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME_USER_SERVICE") ?? "EgitimPlatform_UserService";
        var trust = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true";

        // When running EF CLI on host, map docker service name to localhost
        var hostOverride = Environment.GetEnvironmentVariable("DB_HOST_FOR_MIGRATIONS");
        if (!string.IsNullOrWhiteSpace(hostOverride))
        {
            server = hostOverride!;
        }
        else if (string.Equals(server, "sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            server = "localhost";
        }

        return $"Server={server},{port};Database={dbName};User Id={user};Password={password};TrustServerCertificate={trust};";
    }
}

