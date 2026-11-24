using System.Net;
using System.Text.Json;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Pages;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.App.UI.ViewModels;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.Auth;

public class SignInViewModelTests : AppTestContext
{
    private SignInViewModel CreateVm() => Services.GetRequiredService<SignInViewModel>();

    [Fact]
    public async Task Empty_fields_show_required_and_send_nothing()
    {
        using var _ = new CultureScope("en");
        var vm = CreateVm();

        await vm.SubmitAsync();

        Assert.Equal("This field is required.", vm.EmailError);
        Assert.Equal("This field is required.", vm.PasswordError);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Invalid_email_format_is_reported_without_a_request()
    {
        using var _ = new CultureScope("en");
        var vm = CreateVm();
        vm.Email = "ana@";
        vm.Password = "x";

        await vm.SubmitAsync();

        Assert.Equal("Enter a valid email address.", vm.EmailError);
        Assert.Null(vm.PasswordError);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Successful_sign_in_stores_the_token_and_shows_home()
    {
        var token = TestTokens.For("user-7");
        Api.Respond(_ => ApiResponses.Json(new AuthTokenResponse(token, DateTimeOffset.UtcNow.AddDays(60))));
        var vm = CreateVm();
        vm.Email = " ana@mail.com ";
        vm.Password = "Gift#2026box";

        await vm.SubmitAsync();

        var sent = JsonSerializer.Deserialize<PasswordSignInRequest>(Api.Bodies.Single(), JsonSerializerOptions.Web);
        Assert.Equal(new PasswordSignInRequest("ana@mail.com", "Gift#2026box"), sent);
        Assert.Equal(token, TokenStorage.Token);
        Assert.True(Session.IsSignedIn);
        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public async Task Invalid_credentials_show_the_translated_message_and_keep_the_email()
    {
        using var _ = new CultureScope("pt-BR");
        Api.Respond(_ => ApiResponses.Problem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials));
        var vm = CreateVm();
        vm.Email = "ana@mail.com";
        vm.Password = "Wrong#2026";

        await vm.SubmitAsync();

        Assert.Equal("E-mail ou senha incorretos.", vm.ErrorMessage);
        Assert.Equal("ana@mail.com", vm.Email);
        Assert.False(Session.IsSignedIn);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Too_many_attempts_is_reported()
    {
        using var _ = new CultureScope("en");
        Api.Respond(_ => ApiResponses.Problem(HttpStatusCode.TooManyRequests, ErrorCodes.TooManyAttempts));
        var vm = CreateVm();
        vm.Email = "ana@mail.com";
        vm.Password = "x";

        await vm.SubmitAsync();

        Assert.Equal("Too many attempts. Please try again later.", vm.ErrorMessage);
    }
}

public class SignInPageTests : AppTestContext
{
    [Fact]
    public void Screen_shows_title_fields_and_buttons_in_english()
    {
        using var _ = new CultureScope("en");

        var cut = Render<SignIn>();

        Assert.Equal("Sign In", cut.Find("h1").TextContent);
        Assert.Equal("Email", cut.Find("label[for=sign-in-email]").TextContent);
        Assert.Equal("password", cut.Find("#sign-in-password").GetAttribute("type"));
        Assert.Equal("Sign In", cut.Find("#sign-in-submit").TextContent.Trim());
        Assert.Contains("Don't have an account?", cut.Find(".sign-up-prompt").TextContent);
        Assert.Equal("Sign Up", cut.Find("#go-sign-up").TextContent.Trim());
        Assert.Equal("Change Language", cut.Find("#change-language").TextContent.Trim());
        Assert.Empty(cut.FindAll("nav"));
    }

    [Fact]
    public void Screen_is_translated_in_portuguese()
    {
        using var _ = new CultureScope("pt-BR");

        var cut = Render<SignIn>();

        Assert.Equal("Entrar", cut.Find("h1").TextContent);
        Assert.Contains("Não tem uma conta?", cut.Find(".sign-up-prompt").TextContent);
        Assert.Equal("Mudar idioma", cut.Find("#change-language").TextContent.Trim());
    }

    [Fact]
    public void Sign_up_button_opens_sign_up()
    {
        var cut = Render<SignIn>();

        cut.Find("#go-sign-up").Click();

        Assert.Equal(AppRoutes.SignUp, CurrentPath);
    }

    [Fact]
    public void Change_language_opens_select_language_and_cancel_returns_to_sign_in()
    {
        Navigation.NavigateTo("sign-in");
        var cut = Render<SignIn>();

        cut.Find("#change-language").Click();
        Assert.Equal(AppRoutes.SelectLanguage, CurrentPath);

        Services.GetRequiredService<SelectLanguageViewModel>().Cancel();
        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public void Submitting_empty_form_marks_both_fields_invalid()
    {
        using var _ = new CultureScope("en");
        var cut = Render<SignIn>();

        cut.Find("form").Submit();

        Assert.Contains("is-invalid", cut.Find("#sign-in-email").ClassName);
        Assert.Equal("This field is required.", cut.Find("#sign-in-password-error").TextContent);
    }

    [Fact]
    public void Sign_in_button_is_disabled_while_the_request_runs()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>();
        Api.RespondAsync = r => r.RequestUri!.AbsolutePath == "/" + ApiRoutes.PublicSettings
            ? Task.FromResult(ApiResponses.Json(new PublicSettingsResponse(18, null)))
            : pending.Task;
        var cut = Render<SignIn>();
        cut.Find("#sign-in-email").Change("ana@mail.com");
        cut.Find("#sign-in-password").Change("Gift#2026box");

        cut.Find("form").Submit();

        Assert.True(cut.Find("#sign-in-submit").HasAttribute("disabled"));
        pending.SetResult(ApiResponses.Problem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials));
        cut.WaitForAssertion(() => Assert.False(cut.Find("#sign-in-submit").HasAttribute("disabled")));
        Assert.NotNull(cut.Find("#sign-in-error"));
    }

    [Fact]
    public async Task Signed_in_user_is_sent_home()
    {
        await SignInAsync();

        Render<SignIn>();

        Assert.Equal(AppRoutes.Home, CurrentPath);
    }
}
