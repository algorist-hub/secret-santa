using Algorist.App.UI.Auth;
using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Platform;
using Algorist.App.UI.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddTransient<SignInViewModel>();
        services.AddTransient<SignUpViewModel>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IImageResizer, BrowserImageResizer>();

        services.AddSingleton<AuthSession>();
        services.AddSingleton<SessionBootstrapper>();
        services.AddSingleton<AuthApiClient>();
        services.AddSingleton<PublicSettings>();
        services.AddSingleton<PendingGoogleRegistration>();
        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        services.AddTransient<LanguageHeaderHandler>();
        services.AddTransient<AuthorizationHeaderHandler>();
        services.AddHttpClient(ApiClient.Name, client => client.BaseAddress = apiBaseAddress)
            .AddHttpMessageHandler<LanguageHeaderHandler>()
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(ApiClient.Name));

        return services;
    }
}
