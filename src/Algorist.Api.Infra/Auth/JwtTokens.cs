using System.Security.Claims;
using System.Text;
using Algorist.Api.Application.Auth;
using Algorist.Api.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Algorist.Api.Infra.Auth;

/// <summary>Shared JWT settings for authentication tokens and Google registration tickets.</summary>
public static class JwtTokens
{
    public const string Issuer = "amigo-presente";
    public const string AuthAudience = "amigo-presente";
    public const string GoogleRegistrationAudience = "google-registration";

    public const string SubjectClaim = JwtRegisteredClaimNames.Sub;
    public const string EmailClaim = JwtRegisteredClaimNames.Email;
    public const string NameClaim = JwtRegisteredClaimNames.Name;
    public const string PictureClaim = "picture";

    public static SymmetricSecurityKey SigningKey(AuthOptions options) => new(Encoding.UTF8.GetBytes(options.SigningKey));

    /// <summary>Validation of authentication tokens (used by the bearer authentication handler).</summary>
    public static TokenValidationParameters AuthValidationParameters(AuthOptions options, TimeProvider timeProvider) =>
        Parameters(options, AuthAudience, timeProvider);

    internal static TokenValidationParameters Parameters(AuthOptions options, string audience, TimeProvider timeProvider) => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = audience,
        IssuerSigningKey = SigningKey(options),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        RequireExpirationTime = true,
        NameClaimType = NameClaim,
        LifetimeValidator = (_, expires, _, _) => expires is { } e && e > timeProvider.GetUtcNow().UtcDateTime,
    };

    internal static string Create(AuthOptions options, string audience, IEnumerable<Claim> claims, DateTimeOffset now, DateTimeOffset expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(options), SecurityAlgorithms.HmacSha256),
        });
}

internal sealed class JwtTokenIssuer(IOptions<AuthOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    public IssuedToken Issue(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expires = now.AddDays(options.Value.TokenLifetimeDays);
        var token = JwtTokens.Create(
            options.Value,
            JwtTokens.AuthAudience,
            [
                new Claim(JwtTokens.SubjectClaim, user.Id.ToString()),
                new Claim(JwtTokens.EmailClaim, user.Email),
                new Claim(JwtTokens.NameClaim, user.Name),
            ],
            now,
            expires);
        return new IssuedToken(token, expires);
    }
}

internal sealed class GoogleRegistrationTicketService(IOptions<AuthOptions> options, TimeProvider timeProvider)
    : IGoogleRegistrationTicketService
{
    public string Create(GoogleRegistrationTicketData data)
    {
        var now = timeProvider.GetUtcNow();
        List<Claim> claims =
        [
            new(JwtTokens.SubjectClaim, data.Subject),
            new(JwtTokens.EmailClaim, data.Email),
        ];
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            claims.Add(new Claim(JwtTokens.NameClaim, data.Name));
        }

        if (!string.IsNullOrWhiteSpace(data.PictureUrl))
        {
            claims.Add(new Claim(JwtTokens.PictureClaim, data.PictureUrl));
        }

        return JwtTokens.Create(options.Value, JwtTokens.GoogleRegistrationAudience, claims, now, now + IGoogleRegistrationTicketService.Lifetime);
    }

    public async Task<GoogleRegistrationTicketData?> ReadAsync(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            ticket,
            JwtTokens.Parameters(options.Value, JwtTokens.GoogleRegistrationAudience, timeProvider));
        if (!result.IsValid || result.SecurityToken is not JsonWebToken token)
        {
            return null;
        }

        var subject = Claim(token, JwtTokens.SubjectClaim);
        var email = Claim(token, JwtTokens.EmailClaim);
        return subject is null || email is null
            ? null
            : new GoogleRegistrationTicketData(subject, email, Claim(token, JwtTokens.NameClaim), Claim(token, JwtTokens.PictureClaim));
    }

    private static string? Claim(JsonWebToken token, string type) =>
        token.TryGetPayloadValue<string>(type, out var value) ? value : null;
}
