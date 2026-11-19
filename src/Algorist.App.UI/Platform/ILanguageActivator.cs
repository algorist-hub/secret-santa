namespace Algorist.App.UI.Platform;

/// <summary>
/// Makes a newly stored language take effect across the whole app, then shows the return URL.
/// The web implementation reloads the app; a native host may re-render in place.
/// </summary>
public interface ILanguageActivator
{
    Task ActivateAsync(string returnUrl);
}
