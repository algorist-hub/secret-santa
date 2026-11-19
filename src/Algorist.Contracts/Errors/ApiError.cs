namespace Algorist.Contracts.Errors;

/// <summary>
/// Conventions for API error responses (RFC 7807 problem details).
/// </summary>
public static class ApiError
{
    /// <summary>Name of the problem details extension member holding the error code.</summary>
    public const string CodeProperty = "code";
}
