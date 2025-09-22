using System;

namespace CmsLite.Database.Repositories;

public interface IUserSessionRepo
{
    Task<DbSet.UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<DbSet.UserSession?> GetActiveSessionByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(DbSet.UserSession session, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(DbSet.UserSession session, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
