using System.Net;
using System.Text.Json;
using Algorist.Contracts.Errors;

namespace Algorist.Api.Tests;

public class ApiEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_returns_200()
    {
        var response = await factory.CreateClient().GetAsync("/api/health", _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_api_path_returns_404_problem_with_not_found_code()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist", _ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ErrorCodes.NotFound, await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Unhandled_exception_returns_500_with_unexpected_error_code_and_no_details()
    {
        var response = await factory.CreateClient().GetAsync(TestEndpoints.ThrowPath, _ct);
        var body = await response.Content.ReadAsStringAsync(_ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ErrorCodes.UnexpectedError, await ReadCodeAsync(response));
        Assert.DoesNotContain(TestEndpoints.SecretMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    private async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(_ct));
        return doc.RootElement.GetProperty(ApiError.CodeProperty).GetString();
    }
}
