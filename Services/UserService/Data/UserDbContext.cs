using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Services.UserService.Models.Entities;

namespace EgitimPlatform.Services.UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Avatar); //.HasMaxLength(500); // Bu satırı yorumluyorum
            entity.Property(x => x.Bio).HasMaxLength(1000);
            entity.Property(x => x.Gender).HasMaxLength(20);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Country).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.Website).HasMaxLength(500);
        });

        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("UserSettings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Theme).HasMaxLength(50).HasDefaultValue("light");
            entity.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("tr");
            entity.Property(x => x.TimeZone).HasMaxLength(100).HasDefaultValue("Europe/Istanbul");
        });
    }
}

