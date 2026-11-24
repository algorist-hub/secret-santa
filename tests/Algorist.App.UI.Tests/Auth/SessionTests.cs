using System.Net;
using System.Net.Http.Json;
using Algorist.App.UI.Auth;
using Algorist.App.UI.Http;
using Algorist.App.UI.Pages;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.App.UI.ViewModels;
using Algorist.Contracts.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Algorist.App.UI.Tests.Auth;

public class AuthSessionTests
{
    private readonly FakeTokenStorage _storage = new();

    [Fact]
    public async Task Sign_in_stores_the_token_and_authenticates()
    {
        var session = new AuthSession(_storage);
        using var provider = new AppAuthenticationStateProvider(session);
        var token = TestTokens.For("user-42", "Ana Lima", "ana@mail.com");

        await session.SignInAsync(token);

        Assert.Equal(token, _storage.Token);
        Assert.True(session.IsSignedIn);
        Assert.Equal(new SessionUser("user-42", "ana@mail.com", "Ana Lima"), session.User);
        var state = await provider.GetAuthenticationStateAsync();
        Assert.True(state.User.Identity!.IsAuthenticated);
        Assert.Equal("Ana Lima", state.User.Identity.Name);
    }

    [Fact]
    public async Task Sign_out_clears_storage_and_state_and_notifies()
    {
        var session = new AuthSession(_storage);
        using var provider = new AppAuthenticationStateProvider(session);
        await session.SignInAsync(TestTokens.For("user-42"));
        Task<AuthenticationState>? notified = null;
        provider.AuthenticationStateChanged += t => notified = t;

        await session.SignOutAsync();

        Assert.Null(_storage.Token);
        Assert.False(session.IsSignedIn);
        Assert.Null(session.User);
        Assert.False((await notified!).User.Identity!.IsAuthenticated);
    }

    [Fact]
    public void Unreadable_token_payload_gives_no_user()
    {
        Assert.Null(AuthSession.ReadUser("not-a-token"));
        Assert.Null(AuthSession.ReadUser("a.!!!.c"));
    }
}

public class AuthorizationHeaderHandlerTests
{
    private static async Task<(AuthSession Session, StubHttpHandler Inner, HttpClient Client)> CreateAsync(HttpStatusCode status, bool signedIn = true)
    {
        var session = new AuthSession(new FakeTokenStorage());
        if (signedIn)
        {
            await session.SignInAsync(TestTokens.For("user-1"));
        }

        var inner = StubHttpHandler.Status(status);
        var client = new HttpClient(new AuthorizationHeaderHandler(session) { InnerHandler = inner }) { BaseAddress = new Uri("http://localhost/") };
        return (session, inner, client);
    }

    [Fact]
    public async Task Requests_carry_the_bearer_token()
    {
        var (session, inner, client) = await CreateAsync(HttpStatusCode.OK);

        await client.GetAsync("api/events", Xunit.TestContext.Current.CancellationToken);

        var auth = inner.Requests.Single().Headers.Authorization!;
        Assert.Equal(("Bearer", session.Token), (auth.Scheme, auth.Parameter));
    }

    [Fact]
    public async Task No_header_when_signed_out()
    {
        var (_, inner, client) = await CreateAsync(HttpStatusCode.OK, signedIn: false);

        await client.GetAsync("api/public-settings", Xunit.TestContext.Current.CancellationToken);

        Assert.Null(inner.Requests.Single().Headers.Authorization);
    }

    [Fact]
    public async Task Unauthorized_response_from_the_api_signs_out()
    {
        var (session, _, client) = await CreateAsync(HttpStatusCode.Unauthorized);

        await client.GetAsync("api/events", Xunit.TestContext.Current.CancellationToken);

        Assert.False(session.IsSignedIn);
    }

    [Fact]
    public async Task Unauthorized_response_from_auth_endpoints_keeps_the_session()
    {
        var (session, _, client) = await CreateAsync(HttpStatusCode.Unauthorized);

        await client.PostAsync(ApiRoutes.SignIn, null, Xunit.TestContext.Current.CancellationToken);

        Assert.True(session.IsSignedIn);
    }
}

public class SessionBootstrapperTests
{
    private readonly FakeTokenStorage _storage = new();
    private readonly AuthSession _session;

    public SessionBootstrapperTests() => _session = new AuthSession(_storage);

