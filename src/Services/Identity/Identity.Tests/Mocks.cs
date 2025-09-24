using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tests;

public static class Mocks
{
    public static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }
}

