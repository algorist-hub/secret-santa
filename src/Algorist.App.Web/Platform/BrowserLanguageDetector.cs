using Algorist.App.UI.Platform;
using Microsoft.JSInterop;

namespace Algorist.App.Web.Platform;

/// <summary>Reads the browser's preferred language (navigator.languages / navigator.language).</summary>
internal sealed class BrowserLanguageDetector(IJSRuntime js) : IClientLanguageDetector
{
    public async Task<string?> DetectAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("algoristPlatform.getBrowserLanguage");
        }
        catch (Exception)
        {
            return null;
        }
    }
}
