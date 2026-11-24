using Algorist.Api.Application.Users;
using Algorist.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Infra.Persistence;

internal sealed class UserRepository(MainDbContext db) : IUserRepository, IAvatarStore
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> FindByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == googleSubject, cancellationToken);

    public async Task<AddUserResult> AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return AddUserResult.Added;
        }
        catch (DbUpdateException)
        {
            // A concurrent registration won the unique index; find out which one, independent of the provider's message.
            db.ChangeTracker.Clear();
            if (await db.Users.AnyAsync(u => u.Email == user.Email, cancellationToken))
            {
                return AddUserResult.DuplicateEmail;
            }

            if (user.GoogleSubject is not null && await db.Users.AnyAsync(u => u.GoogleSubject == user.GoogleSubject, cancellationToken))
            {
                return AddUserResult.DuplicateGoogleAccount;
            }

            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<UserAvatar?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        db.UserAvatars.AsNoTracking().SingleOrDefaultAsync(a => a.UserId == userId, cancellationToken);
}
