namespace Algorist.Api.Application.Localization;

/// <summary>
/// Cultures the server can localize content for. Requests without a supported language use <see cref="Default"/>.
/// </summary>
public static class ServerCultures
{
    public const string Default = "pt-BR";

    public static readonly string[] SupportedUICultures = ["pt-BR", "en"];

    public static readonly string[] SupportedCultures = ["pt-BR", "en-GB", "en"];
}
