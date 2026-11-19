namespace Algorist.Api.Tests;

public class ServerLocalizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("en", "Hello!")]
    [InlineData("en-GB", "Hello!")]
    [InlineData("pt-BR", "Olá!")]
    [InlineData("fr", "Olá!")]
    [InlineData(null, "Olá!")]
    public async Task Server_content_uses_request_language_with_pt_BR_fallback(string? acceptLanguage, string expected)
    {
        var client = factory.CreateClient();
        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        var text = await client.GetStringAsync(TestEndpoints.LocalizedPath, _ct);

        Assert.Equal(expected, text);
    }
}
