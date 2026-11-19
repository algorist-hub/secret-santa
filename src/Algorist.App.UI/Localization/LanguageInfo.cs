namespace Algorist.App.UI.Localization;

/// <summary>A supported app language.</summary>
/// <param name="Code">Language code stored locally and used as UI culture (e.g. "en").</param>
/// <param name="CultureName">Formatting culture for dates and numbers (e.g. "en-GB").</param>
/// <param name="FlagImage">Flag image file name under images/flags.</param>
/// <param name="NameKey">Resource key of the display name (always written in the language itself).</param>
public sealed record LanguageInfo(string Code, string CultureName, string FlagImage, string NameKey)
{
    public string FlagImagePath => $"_content/Algorist.App.UI/images/flags/{FlagImage}";
}
