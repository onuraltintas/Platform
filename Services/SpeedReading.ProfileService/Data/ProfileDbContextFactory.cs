using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpeedReading.ProfileService.Data;

public class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProfileDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost,1433;Database=SpeedReading_Profile;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        }
        optionsBuilder.UseSqlServer(connectionString);
        return new ProfileDbContext(optionsBuilder.Options);
    }
}

