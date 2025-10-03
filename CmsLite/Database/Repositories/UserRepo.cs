using System;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database.Repositories;

public class UserRepo : IUserRepo
{
    private readonly CmsLiteDbContext dbContext;

    public UserRepo(CmsLiteDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DbSet.User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UsersTable.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<DbSet.User?> GetUserByEmailAsync(string email)
    {
        return await dbContext.UsersTable.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task CreateUserAsync(DbSet.User user)
    {
        await dbContext.UsersTable.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(DbSet.User user)
    {
        dbContext.UsersTable.Update(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            dbContext.UsersTable.Remove(user);
            await dbContext.SaveChangesAsync();
        }
    }
}
