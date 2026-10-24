using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tallix.Application.Users;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly TallixDbContext _db;

    public UserRepository(TallixDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByAuthSubjectAsync(string authSubject, CancellationToken cancellationToken) =>
        _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AuthSubject == authSubject, cancellationToken);

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Forget the rejected entity so this DbContext isn't left in a bad state.
            _db.Entry(user).State = EntityState.Detached;
            return false;
        }
    }
}