using System.Buffers.Text;
using System.Text.Json;
using Algorist.App.UI.Platform;

namespace Algorist.App.UI.Auth;

/// <summary>The signed-in user as described by the token (decoded, not validated: the server is the authority).</summary>
public sealed record SessionUser(string Id, string? Email, string? Name);

/// <summary>The current authentication token for this device and who it belongs to.</summary>
public sealed class AuthSession(ISecureTokenStorage storage)
{
    public string? Token { get; private set; }

    public SessionUser? User { get; private set; }

    public bool IsSignedIn => Token is not null;

    /// <summary>Raised after signing in or out.</summary>
    public event Action? Changed;

    /// <summary>Stores the token on the device and makes it current.</summary>
    public async Task SignInAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        await storage.SetTokenAsync(token);
        SetCurrent(token);
    }

    /// <summary>Removes the token from the device and from memory.</summary>
    public async Task SignOutAsync()
    {
        var wasSignedIn = IsSignedIn;
        Token = null;
        User = null;
        try
        {
            await storage.ClearAsync();
        }
        finally
        {
            if (wasSignedIn)
            {
                Changed?.Invoke();
            }
        }
    }

    /// <summary>Uses an already stored token without storing it again (app start).</summary>
    internal void Restore(string token) => SetCurrent(token);

    private void SetCurrent(string token)
    {
        Token = token;
        User = ReadUser(token);
        Changed?.Invoke();
    }

    internal static SessionUser? ReadUser(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
            var root = payload.RootElement;
            return root.TryGetProperty("sub", out var sub) && sub.GetString() is { } id
                ? new SessionUser(id, Text(root, "email"), Text(root, "name"))
                : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
