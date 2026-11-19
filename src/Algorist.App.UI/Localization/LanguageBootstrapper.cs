using Algorist.App.UI.Platform;
using Microsoft.Extensions.Logging;

namespace Algorist.App.UI.Localization;

/// <summary>
/// Chooses the language at app start: the stored language, otherwise the detected client language,
/// otherwise pt-BR in memory with <see cref="LanguageState.SelectionRequired"/> set.
/// </summary>
public sealed class LanguageBootstrapper(
    ILocalStorage storage,
    IClientLanguageDetector detector,
    LanguageState state,
    ILogger<LanguageBootstrapper> logger)
{
    public async Task InitializeAsync()
    {
        var stored = SupportedLanguages.Find(await ReadStoredAsync());
        if (stored is not null)
        {
            state.Apply(stored);
            return;
        }

        var detected = LanguageResolver.Resolve(await DetectAsync());
        if (detected is not null)
        {
            state.Apply(detected);
            await StoreAsync(detected);
            return;
        }

        state.Apply(SupportedLanguages.Default);
        state.SelectionRequired = true;
    }

    private async Task<string?> ReadStoredAsync()
    {
        try
        {
            return await storage.GetAsync(LanguageState.StorageKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the stored language.");
            return null;
        }
    }

    private async Task<string?> DetectAsync()
    {
        try
        {
            return await detector.DetectAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Client language detection failed.");
            return null;
        }
    }

    private async Task StoreAsync(LanguageInfo language)
    {
        try
        {
            await storage.SetAsync(LanguageState.StorageKey, language.Code);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not store the detected language.");
        }
    }
}
