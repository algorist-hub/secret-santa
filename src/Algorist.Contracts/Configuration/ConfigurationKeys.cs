namespace Algorist.Contracts.Configuration;

public static class ConfigurationKeys
{
    /// <summary>Main database connection string (environment variable or user secret).</summary>
    public const string MainDatabaseConnectionString = "MainDatabaseConnectionString";

    /// <summary>Key that signs authentication tokens (environment variable or user secret; at least 32 characters).</summary>
    public const string JwtSigningKey = "JwtSigningKey";

    /// <summary>Google OAuth web client ID; when absent, Google sign-in is disabled.</summary>
    public const string GoogleClientId = "GoogleClientId";

    /// <summary>Minimum age, in whole years, required to register (default 18).</summary>
    public const string RegistrationMinimumAgeYears = "Registration:MinimumAgeYears";

    /// <summary>Lifetime of an authentication token, in days (default 60).</summary>
    public const string AuthTokenLifetimeDays = "Auth:TokenLifetimeDays";
}
