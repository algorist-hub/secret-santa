using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared UI services. Each host must also register implementations
    /// of the interfaces in the Algorist.App.UI.Platform namespace.
    /// </summary>
    public static IServiceCollection AddAppUI(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.AddLocalization();
        services.AddSingleton<LanguageState>();
        services.AddSingleton<LanguageBootstrapper>();
        services.AddScoped<INavigationHistory, NavigationHistory>();
        services.AddScoped<ErrorMessageTranslator>();
        services.AddTransient<SelectLanguageViewModel>();

        services.AddTransient<LanguageHeaderHandler>();
        services.AddHttpClient(ApiClient.Name, client => client.BaseAddress = apiBaseAddress)
            .AddHttpMessageHandler<LanguageHeaderHandler>();
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(ApiClient.Name));

        return services;
    }
}
