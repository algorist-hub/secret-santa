namespace Algorist.App.UI.Platform;

/// <summary>Device-local key/value storage that survives app restarts.</summary>
public interface ILocalStorage
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);

    Task RemoveAsync(string key);
}
