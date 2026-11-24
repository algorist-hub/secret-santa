using Algorist.App.UI.Auth;
using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Platform;
using Algorist.App.UI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Algorist.App.UI.Tests.TestSupport;

/// <summary>bUnit context with the shared UI services and fake platform services.</summary>
public abstract class AppTestContext : BunitContext
{
    internal FakeLocalStorage Storage { get; } = new();

    internal FakeLanguageActivator Activator { get; } = new();

    internal LanguageState LanguageState { get; } = new();

    internal FakeTokenStorage TokenStorage { get; } = new();

    internal AuthSession Session { get; }

    /// <summary>Answers every API request; by default 404.</summary>
    internal StubHttpHandler Api { get; } = StubHttpHandler.Status(System.Net.HttpStatusCode.NotFound);

    internal FakeGoogleSignInProvider Google { get; } = new();

    internal PendingGoogleRegistration PendingGoogle { get; } = new();

    internal FakeImageResizer Resizer { get; } = new();

    /// <summary>"Today" for the app in tests: 24 November 2026.</summary>
    internal static readonly DateOnly Today = new(2026, 11, 24);

    protected AppTestContext()
    {
        Services.AddLocalization();
        Services.AddSingleton(LanguageState);
        Services.AddSingleton<ILocalStorage>(Storage);
        Services.AddSingleton<ILanguageActivator>(Activator);
        Services.AddScoped<INavigationHistory, NavigationHistory>();
        Services.AddTransient<SelectLanguageViewModel>();

        Session = new AuthSession(TokenStorage);
        Services.AddSingleton(Session);
        Services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();
        // Replace bUnit's placeholder authorization with the real services.
        Services.RemoveAll<IAuthorizationService>();
        Services.RemoveAll<IAuthorizationPolicyProvider>();
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();

        Services.AddHttpClient(ApiClient.Name, c => c.BaseAddress = new Uri("http://localhost/")).ConfigurePrimaryHttpMessageHandler(() => Api);
        Services.AddSingleton<AuthApiClient>();
        Services.AddSingleton<PublicSettings>();
        Services.AddScoped<ErrorMessageTranslator>();
        Services.AddTransient<SignInViewModel>();
        Services.AddSingleton<IGoogleSignInProvider>(Google);
        Services.AddSingleton(PendingGoogle);
        Services.AddTransient<SignUpViewModel>();
        Services.AddSingleton<IImageResizer>(Resizer);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero)));
    }

    /// <summary>Answers public settings with these values and every other request with <paramref name="other"/>.</summary>
    internal void RespondApi(int minimumAgeYears = 18, string? googleClientId = null, Func<HttpRequestMessage, HttpResponseMessage>? other = null) =>
        Api.Respond(r => r.RequestUri!.AbsolutePath == "/" + Algorist.Contracts.Auth.ApiRoutes.PublicSettings
            ? ApiResponses.Json(new Algorist.Contracts.Auth.PublicSettingsResponse(minimumAgeYears, googleClientId))
            : other?.Invoke(r) ?? new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

    /// <summary>Signs in as a test user (token stored in the fake storage).</summary>
    protected Task SignInAsync(string userId = "user-1") => Session.SignInAsync(TestTokens.For(userId));

    protected NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    protected string CurrentPath => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
}