    private async Task<StubHttpHandler> RunAsync(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var stub = new StubHttpHandler(respond);
        var services = new ServiceCollection();
        services.AddHttpClient(ApiClient.Name, c => c.BaseAddress = new Uri("http://localhost/")).ConfigurePrimaryHttpMessageHandler(() => stub);
        using var provider = services.BuildServiceProvider();
        var bootstrapper = new SessionBootstrapper(_storage, _session, provider.GetRequiredService<IHttpClientFactory>(), NullLogger<SessionBootstrapper>.Instance);

        await bootstrapper.InitializeAsync(Xunit.TestContext.Current.CancellationToken);
        return stub;
    }

    [Fact]
    public async Task No_stored_token_stays_signed_out_without_calling_the_server()
    {
        var stub = await RunAsync(_ => throw new InvalidOperationException("must not be called"));

        Assert.False(_session.IsSignedIn);
        Assert.Empty(stub.Requests);
    }

    [Fact]
    public async Task Accepted_token_is_renewed_and_stored()
    {
        var old = TestTokens.For("user-1", tag: "old");
        var renewed = TestTokens.For("user-1", tag: "new");
        _storage.Token = old;

        var stub = await RunAsync(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthTokenResponse(renewed, DateTimeOffset.UtcNow.AddDays(60))),
        });

        var request = stub.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/" + ApiRoutes.Renew, request.RequestUri!.AbsolutePath);
        Assert.Equal(old, request.Headers.Authorization!.Parameter);
        Assert.Equal(renewed, _storage.Token);
        Assert.Equal(renewed, _session.Token);
    }

    [Fact]
    public async Task Rejected_token_is_removed()
    {
        _storage.Token = TestTokens.For("user-1");

        await RunAsync(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Assert.Null(_storage.Token);
        Assert.False(_session.IsSignedIn);
    }

    [Fact]
    public async Task Unreachable_server_keeps_the_token()
    {
        var token = TestTokens.For("user-1");
        _storage.Token = token;

        await RunAsync(_ => throw new HttpRequestException("offline"));

        Assert.Equal(token, _storage.Token);
        Assert.Equal(token, _session.Token);
    }

    [Fact]
    public async Task Server_error_keeps_the_token()
    {
        var token = TestTokens.For("user-1");
        _storage.Token = token;

        await RunAsync(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.Equal(token, _session.Token);
    }

    [Fact]
    public async Task Unreadable_storage_means_signed_out()
    {
        _storage.ThrowOnRead = true;

        await RunAsync(_ => throw new InvalidOperationException("must not be called"));

        Assert.False(_session.IsSignedIn);
    }
}

public class SessionRoutingTests : AppTestContext
{
    [Fact]
    public void Root_goes_to_sign_in_when_signed_out()
    {
        Render<Pages.Index>();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public async Task Root_goes_to_home_when_signed_in()
    {
        await SignInAsync();

        Render<Pages.Index>();

        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public async Task Select_language_cancel_with_no_history_goes_home_when_signed_in()
    {
        await SignInAsync();
        Navigation.NavigateTo("select-language");
        var vm = Services.GetRequiredService<SelectLanguageViewModel>();

        vm.Cancel();

        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public void Signed_out_deep_link_to_a_menu_screen_shows_sign_in()
    {
        using var _ = new CultureScope("en");
        Navigation.NavigateTo("events");

        Render<AppRoot>();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public async Task Signed_in_user_sees_the_menu_screen()
    {
        using var _ = new CultureScope("en");
        await SignInAsync();
        Navigation.NavigateTo("events");

        var cut = Render<AppRoot>();

        Assert.Equal(AppRoutes.Events, CurrentPath);
        Assert.Equal("Events", cut.Find("h1").TextContent);
    }

    [Fact]
    public async Task Session_ending_while_in_use_shows_sign_in()
    {
        using var _ = new CultureScope("en");
        await SignInAsync();
        Navigation.NavigateTo("friends");
        Render<AppRoot>();

        await Session.SignOutAsync();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public async Task Not_found_links_home_when_signed_in()
    {
        using var _ = new CultureScope("en");
        await SignInAsync();

        var cut = Render<NotFound>();

        var link = cut.Find("a.link1");
        Assert.Equal("home", link.GetAttribute("href"));
        Assert.Equal("Go to Home", link.TextContent.Trim());
    }
}
