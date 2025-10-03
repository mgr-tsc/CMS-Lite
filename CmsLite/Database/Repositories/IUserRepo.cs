using System;

namespace CmsLite.Database.Repositories;

public interface IUserRepo
{
    Task<DbSet.User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<DbSet.User?> GetUserByEmailAsync(string email);
    Task CreateUserAsync(DbSet.User user);
    Task UpdateUserAsync(DbSet.User user);
    Task DeleteUserAsync(string userId);

}
