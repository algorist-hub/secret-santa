using Algorist.Api.Endpoints;
using Algorist.Contracts.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.Api.Tests;

/// <summary>
/// Test host. Uses the "Testing" environment (no user secrets) and a dummy connection string;
/// no test opens a database connection.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string DummyConnectionString = "Server=localhost;Database=NotUsed;Trusted_Connection=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(ConfigurationKeys.MainDatabaseConnectionString, DummyConnectionString);
        builder.ConfigureServices(services => services.AddSingleton<IEndpointModule, TestEndpoints>());
    }
}
