using System.Security.Claims;
using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Common;
using Algorist.Api.Application.Registration;
using Algorist.Api.Auth;
using Algorist.Api.ErrorHandling;
using Algorist.Api.Infra.Auth;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Endpoints;

/// <summary>Sign-in, Google sign-in, sign-up and session renewal under <c>/api/auth</c>.</summary>
internal sealed class AuthEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth");

        auth.MapPost("/sign-in", async (PasswordSignInRequest request, PasswordSignIn useCase, CancellationToken ct) =>
                ToTokenResult(await useCase.ExecuteAsync(request, ct)))
            .RequireRateLimiting(RateLimiting.AuthPolicy);

        auth.MapPost("/google", async (GoogleSignInRequest request, GoogleSignIn useCase, CancellationToken ct) =>
            {
                var result = await useCase.ExecuteAsync(request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.ErrorCode!);
            })
            .RequireRateLimiting(RateLimiting.AuthPolicy);

        auth.MapPost("/sign-up", async (SignUpRequest request, RegisterUser useCase, CancellationToken ct) =>
                ToTokenResult(await useCase.ExecuteAsync(request, ct)))
            .RequireRateLimiting(RateLimiting.AuthPolicy);

        auth.MapPost("/renew", async (ClaimsPrincipal principal, RenewSession useCase, CancellationToken ct) =>
                Guid.TryParse(principal.FindFirstValue(JwtTokens.SubjectClaim), out var userId)
                    ? ToTokenResult(await useCase.ExecuteAsync(userId, ct))
                    : Problem(ErrorCodes.Unauthorized))
            .RequireAuthorization();

        api.MapGet("/public-settings", (IOptions<AuthOptions> authOptions, IOptions<RegistrationOptions> registration) =>
            Results.Ok(new PublicSettingsResponse(registration.Value.MinimumAgeYears, authOptions.Value.GoogleClientId)));
    }

    private static IResult ToTokenResult(Result<IssuedToken> result) =>
        result.IsSuccess
            ? Results.Ok(new AuthTokenResponse(result.Value!.Token, result.Value.ExpiresAtUtc))
            : Problem(result.ErrorCode!);

    private static IResult Problem(string errorCode) => ProblemResults.Problem(StatusFor(errorCode), errorCode);

    internal static int StatusFor(string errorCode) => errorCode switch
    {
        ErrorCodes.InvalidCredentials or ErrorCodes.GoogleSignInFailed or ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorCodes.EmailAlreadyRegistered => StatusCodes.Status409Conflict,
        ErrorCodes.TooManyAttempts => StatusCodes.Status429TooManyRequests,
        ErrorCodes.UnexpectedError => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };
}
