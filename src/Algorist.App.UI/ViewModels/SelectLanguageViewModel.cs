using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Platform;
using Microsoft.AspNetCore.Components;

namespace Algorist.App.UI.ViewModels;

/// <summary>State and actions of the Select Language screen.</summary>
public sealed class SelectLanguageViewModel(
    LanguageState state,
    ILocalStorage storage,
    ILanguageActivator activator,
    INavigationHistory history,
    NavigationManager navigation)
{
    public IReadOnlyList<LanguageInfo> Options => SupportedLanguages.All;

    /// <summary>The selected option; starts as the current language (pt-BR when none is stored).</summary>
    public string SelectedCode { get; private set; } = state.Current.Code;

    /// <summary>The previous screen, or the initial screen when there is none.</summary>
    public string ReturnUrl
    {
        get
        {
            var previous = history.Previous;
            return string.IsNullOrEmpty(previous) || IsSelectLanguage(previous) ? AppRoutes.Initial : previous;
        }
    }

    public void Select(string code)
    {
        if (SupportedLanguages.Find(code) is { } language)
        {
            SelectedCode = language.Code;
        }
    }

    public async Task ApplyAsync()
    {
        var language = SupportedLanguages.Find(SelectedCode) ?? SupportedLanguages.Default;
        await storage.SetAsync(LanguageState.StorageKey, language.Code);
        state.SelectionRequired = false;
        await activator.ActivateAsync(ReturnUrl);
    }

    public void Cancel()
    {
        // After a failed first-run detection the app continues in pt-BR without storing anything,
        // so detection runs again on the next start.
        state.SelectionRequired = false;
        navigation.NavigateTo(ReturnUrl);
    }

    private static bool IsSelectLanguage(string path) =>
        path.Split('?', '#')[0].Equals(AppRoutes.SelectLanguage, StringComparison.OrdinalIgnoreCase);
}
