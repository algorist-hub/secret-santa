using System.Net;
using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Resources;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Algorist.App.UI.Tests.Localization;

public class ErrorMessageTranslatorTests
{
    private static ErrorMessageTranslator CreateTranslator()
    {
        var services = new ServiceCollection().AddLogging().AddLocalization().BuildServiceProvider();
        return new ErrorMessageTranslator(services.GetRequiredService<IStringLocalizer<AppStrings>>());
    }

    [Theory]
    [InlineData("en", ErrorCodes.NotFound, "The requested item was not found.")]
    [InlineData("pt-BR", ErrorCodes.NotFound, "O item solicitado não foi encontrado.")]
    [InlineData("en", ErrorCodes.UnexpectedError, "An unexpected error occurred. Please try again.")]
    [InlineData("en", ErrorCodes.InvalidCredentials, "Email or password is incorrect.")]
    [InlineData("pt-BR", ErrorCodes.InvalidCredentials, "E-mail ou senha incorretos.")]
    [InlineData("en", ErrorCodes.TooManyAttempts, "Too many attempts. Please try again later.")]
    [InlineData("en", ErrorCodes.EmailAlreadyRegistered, "This email is already registered. Try signing in.")]
    [InlineData("pt-BR", ErrorCodes.Unauthorized, "Sua sessão expirou. Entre novamente.")]
    public void Known_codes_are_translated_into_the_current_language(string culture, string code, string expected)
    {
        using var _ = new CultureScope(culture);

        Assert.Equal(expected, CreateTranslator().Translate(code));
    }

    [Theory]
    [InlineData("SOMETHING_NEW")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_codes_get_the_generic_message(string? code)
    {
        using var _ = new CultureScope("en");

        Assert.Equal("Something went wrong. Please try again.", CreateTranslator().Translate(code));
    }
}

public class LanguageHeaderHandlerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task Requests_carry_the_current_language()
    {
        using var _ = new CultureScope("pt-BR");
        var state = new LanguageState();
        state.Apply(SupportedLanguages.English);
        var inner = new CapturingHandler();
        using var client = new HttpClient(new LanguageHeaderHandler(state) { InnerHandler = inner }) { BaseAddress = new Uri("http://localhost/") };
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr");

        await client.GetAsync("api/health", Xunit.TestContext.Current.CancellationToken);

        var language = Assert.Single(inner.Request!.Headers.AcceptLanguage);
        Assert.Equal("en", language.Value);
    }
}
