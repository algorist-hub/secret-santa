using Algorist.Api.Domain.Users;

namespace Algorist.Api.Application.Auth;

/// <summary>Salted one-way password hashing.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

/// <summary>An authentication token and its expiry.</summary>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>Issues authentication tokens for users.</summary>
public interface ITokenIssuer
{
    IssuedToken Issue(User user);
}

/// <summary>What Google confirmed about a person who still has to finish registration.</summary>
public sealed record GoogleRegistrationTicketData(string Subject, string Email, string? Name, string? PictureUrl);

/// <summary>
/// Short-lived, signed tickets carrying a verified Google identity from Google sign-in to sign-up,
/// so a client cannot register arbitrary emails as Google accounts.
/// </summary>
public interface IGoogleRegistrationTicketService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    string Create(GoogleRegistrationTicketData data);

    /// <summary>The ticket's data, or null when it is invalid, tampered with or expired.</summary>
    Task<GoogleRegistrationTicketData?> ReadAsync(string ticket);
}
