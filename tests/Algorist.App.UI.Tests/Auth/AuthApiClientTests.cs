using System.Net;
using System.Net.Http.Json;
using System.Text;
using Algorist.App.UI.Http;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.Auth;

public class AuthApiClientTests
{
    private static readonly CancellationToken Ct = Xunit.TestContext.Current.CancellationToken;

    internal static AuthApiClient Create(StubHttpHandler stub)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(ApiClient.Name, c => c.BaseAddress = new Uri("http://localhost/")).ConfigurePrimaryHttpMessageHandler(() => stub);
        return new AuthApiClient(services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>());
    }

    private static HttpResponseMessage Problem(HttpStatusCode status, string code) =>
        new(status) { Content = new StringContent($"{{\"status\":{(int)status},\"code\":\"{code}\"}}", Encoding.UTF8, "application/problem+json") };

    [Fact]
    public async Task Successful_sign_in_returns_the_token()
    {
        var stub = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthTokenResponse("tok", DateTimeOffset.UnixEpoch)),
        });

        var result = await Create(stub).SignInAsync(new PasswordSignInRequest("a@b.com", "x"), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("tok", result.Value!.Token);
        Assert.Equal("/" + ApiRoutes.SignIn, stub.Requests.Single().RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorCodes.TooManyAttempts)]
    [InlineData(HttpStatusCode.Conflict, ErrorCodes.EmailAlreadyRegistered)]
    public async Task Problem_responses_become_error_codes(HttpStatusCode status, string code)
    {
        var result = await Create(new StubHttpHandler(_ => Problem(status, code))).SignInAsync(new PasswordSignInRequest("a@b.com", "x"), Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.ErrorCode);
    }

    [Fact]
    public async Task Response_without_a_code_is_an_unexpected_error()
    {
        var result = await Create(StubHttpHandler.Status(HttpStatusCode.BadGateway)).SignUpAsync(new SignUpRequest(), Ct);

        Assert.Equal(ErrorCodes.UnexpectedError, result.ErrorCode);
    }

    [Fact]
    public async Task Unreachable_server_is_a_network_error()
    {
        var result = await Create(new StubHttpHandler(_ => throw new HttpRequestException("offline"))).GetPublicSettingsAsync(Ct);

        Assert.Equal(ClientErrorCodes.Network, result.ErrorCode);
    }

    [Fact]
    public async Task Public_settings_are_fetched_once_and_retried_after_failure()
    {
        var calls = 0;
        var stub = new StubHttpHandler(_ => ++calls == 1
            ? throw new HttpRequestException("offline")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new PublicSettingsResponse(21, "cid")) });
        var settings = new PublicSettings(Create(stub));

        Assert.Null(await settings.GetAsync(Ct));
        Assert.Equal(new PublicSettingsResponse(21, "cid"), await settings.GetAsync(Ct));
        Assert.Equal(new PublicSettingsResponse(21, "cid"), await settings.GetAsync(Ct));
        Assert.Equal(2, calls);
    }
}
