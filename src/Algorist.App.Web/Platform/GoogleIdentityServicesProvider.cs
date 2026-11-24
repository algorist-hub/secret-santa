using Algorist.App.UI.Platform;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Algorist.App.Web.Platform;

/// <summary>
/// Google sign-in with Google Identity Services: <c>platform.js</c> loads Google's script on demand and renders
/// Google's button; the credential (ID token) comes back through <see cref="CredentialCallback"/>.
/// </summary>
internal sealed class GoogleIdentityServicesProvider(IJSRuntime js, ILogger<GoogleIdentityServicesProvider> logger)
    : IGoogleSignInProvider, IDisposable
{
    private DotNetObjectReference<CredentialCallback>? _callback;

    public async Task<bool> RenderButtonAsync(ElementReference container, string clientId, string languageCode, Func<string, Task> onIdToken)
    {
        _callback?.Dispose();
        _callback = DotNetObjectReference.Create(new CredentialCallback(onIdToken));
        try
        {
            return await js.InvokeAsync<bool>("algoristPlatform.google.renderButton", container, clientId, languageCode, _callback);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Google sign-in is unavailable.");
            return false;
        }
    }

    public void Dispose() => _callback?.Dispose();

    internal sealed class CredentialCallback(Func<string, Task> onIdToken)
    {
        [JSInvokable]
        public Task OnCredential(string credential) => onIdToken(credential);
    }
}
