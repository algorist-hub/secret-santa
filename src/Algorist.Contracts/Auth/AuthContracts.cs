using System.Text.Json.Serialization;

namespace Algorist.Contracts.Auth;

/// <summary>API routes shared by the server and the client (relative to the app base address).</summary>
public static class ApiRoutes
{
    public const string PublicSettings = "api/public-settings";
    public const string AuthPrefix = "api/auth/";
    public const string SignIn = "api/auth/sign-in";
    public const string GoogleSignIn = "api/auth/google";
    public const string SignUp = "api/auth/sign-up";
    public const string Renew = "api/auth/renew";
}

/// <summary>Settings a client may read without signing in.</summary>
public sealed record PublicSettingsResponse(int MinimumAgeYears, string? GoogleClientId);

public sealed record PasswordSignInRequest(string Email, string Password);

/// <summary>A Google identity (ID) token obtained by the client from Google.</summary>
public sealed record GoogleSignInRequest(string IdToken);

/// <summary>An authentication token and when it expires.</summary>
public sealed record AuthTokenResponse(string Token, DateTimeOffset ExpiresAtUtc);

[JsonConverter(typeof(JsonStringEnumConverter<GoogleSignInStatus>))]
public enum GoogleSignInStatus
{
    /// <summary>The Google account belongs to a user, who is now signed in.</summary>
    SignedIn,

    /// <summary>No user yet: finish registration on Sign Up using the ticket and profile.</summary>
    RegistrationRequired,
}

public sealed record GoogleSignInResponse(
    GoogleSignInStatus Status,
    string? Token = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? RegistrationTicket = null,
    string? Email = null,
    string? Name = null,
    string? PictureUrl = null);

/// <summary>An avatar image sent with sign-up.</summary>
public sealed record AvatarUpload(string ContentType, string Base64);

public sealed record SignUpRequest
{
    /// <summary>Ignored when <see cref="GoogleRegistrationTicket"/> is set (the ticket's email is used).</summary>
    public string Email { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public DateOnly DateOfBirth { get; init; }

    /// <summary>Required for email sign-ups; must be null or empty with a Google ticket.</summary>
    public string? Password { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Address { get; init; }

    public AvatarUpload? Avatar { get; init; }

    /// <summary>Ticket from <see cref="GoogleSignInResponse.RegistrationTicket"/> for Google sign-ups.</summary>
    public string? GoogleRegistrationTicket { get; init; }

    /// <summary>Google sign-ups only: keep the Google profile picture as avatar (ignored when <see cref="Avatar"/> is set).</summary>
    public bool UseGoogleAvatar { get; init; }
}
