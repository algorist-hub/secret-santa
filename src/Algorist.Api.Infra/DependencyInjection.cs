using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Users;
using Algorist.Api.Infra.Auth;
using Algorist.Api.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.Api.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string mainDatabaseConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mainDatabaseConnectionString);

        services.AddDbContext<MainDbContext>(options => options.UseSqlServer(mainDatabaseConnectionString));
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());
        services.AddScoped<IAvatarStore>(sp => sp.GetRequiredService<UserRepository>());

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IGoogleRegistrationTicketService, GoogleRegistrationTicketService>();
        services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
        return services;
    }

    /// <summary>Applies pending EF Core migrations to the main database.</summary>
    public static async Task MigrateMainDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MainDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
