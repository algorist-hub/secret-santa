using Algorist.Api.Application.Common;
using Algorist.Api.Application.Users;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Algorist.Contracts.Validation;

namespace Algorist.Api.Application.Auth;

/// <summary>Signs in with email and password. Every failure is reported the same way.</summary>
public sealed class PasswordSignIn(IUserRepository users, IPasswordHasher hasher, ITokenIssuer tokens)
{
    // Verified against when there is no account/password, so failures take about as long as real checks.
    private static string? _dummyHash;

    public async Task<Result<IssuedToken>> ExecuteAsync(PasswordSignInRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Result<IssuedToken>.Fail(ErrorCodes.ValidationFailed);
        }

        var user = await users.FindByEmailAsync(RegistrationRules.NormalizeEmail(request.Email), cancellationToken);
        if (user?.PasswordHash is not { } passwordHash)
        {
            hasher.Verify(_dummyHash ??= hasher.Hash(Guid.NewGuid().ToString()), request.Password);
            return Result<IssuedToken>.Fail(ErrorCodes.InvalidCredentials);
        }

        return hasher.Verify(passwordHash, request.Password)
            ? tokens.Issue(user)
            : Result<IssuedToken>.Fail(ErrorCodes.InvalidCredentials);
    }
}
