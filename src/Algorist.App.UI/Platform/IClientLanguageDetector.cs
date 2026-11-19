namespace Algorist.App.UI.Platform;

/// <summary>Detects the preferred language of the client (browser or device).</summary>
public interface IClientLanguageDetector
{
    /// <summary>Returns the preferred language tag (e.g. "pt-BR"), or null when it cannot be detected.</summary>
    Task<string?> DetectAsync();
}
