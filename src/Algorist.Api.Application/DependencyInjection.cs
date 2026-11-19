using Microsoft.Extensions.DependencyInjection;

namespace Algorist.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddLocalization();
        return services;
    }
}
