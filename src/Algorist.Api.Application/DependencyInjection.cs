using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Registration;
using Algorist.Contracts.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PasswordSignIn>();
        services.AddScoped<GoogleSignIn>();
        services.AddScoped<RegisterUser>();
        services.AddScoped<RenewSession>();

        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .Configure(o =>
            {
                o.SigningKey = configuration[ConfigurationKeys.JwtSigningKey] ?? string.Empty;
                var googleClientId = configuration[ConfigurationKeys.GoogleClientId];
                o.GoogleClientId = string.IsNullOrWhiteSpace(googleClientId) ? null : googleClientId.Trim();
            })
            .Validate(o => o.TokenLifetimeDays >= 1, $"'{ConfigurationKeys.AuthTokenLifetimeDays}' must be 1 or more.")
            .Validate(o => o.AttemptsPerMinute >= 1, $"'{AuthOptions.SectionName}:{nameof(AuthOptions.AttemptsPerMinute)}' must be 1 or more.")
            .ValidateOnStart();

        services.AddOptions<RegistrationOptions>()
            .BindConfiguration(RegistrationOptions.SectionName)
            .Validate(o => o.MinimumAgeYears >= 1, $"'{ConfigurationKeys.RegistrationMinimumAgeYears}' must be 1 or more.")
            .ValidateOnStart();

        return services;
    }
}
