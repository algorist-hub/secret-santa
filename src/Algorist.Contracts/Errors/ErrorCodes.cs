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

    public static IReadOnlyList<string> All { get; } = [UnexpectedError, NotFound, ValidationFailed];
}
