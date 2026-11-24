using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Registration;
using Algorist.Contracts.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Tests;

public class StartupConfigurationTests
{
    [Fact]
    public void Startup_fails_naming_the_key_when_connection_string_is_missing()
    {
        using var _ = new EnvironmentVariableScope(ConfigurationKeys.MainDatabaseConnectionString, null);
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Testing"));

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(ConfigurationKeys.MainDatabaseConnectionString, ex.ToString());
    }

    [Fact]
    public async Task Startup_succeeds_when_connection_string_is_configured()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short-key")]
    public void Startup_fails_naming_the_key_when_signing_key_is_missing_or_short(string? key)
    {
        using var _ = new EnvironmentVariableScope(ConfigurationKeys.JwtSigningKey, null);
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting(ConfigurationKeys.MainDatabaseConnectionString, ApiFactory.DummyConnectionString);
            if (key is not null)
            {
                b.UseSetting(ConfigurationKeys.JwtSigningKey, key);
            }
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(ConfigurationKeys.JwtSigningKey, ex.ToString());
    }

    [Fact]
    public async Task Defaults_apply_when_settings_are_not_configured()
    {
        await using var factory = new ApiFactory();

        Assert.Equal(18, factory.Services.GetRequiredService<IOptions<RegistrationOptions>>().Value.MinimumAgeYears);
        Assert.Equal(60, factory.Services.GetRequiredService<IOptions<AuthOptions>>().Value.TokenLifetimeDays);
    }

    [Theory]
    [InlineData(ConfigurationKeys.RegistrationMinimumAgeYears)]
    [InlineData(ConfigurationKeys.AuthTokenLifetimeDays)]
    public void Startup_fails_naming_the_key_when_a_setting_is_below_one(string key)
    {
        using var factory = new ApiFactory().WithWebHostBuilder(b => b.UseSetting(key, "0"));

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(key, ex.ToString());
    }
}

/// <summary>Temporarily sets (or clears) an environment variable.</summary>
internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _previous;

    public EnvironmentVariableScope(string name, string? value)
    {
        _name = name;
        _previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
}
