using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using CmsLiteTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsLiteTests;

public class DirectoryTests
{
    [Fact]
    public async Task CreateDirectory_Success()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();
        var db = scope.ServiceProvider.GetRequiredService<CmsLiteDbContext>();

        var directory = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = null, // Root directory
            Name = "Documents",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        await directoryRepo.CreateDirectoryAsync(directory);

        var created = await directoryRepo.GetDirectoryByIdAsync(directory.Id);
        Assert.NotNull(created);
        Assert.Equal("Documents", created.Name);
        Assert.Equal(0, created.Level); // Root should be level 0
        Assert.Null(created.ParentId);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreateSubdirectory_SetsCorrectLevel()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create root directory
        var rootDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = null,
            Name = "Root",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(rootDir);

        // Create subdirectory
        var subDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "SubFolder",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(subDir);

        var created = await directoryRepo.GetDirectoryByIdAsync(subDir.Id);
        Assert.NotNull(created);
        Assert.Equal(1, created.Level); // Should be level 1
        Assert.Equal(rootDir.Id, created.ParentId);
    }

    [Fact]
    public async Task CreateDirectory_ExceedsMaxNesting_ThrowsException()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create 5 levels: Level 0, 1, 2, 3, 4
        var currentParentId = (string?)null;
        var directories = new List<DbSet.Directory>();

        for (int level = 0; level < 5; level++)
        {
            var dir = new DbSet.Directory
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = CmsLiteTestFactoryAuth.TestTenantId,
                ParentId = currentParentId,
                Name = $"Level{level}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            await directoryRepo.CreateDirectoryAsync(dir);
            directories.Add(dir);
            currentParentId = dir.Id;
        }

        // Attempt to create 6th level (should fail)
        var invalidDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = currentParentId,
            Name = "Level5_ShouldFail",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => directoryRepo.CreateDirectoryAsync(invalidDir));

        Assert.Contains("Maximum directory nesting level (5) exceeded", exception.Message);
    }

    [Fact]
    public async Task GetOrCreateRootDirectory_CreatesWhenNotExists()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        Assert.NotNull(rootDir);
        Assert.Equal("Root", rootDir.Name);
        Assert.Equal(0, rootDir.Level);
        Assert.Null(rootDir.ParentId);
        Assert.Equal(CmsLiteTestFactoryAuth.TestTenantId, rootDir.TenantId);
        Assert.True(rootDir.IsActive);
    }

    [Fact]
    public async Task GetOrCreateRootDirectory_ReturnsExistingRoot()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create first root directory
        var firstRoot = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);
        var firstId = firstRoot.Id;

        // Call again - should return same directory
        var secondRoot = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        Assert.Equal(firstId, secondRoot.Id);
        Assert.Equal(firstRoot.Name, secondRoot.Name);
    }

    [Fact]
    public async Task IsRootDirectory_CorrectlyIdentifiesRoot()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);
        var isRoot = await directoryRepo.IsRootDirectoryAsync(rootDir.Id);

        Assert.True(isRoot);

        // Create subdirectory
        var subDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "SubDirectory",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(subDir);

        var isSubRoot = await directoryRepo.IsRootDirectoryAsync(subDir.Id);
        Assert.False(isSubRoot);
    }

    [Fact]
    public async Task CanCreateSubdirectory_ValidatesMaxDepth()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create directories up to level 3
        var level0 = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var level1 = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = level0.Id,
            Name = "Level1",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(level1);

        var level2 = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = level1.Id,
            Name = "Level2",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(level2);

        var level3 = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = level2.Id,
            Name = "Level3",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(level3);

        // Level 3 should allow one more subdirectory (to level 4)
        var canCreateLevel4 = await directoryRepo.CanCreateSubdirectoryAsync(level3.Id);
        Assert.True(canCreateLevel4);

        // Create level 4
        var level4 = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = level3.Id,
            Name = "Level4",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(level4);

        // Level 4 should NOT allow more subdirectories (would be level 5)
        var canCreateLevel5 = await directoryRepo.CanCreateSubdirectoryAsync(level4.Id);
        Assert.False(canCreateLevel5);
    }

    [Fact]
    public async Task GetDirectoryTreePerTenant_ReturnsHierarchy()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create directory hierarchy
        var root = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var docs = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = root.Id,
            Name = "Documents",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(docs);

        var images = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = root.Id,
            Name = "Images",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(images);

        var tree = await directoryRepo.GetDirectoryTreePerTenant(CmsLiteTestFactoryAuth.TestTenantId);

        Assert.Equal(3, tree.Count); // Root + Documents + Images

        // Verify ordering (by level, then name)
        Assert.Equal("Root", tree[0].Name);
        Assert.Equal(0, tree[0].Level);

        // Should be ordered alphabetically: Documents, Images
        var level1Dirs = tree.Where(d => d.Level == 1).OrderBy(d => d.Name).ToList();
        Assert.Equal("Documents", level1Dirs[0].Name);
        Assert.Equal("Images", level1Dirs[1].Name);
    }
}