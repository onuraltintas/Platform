using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpeedReading.ProgressService.Data;

public class ProgressDbContextFactory : IDesignTimeDbContextFactory<ProgressDbContext>
{
    public ProgressDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProgressDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost,1433;Database=SpeedReading_Progress;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        }
        optionsBuilder.UseSqlServer(connectionString);
        return new ProgressDbContext(optionsBuilder.Options);
    }
}

