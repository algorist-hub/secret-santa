using Algorist.Api.Application.Common;
using Algorist.Api.Application.Users;
using Algorist.Contracts.Errors;

namespace Algorist.Api.Application.Auth;

/// <summary>Issues a fresh token for a signed-in user who still exists (sliding session).</summary>
public sealed class RenewSession(IUserRepository users, ITokenIssuer tokens)
{
    public async Task<Result<IssuedToken>> ExecuteAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.FindByIdAsync(userId, cancellationToken) is { } user
            ? tokens.Issue(user)
            : Result<IssuedToken>.Fail(ErrorCodes.Unauthorized);
}
