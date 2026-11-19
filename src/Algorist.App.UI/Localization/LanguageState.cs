using System.Globalization;

namespace Algorist.App.UI.Localization;

/// <summary>The language currently in effect for this app session.</summary>
public sealed class LanguageState
{
    public const string StorageKey = "app.language";

    public LanguageInfo Current { get; private set; } = SupportedLanguages.Default;

    /// <summary>True when detection failed and the user must pick a language (Select Language screen).</summary>
    public bool SelectionRequired { get; set; }

    /// <summary>Applies the UI language and its formatting culture together.</summary>
    public void Apply(LanguageInfo language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var uiCulture = CultureInfo.GetCultureInfo(language.Code);
        var culture = CultureInfo.GetCultureInfo(language.CultureName);

        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
        CultureInfo.CurrentCulture = culture;

        Current = language;
    }
}
