namespace Algorist.App.UI.Localization;

public static class SupportedLanguages
{
    public static readonly LanguageInfo PortugueseBrazil = new("pt-BR", "pt-BR", "br.svg", "Language_ptBR_Name");

    public static readonly LanguageInfo English = new("en", "en-GB", "gb.svg", "Language_en_Name");

    /// <summary>All supported languages, in display order.</summary>
    public static IReadOnlyList<LanguageInfo> All { get; } = [PortugueseBrazil, English];

    public static LanguageInfo Default => PortugueseBrazil;

    /// <summary>Finds a supported language by its exact code (case-insensitive), or null.</summary>
    public static LanguageInfo? Find(string? code) =>
        All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
}
