using Algorist.App.UI.Platform;

namespace Algorist.App.Web.Platform;

/// <summary>
/// Web token storage. Browsers have no truly secure store, so the token lives in localStorage;
/// native hosts should use platform secure storage instead.
/// </summary>
internal sealed class LocalStorageTokenStorage(ILocalStorage storage) : ISecureTokenStorage
{
    private const string TokenKey = "app.auth-token";

    public Task<string?> GetTokenAsync() => storage.GetAsync(TokenKey);

    public Task SetTokenAsync(string token) => storage.SetAsync(TokenKey, token);

    public Task ClearAsync() => storage.RemoveAsync(TokenKey);
}
