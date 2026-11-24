namespace Algorist.Api.Application.Auth;

/// <summary>Identity claims taken from a Google ID token that was validated for this app.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name, string? PictureUrl);

/// <summary>Validates a Google ID token (signature, expiry, issuer and audience = this app's Google client ID).</summary>
public interface IGoogleIdTokenValidator
{
    /// <summary>The identity in the token, or null when the token is not valid for this app.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
