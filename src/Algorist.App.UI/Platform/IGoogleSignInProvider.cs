using Microsoft.AspNetCore.Components;

namespace Algorist.App.UI.Platform;

/// <summary>
/// Google sign-in for the host platform. The web host renders Google's own button (Google Identity Services);
/// a native host can render its own button in the container and use a native Google SDK.
/// Either way the result is a Google ID token, which the API validates.
/// </summary>
public interface IGoogleSignInProvider
{
    /// <summary>
    /// Shows the Google sign-in button inside <paramref name="container"/>. <paramref name="onIdToken"/> runs when the
    /// person completes Google sign-in (never when they cancel). Returns false when Google sign-in is unavailable here.
    /// </summary>
    Task<bool> RenderButtonAsync(ElementReference container, string clientId, string languageCode, Func<string, Task> onIdToken);
}
