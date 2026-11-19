using Algorist.App.UI.Platform;
using Microsoft.AspNetCore.Components;

namespace Algorist.App.Web.Platform;

/// <summary>
/// Reloads the webapp at the return URL so startup applies the newly stored language
/// and the matching satellite resource assemblies are loaded.
/// </summary>
internal sealed class ReloadLanguageActivator(NavigationManager navigation) : ILanguageActivator
{
    public Task ActivateAsync(string returnUrl)
    {
        navigation.NavigateTo(returnUrl.TrimStart('/'), forceLoad: true, replace: true);
        return Task.CompletedTask;
    }
}
