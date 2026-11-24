using Algorist.Contracts.Auth;
using Algorist.Contracts.Validation;

namespace Algorist.Api.Application.Registration;

/// <summary>Checks an uploaded avatar: allowed type, size limit, and file signature matching the declared type.</summary>
public static class AvatarImage
{
    public static bool TryDecode(AvatarUpload upload, out byte[]? content)
    {
        content = null;
        if (!AvatarRules.IsAllowedContentType(upload.ContentType)
            || string.IsNullOrEmpty(upload.Base64)
            || upload.Base64.Length > (AvatarRules.MaxBytes + 2) / 3 * 4)
        {
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(upload.Base64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length == 0 || bytes.Length > AvatarRules.MaxBytes || !HasSignature(upload.ContentType, bytes))
        {
            return false;
        }

        content = bytes;
        return true;
    }

    private static bool HasSignature(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/jpeg" => bytes.StartsWith((byte[])[0xFF, 0xD8, 0xFF]),
        "image/png" => bytes.StartsWith((byte[])[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
        _ => false,
    };
}
