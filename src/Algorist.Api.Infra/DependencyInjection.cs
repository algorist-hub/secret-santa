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
        return services;
    }
}
