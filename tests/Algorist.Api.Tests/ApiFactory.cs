using Algorist.Api.Application.Auth;
using Algorist.Api.Endpoints;
using Algorist.Api.Infra.Persistence;
using Algorist.Contracts.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Algorist.Api.Tests;

/// <summary>
/// Test host. Uses the "Testing" environment (no user secrets, no startup migrations), an in-memory SQLite
/// database created from the model, and a fake Google token validator.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string DummyConnectionString = "Server=localhost;Database=NotUsed;Trusted_Connection=True;";
    public const string TestSigningKey = "test-signing-key-0123456789-abcdefghijklmnop";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeGoogleIdTokenValidator Google { get; } = new();

    /// <summary>The app's clock; starts at the real current time.</summary>
    public ManualTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(ConfigurationKeys.MainDatabaseConnectionString, DummyConnectionString);
        builder.UseSetting(ConfigurationKeys.JwtSigningKey, TestSigningKey);
        builder.UseSetting("Auth:AttemptsPerMinute", "100000");
        builder.ConfigureServices(services => services.AddSingleton<IEndpointModule, TestEndpoints>());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<MainDbContext>>();
            services.RemoveAll<DbContextOptions<MainDbContext>>();
            services.AddDbContext<MainDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IGoogleIdTokenValidator>();
            services.AddSingleton<IGoogleIdTokenValidator>(Google);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        _connection.Open();
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<MainDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>Runs <paramref name="action"/> with a fresh <see cref="MainDbContext"/> scope.</summary>
    public async Task<T> WithDbAsync<T>(Func<MainDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<MainDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Google token validator for tests: tokens are registered with <see cref="Add"/>.</summary>
public sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private readonly Dictionary<string, GoogleIdentity> _tokens = [];

    public void Add(string idToken, GoogleIdentity identity) => _tokens[idToken] = identity;

    public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.TryGetValue(idToken, out var identity) ? identity : null);
}
