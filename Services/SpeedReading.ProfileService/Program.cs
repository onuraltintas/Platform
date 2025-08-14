using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Auditing.Extensions;
using SpeedReading.ProfileService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpeedReading Profile Service", Version = "v1" });
});

// EF Core provider seçimi: ConnectionString varsa SQL Server, yoksa InMemory
var profileConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(profileConn))
{
    builder.Services.AddDbContext<ProfileDbContext>(opt => opt.UseSqlServer(profileConn));
}
else
{
    builder.Services.AddDbContext<ProfileDbContext>(opt => opt.UseInMemoryDatabase("sr-profile"));
}
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddAuditing(builder.Configuration);
// Audit DB context (same connection as service DB by default)
var auditConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(auditConn))
{
    builder.Services.AddAuditingDbContext(auditConn);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSecurityMiddleware();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// SQL kullanılıyorsa otomatik migration uygula
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
        if (db.Database.IsSqlServer())
        {
            db.Database.Migrate();
        }
    }
    catch
    {
        // ignore migration errors at startup
    }
}

// Audit veritabanını oluştur (migrations yoksa EnsureCreated)
try { await app.Services.EnsureAuditDatabaseCreatedAsync(); } catch { }

app.Run();

