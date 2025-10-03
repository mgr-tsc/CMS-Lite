using System;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database.Repositories;

public class UserSessionRepo : IUserSessionRepo
{
    private readonly CmsLiteDbContext dbContext;

    public UserSessionRepo(CmsLiteDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DbSet.UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserSessionsTable.Include(e => e.User).FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken: cancellationToken);
    }

    public async Task CreateSessionAsync(DbSet.UserSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.UserSessionsTable.AddAsync(session, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSessionAsync(DbSet.UserSession session, CancellationToken cancellationToken = default)
    {
        dbContext.UserSessionsTable.Update(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionByIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            session.IsRevoked = true;
            session.JwtToken = string.Empty;
            await UpdateSessionAsync(session, cancellationToken);
        }
    }

    public Task<DbSet.UserSession?> GetActiveSessionByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserSessionsTable.FirstOrDefaultAsync(s => s.User.Id == userId && s.IsRevoked == false && s.ExpiresAtUtc > DateTime.UtcNow, cancellationToken: cancellationToken);
    }
}
