namespace Algorist.Contracts.Validation;

/// <summary>Avatar image rules shared by the API and the client.</summary>
public static class AvatarRules
{
    /// <summary>Largest accepted image after the client has resized it.</summary>
    public const int MaxBytes = 1024 * 1024;

    /// <summary>The client reduces images to fit within this many pixels on each side.</summary>
    public const int MaxDimension = 512;

    public static IReadOnlyList<string> AllowedContentTypes { get; } = ["image/jpeg", "image/png", "image/webp"];

    public static bool IsAllowedContentType(string? contentType) =>
        contentType is not null && AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}
