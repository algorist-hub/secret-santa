using Algorist.Contracts.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Algorist.Api.Tests;

public class StartupConfigurationTests
{
    [Fact]
    public void Startup_fails_naming_the_key_when_connection_string_is_missing()
    {
        var previous = Environment.GetEnvironmentVariable(ConfigurationKeys.MainDatabaseConnectionString);
        Environment.SetEnvironmentVariable(ConfigurationKeys.MainDatabaseConnectionString, null);
        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.UseEnvironment("Testing"));

            var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains(ConfigurationKeys.MainDatabaseConnectionString, ex.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKeys.MainDatabaseConnectionString, previous);
        }
    }

    [Fact]
    public async Task Startup_succeeds_when_connection_string_is_configured()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }
}
