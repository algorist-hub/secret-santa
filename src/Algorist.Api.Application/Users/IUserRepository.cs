using Algorist.Api.Domain.Users;

namespace Algorist.Api.Application.Users;

public enum AddUserResult
{
    Added,

    /// <summary>Another account already uses the email.</summary>
    DuplicateEmail,

    /// <summary>Another account is already linked to the Google account.</summary>
    DuplicateGoogleAccount,
}

/// <summary>Persistence of users. Emails passed in must already be normalized.</summary>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> FindByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);

    /// <summary>Adds the user (and its avatar, if any) and saves; reports unique-key conflicts instead of throwing.</summary>
    Task<AddUserResult> AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>Saves changes made to users returned by this repository.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Read access to uploaded avatars (kept separate so it can move to blob storage).</summary>
public interface IAvatarStore
{
    Task<UserAvatar?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
