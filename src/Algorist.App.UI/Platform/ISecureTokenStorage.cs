namespace Algorist.App.UI.Platform;

/// <summary>Storage for authentication tokens, as secure as the platform allows.</summary>
public interface ISecureTokenStorage
{
    Task<string?> GetTokenAsync();

    Task SetTokenAsync(string token);

    Task ClearAsync();
}
