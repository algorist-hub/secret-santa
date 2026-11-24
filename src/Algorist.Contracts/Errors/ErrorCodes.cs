namespace Algorist.Contracts.Errors;

/// <summary>
/// Stable, language-independent error codes returned by the API.
/// The client translates each code into the current language.
/// </summary>
public static class ErrorCodes
{
    public const string UnexpectedError = "UNEXPECTED_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";

    /// <summary>Email/password do not match an account with a password (never says which).</summary>
    public const string InvalidCredentials = "INVALID_CREDENTIALS";

    /// <summary>Too many sign-in or sign-up attempts from this client; try later.</summary>
    public const string TooManyAttempts = "TOO_MANY_ATTEMPTS";

    /// <summary>Google identity token or Google registration ticket invalid, expired or unverified.</summary>
    public const string GoogleSignInFailed = "GOOGLE_SIGN_IN_FAILED";

    /// <summary>Date of birth is below the configured minimum age.</summary>
    public const string Underage = "UNDERAGE";

    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string WeakPassword = "WEAK_PASSWORD";

    /// <summary>Avatar is not JPEG, PNG or WebP, or is larger than allowed.</summary>
    public const string InvalidAvatar = "INVALID_AVATAR";

    /// <summary>Missing, invalid or expired authentication token.</summary>
    public const string Unauthorized = "UNAUTHORIZED";

    public static IReadOnlyList<string> All { get; } =
    [
        UnexpectedError, NotFound, ValidationFailed,
        InvalidCredentials, TooManyAttempts, GoogleSignInFailed, Underage,
        EmailAlreadyRegistered, WeakPassword, InvalidAvatar, Unauthorized,
    ];
}
