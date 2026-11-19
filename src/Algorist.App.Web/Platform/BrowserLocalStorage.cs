using Algorist.App.UI.Platform;
using Microsoft.JSInterop;

namespace Algorist.App.Web.Platform;

/// <summary><see cref="ILocalStorage"/> backed by <c>window.localStorage</c>.</summary>
internal sealed class BrowserLocalStorage(IJSRuntime js) : ILocalStorage
{
    public async Task<string?> GetAsync(string key) =>
        await js.InvokeAsync<string?>("algoristPlatform.storage.get", key);

    public async Task SetAsync(string key, string value) =>
        await js.InvokeVoidAsync("algoristPlatform.storage.set", key, value);

    public async Task RemoveAsync(string key) =>
        await js.InvokeVoidAsync("algoristPlatform.storage.remove", key);
}
