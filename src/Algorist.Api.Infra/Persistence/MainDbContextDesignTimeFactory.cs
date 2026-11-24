using Algorist.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Algorist.Api.Infra.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools. Reads <c>MainDatabaseConnectionString</c> from an environment
/// variable or from the user secrets of Algorist.Api, without starting the web host.
/// </summary>
internal sealed class MainDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    /// <summary>Must match the <c>UserSecretsId</c> in Algorist.Api.csproj.</summary>
    private const string ApiUserSecretsId = "a7581be5-830e-4165-be9c-adcf7e038032";

    public MainDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration[ConfigurationKeys.MainDatabaseConnectionString];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set '{ConfigurationKeys.MainDatabaseConnectionString}' as an environment variable or in the user secrets of Algorist.Api.");
        }

        return new MainDbContext(new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(connectionString).Options);
    }
}
