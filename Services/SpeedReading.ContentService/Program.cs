using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SpeedReading.ContentService.Data;
using EgitimPlatform.Shared.Security.Extensions;
using EgitimPlatform.Shared.Auditing.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddAuditing(builder.Configuration);
// Audit DB context (same connection as service DB by default)
var auditConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(auditConn))
{
    builder.Services.AddAuditingDbContext(auditConn);
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SpeedReading Content Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// EF Core provider seçimi: ConnectionString varsa SQL Server, yoksa InMemory
var contentConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(contentConn))
{
    builder.Services.AddDbContext<ContentDbContext>(opt => opt.UseSqlServer(contentConn));
}
else
{
    builder.Services.AddDbContext<ContentDbContext>(opt => opt.UseInMemoryDatabase("sr-content"));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseSecurityMiddleware(); // Temporarily disabled for testing

// SQL kullanılıyorsa otomatik migration uygula
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
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

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Minimal seed for InMemory DB (dev/demo)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        if (!db.ExerciseTypes.Any())
        {
            db.ExerciseTypes.AddRange(
                new ExerciseTypeEntity { ExerciseTypeId = Guid.NewGuid(), TypeName = "RSVP", Description = "Rapid Serial Visual Presentation" },
                new ExerciseTypeEntity { ExerciseTypeId = Guid.NewGuid(), TypeName = "Fixation", Description = "Fiksasyon süresi kısaltma" },
                new ExerciseTypeEntity { ExerciseTypeId = Guid.NewGuid(), TypeName = "Saccade", Description = "Sakkad uzunluğu artırma" }
            );
        }
        if (!db.ReadingLevels.Any())
        {
            db.ReadingLevels.AddRange(
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Çocuk", MinAge = 9, MaxAge = 12, MinWPM = 150, MaxWPM = 220, TargetComprehension = 60 },
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Genç", MinAge = 12, MaxAge = 17, MinWPM = 200, MaxWPM = 350, TargetComprehension = 70 },
                new ReadingLevelEntity { LevelId = Guid.NewGuid(), LevelName = "Yetişkin", MinAge = 17, MaxAge = 99, MinWPM = 300, MaxWPM = 500, TargetComprehension = 75 }
            );
        }
        if (!db.Texts.Any())
        {
            db.Texts.Add(new TextEntity
            {
                TextId = Guid.NewGuid(),
                Title = "Örnek Metin",
                DifficultyLevel = "Temel",
                Content = "Bu bir örnek metindir. Hızlı okuma modülü için demo verisi sağlar.",
                WordCount = 12,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        db.SaveChanges();
    }
    catch
    {
        // swallow seed errors in dev
    }
}

app.Run();

