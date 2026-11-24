using Algorist.Api.Application.Auth;
using Algorist.Api.ErrorHandling;
using Algorist.Api.Infra.Auth;
using Algorist.Contracts.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Auth;

internal static class AuthenticationSetup
{
    /// <summary>Bearer-token authentication; failures answer 401 with the <c>UNAUTHORIZED</c> error code.</summary>
    public static IServiceCollection AddBearerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>, TimeProvider>((bearer, auth, time) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = JwtTokens.AuthValidationParameters(auth.Value, time);
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                        await problems.WriteAsync(new ProblemDetailsContext
                        {
                            HttpContext = context.HttpContext,
                            ProblemDetails = ProblemResults.Create(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized),
                        });
                    },
                };
            });
        services.AddAuthorization();
        return services;
    }
}
