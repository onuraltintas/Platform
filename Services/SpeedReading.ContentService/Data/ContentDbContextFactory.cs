using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpeedReading.ContentService.Data;

public class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    public ContentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost,1433;Database=SpeedReading_Content;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        }
        optionsBuilder.UseSqlServer(connectionString);
        return new ContentDbContext(optionsBuilder.Options);
    }
}

