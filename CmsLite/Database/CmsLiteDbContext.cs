using System;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database;

public class CmsLiteDbContext : DbContext, ICmsLiteDbContext
{
    public CmsLiteDbContext(DbContextOptions<CmsLiteDbContext> opts) : base(opts) { }
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.User> UsersTable => Set<DbSet.User>();
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.UserSession> UserSessionsTable => Set<DbSet.UserSession>();
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.Tenant> TenantsTable => Set<DbSet.Tenant>();
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentItem> ContentItemsTable => Set<DbSet.ContentItem>();
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentVersion> ContentVersionsTable => Set<DbSet.ContentVersion>();

    Microsoft.EntityFrameworkCore.DbSet<DbSet.User> ICmsLiteDbContext.Users => UsersTable;

    Microsoft.EntityFrameworkCore.DbSet<DbSet.UserSession> ICmsLiteDbContext.UserSessions => UserSessionsTable;

    Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentItem> ICmsLiteDbContext.ContentItems => ContentItemsTable;

    Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentVersion> ICmsLiteDbContext.ContentVersions => ContentVersionsTable;

    Microsoft.EntityFrameworkCore.DbSet<DbSet.Tenant> ICmsLiteDbContext.Tenants => TenantsTable;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbSet.User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Id).IsUnique();
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.HasOne<DbSet.Tenant>()
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<DbSet.UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<DbSet.Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Id).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasMany<DbSet.User>()
                .WithOne()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(t => t.ContentItems)
                .WithOne(ci => ci.Tenant)
                .HasForeignKey(ci => ci.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<DbSet.ContentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Resource }).IsUnique();
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.ContentItems)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<DbSet.ContentVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Resource, e.Version }).IsUnique();
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
public static class DbSet
{
    public class ContentItem
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = default!;  // Foreign key
        public string Resource { get; set; } = default!;
        public int LatestVersion { get; set; }
        public string ContentType { get; set; } = "application/json";
        public long ByteSize { get; set; }
        public string Sha256 { get; set; } = default!;
        public string ETag { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int IsDeleted { get; set; }

        // Navigation properties
        public Tenant Tenant { get; set; } = default!;
    }

    public class ContentVersion
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = default!;  // Foreign key
        public string Resource { get; set; } = default!;
        public int Version { get; set; }
        public long ByteSize { get; set; }
        public string Sha256 { get; set; } = default!;
        public string ETag { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }

        // Navigation properties
        public Tenant Tenant { get; set; } = default!;
    }

    public class Tenant
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<ContentItem> ContentItems { get; set; } = new List<ContentItem>();
    }
    public class User
    {
        public string Id { get; set; } = default!;
        public string TenantId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAtUtc { get; set; }

        // Navigation properties
        public Tenant Tenant { get; set; } = default!;
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    }
    public class UserSession
    {
        public string Id { get; set; } = default!;
        public string UserId { get; set; } = default!;

        public DbSet.User User { get; set; } = default!;
        public string JwtToken { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public bool IsRevoked { get; set; } = false;

    }
}


