namespace Algorist.Api.Application.Auth;

/// <summary>Authentication settings. Bound from the <c>Auth</c> section plus the top-level secret keys.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public const int MinimumSigningKeyLength = 32;

    /// <summary>Lifetime of an issued authentication token, in days.</summary>
    public int TokenLifetimeDays { get; set; } = 60;

    /// <summary>Sign-in/sign-up attempts allowed per client address per minute.</summary>
    public int AttemptsPerMinute { get; set; } = 10;

    /// <summary>Symmetric key that signs tokens (from <c>JwtSigningKey</c>).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Google OAuth client ID (from <c>GoogleClientId</c>); null disables Google sign-in.</summary>
    public string? GoogleClientId { get; set; }

    public bool GoogleSignInEnabled => !string.IsNullOrWhiteSpace(GoogleClientId);
}
