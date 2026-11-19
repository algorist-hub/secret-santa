using System.Globalization;
using System.Text.RegularExpressions;

namespace Algorist.App.UI.Localization;

/// <summary>Maps a detected client language tag to a supported app language.</summary>
public static partial class LanguageResolver
{
    /// <summary>
    /// Portuguese (any variant) gives pt-BR; any other recognized language gives en;
    /// null, empty, malformed or unknown gives null (the user must choose).
    /// </summary>
    public static LanguageInfo? Resolve(string? languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return null;
        }

        var tag = languageTag.Trim().Replace('_', '-');
        if (!LanguageTagPattern().IsMatch(tag))
        {
            return null;
        }

        var primary = tag.Split('-')[0].ToLowerInvariant();
        if (primary == "pt")
        {
            return SupportedLanguages.PortugueseBrazil;
        }

        return IsKnownLanguage(primary) ? SupportedLanguages.English : null;
    }

    private static bool IsKnownLanguage(string primarySubtag)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(primarySubtag, predefinedOnly: true);
            return !string.IsNullOrEmpty(culture.Name);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[A-Za-z]{2,3}(-[A-Za-z0-9]{1,8})*$")]
    private static partial Regex LanguageTagPattern();
}
