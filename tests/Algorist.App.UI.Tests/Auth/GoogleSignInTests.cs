using System.Net;
using Algorist.App.UI.Auth;
using Algorist.App.UI.Components;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Pages;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.App.UI.ViewModels;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.Auth;

public class GoogleSignInButtonTests : AppTestContext
{
    private void ServerSettings(string? googleClientId) =>
        Api.Respond(r => r.RequestUri!.AbsolutePath == "/" + ApiRoutes.PublicSettings
            ? ApiResponses.Json(new PublicSettingsResponse(18, googleClientId))
            : new HttpResponseMessage(HttpStatusCode.NotFound));

    [Fact]
    public void Hidden_when_google_is_not_configured_on_the_server()
    {
        ServerSettings(null);

        var cut = Render<GoogleSignInButton>(p => p.Add(x => x.OnIdToken, _ => { }));

        Assert.Empty(cut.FindAll(".google-sign-in"));
        Assert.Empty(Google.Renders);
    }

    [Fact]
    public void Hidden_when_the_server_is_unreachable()
    {
        Api.Respond(_ => throw new HttpRequestException("offline"));

        var cut = Render<GoogleSignInButton>(p => p.Add(x => x.OnIdToken, _ => { }));

        Assert.Empty(cut.FindAll(".google-sign-in"));
    }

    [Fact]
    public void Rendered_with_the_client_id_and_current_language()
    {
        ServerSettings("client-1.apps.googleusercontent.com");
        LanguageState.Apply(SupportedLanguages.English);

        var cut = Render<GoogleSignInButton>(p => p.Add(x => x.OnIdToken, _ => { }));

        cut.WaitForAssertion(() => Assert.Single(Google.Renders));
        Assert.Equal(("client-1.apps.googleusercontent.com", "en"), Google.Renders[0]);
        Assert.Single(cut.FindAll(".google-sign-in .google-button"));
        Assert.Equal("or", cut.Find(".auth-divider").TextContent.Trim());
    }

    [Fact]
    public void Hidden_when_the_platform_cannot_show_google_sign_in()
    {
        ServerSettings("client-1.apps.googleusercontent.com");
        Google.Available = false;

        var cut = Render<GoogleSignInButton>(p => p.Add(x => x.OnIdToken, _ => { }));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".google-sign-in")));
    }
}

public class GoogleSignInFlowTests : AppTestContext
{
    private SignInViewModel CreateVm() => Services.GetRequiredService<SignInViewModel>();

    [Fact]
    public async Task Registered_google_user_is_signed_in_and_sent_home()
    {
        var token = TestTokens.For("user-g");
        Api.Respond(_ => ApiResponses.Json(new GoogleSignInResponse(GoogleSignInStatus.SignedIn, token, DateTimeOffset.UtcNow.AddDays(60))));

        await CreateVm().GoogleSignInAsync("google-id-token");

        Assert.Contains("google-id-token", Api.Bodies.Single());
        Assert.Equal(token, TokenStorage.Token);
        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public async Task New_google_user_goes_to_sign_up_with_google_data()
    {
        Api.Respond(_ => ApiResponses.Json(new GoogleSignInResponse(
            GoogleSignInStatus.RegistrationRequired,
            RegistrationTicket: "ticket-1",
            Email: "ana@gmail.com",
            Name: "Ana Google",
            PictureUrl: "https://lh3.googleusercontent.com/a/ana")));

        await CreateVm().GoogleSignInAsync("google-id-token");

        Assert.Equal(new GoogleRegistration("ticket-1", "ana@gmail.com", "Ana Google", "https://lh3.googleusercontent.com/a/ana"), PendingGoogle.Current);
        Assert.False(Session.IsSignedIn);
        Assert.Equal(AppRoutes.SignUp, CurrentPath);
    }

    [Fact]
    public async Task Failed_google_sign_in_shows_the_translated_error()
    {
        using var _ = new CultureScope("en");
        Api.Respond(_ => ApiResponses.Problem(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed));
        var vm = CreateVm();

        await vm.GoogleSignInAsync("bad-token");

        Assert.Equal("Could not sign in with Google. Please try again.", vm.ErrorMessage);
        Assert.Null(PendingGoogle.Current);
        Assert.False(Session.IsSignedIn);
    }

    [Fact]
    public void Completing_google_sign_in_from_the_button_updates_the_sign_in_screen()
    {
        using var _ = new CultureScope("en");
        Api.Respond(r => r.RequestUri!.AbsolutePath == "/" + ApiRoutes.PublicSettings
            ? ApiResponses.Json(new PublicSettingsResponse(18, "client-1"))
            : ApiResponses.Problem(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed));
        var cut = Render<SignIn>();
        cut.WaitForAssertion(() => Assert.NotNull(Google.OnIdToken));

        cut.InvokeAsync(() => Google.OnIdToken!("bad-token"));

        cut.WaitForAssertion(() => Assert.Equal("Could not sign in with Google. Please try again.", cut.Find("#sign-in-error").TextContent));
    }
}
