using Algorist.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IAppPasswordHasher = Algorist.Api.Application.Auth.IPasswordHasher;

namespace Algorist.Api.Infra.Auth;

/// <summary>PBKDF2 password hashing from ASP.NET Core Identity (salted, versioned format).</summary>
internal sealed class IdentityPasswordHasher : IAppPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // The hasher does not use the user instance; a typed null keeps its generic API.
    private static readonly User NoUser = null!;

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(NoUser, passwordHash, password) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
