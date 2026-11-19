using Algorist.App.UI.Platform;

namespace Algorist.App.Web.Platform;

internal static class WebPlatformServiceCollectionExtensions
{
    /// <summary>Registers the browser implementations of the platform interfaces.</summary>
    public static IServiceCollection AddWebPlatform(this IServiceCollection services)
    {
        services.AddSingleton<ILocalStorage, BrowserLocalStorage>();
        services.AddSingleton<ISecureTokenStorage, LocalStorageTokenStorage>();
        services.AddSingleton<IClientLanguageDetector, BrowserLanguageDetector>();
        services.AddScoped<ILanguageActivator, ReloadLanguageActivator>();
        services.AddSingleton<IAdService, NoAdService>();
        services.AddSingleton<IBillingService, UnsupportedBillingService>();
        return services;
    }
}
