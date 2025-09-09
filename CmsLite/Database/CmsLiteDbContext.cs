using System;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database;

public class CmsLiteDbContext : DbContext
{
    public CmsLiteDbContext(DbContextOptions<CmsLiteDbContext> opts) : base(opts) { }
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentItem> ContentItems => Set<DbSet.ContentItem>();
    public Microsoft.EntityFrameworkCore.DbSet<DbSet.ContentVersion> ContentVersions => Set<DbSet.ContentVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbSet.ContentItem>().ToTable("ContentItem");
        modelBuilder.Entity<DbSet.ContentVersion>().ToTable("ContentVersion");
    }


}
public static class DbSet
    {
        public class ContentItem
        {
            public int Id { get; set; }
            public string Tenant { get; set; } = default!;
            public string Resource { get; set; } = default!;
            public int LatestVersion { get; set; }
            public string ContentType { get; set; } = "application/json";
            public long ByteSize { get; set; }
            public string Sha256 { get; set; } = default!;
            public string ETag { get; set; } = default!;
            public DateTime CreatedAtUtc { get; set; }
            public DateTime UpdatedAtUtc { get; set; }
            public int IsDeleted { get; set; }
        }

        public class ContentVersion
        {
            public int Id { get; set; }
            public string Tenant { get; set; } = default!;
            public string Resource { get; set; } = default!;
            public int Version { get; set; }
            public long ByteSize { get; set; }
            public string Sha256 { get; set; } = default!;
            public string ETag { get; set; } = default!;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
