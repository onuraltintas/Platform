using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ProgressService.Data;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Auditing.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpeedReading Progress Service", Version = "v1" });
});

// EF Core provider seçimi: ConnectionString varsa SQL Server, yoksa InMemory
var progressConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(progressConn))
{
    builder.Services.AddDbContext<ProgressDbContext>(opt => opt.UseSqlServer(progressConn));
}
else
{
    builder.Services.AddDbContext<ProgressDbContext>(opt => opt.UseInMemoryDatabase("sr-progress"));
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
        var db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
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

