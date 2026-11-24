using System.Threading.RateLimiting;
using Algorist.Api.Application.Auth;
using Algorist.Api.ErrorHandling;
using Algorist.Contracts.Errors;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Auth;

internal static class RateLimiting
{
    public const string AuthPolicy = "auth";

    /// <summary>
    /// Fixed window of <see cref="AuthOptions.AttemptsPerMinute"/> sign-in/sign-up attempts per client IP.
    /// Rejections answer 429 with the <c>TOO_MANY_ATTEMPTS</c> error code.
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        // Behind App Service's front end the client IP arrives in X-Forwarded-For (only the last hop is trusted).
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(AuthPolicy, context =>
            {
                var permits = context.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value.AttemptsPerMinute;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
            limiter.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = ProblemResults.Create(StatusCodes.Status429TooManyRequests, ErrorCodes.TooManyAttempts),
                });
            };
        });
        return services;
    }
}
