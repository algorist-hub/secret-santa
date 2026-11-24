using Algorist.Api.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Infra.Auth;

/// <summary>Validates Google ID tokens with Google's public keys; the audience must be this app's client ID.</summary>
internal sealed class GoogleIdTokenValidator(IOptions<AuthOptions> options, ILogger<GoogleIdTokenValidator> logger)
    : IGoogleIdTokenValidator
{
    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = options.Value.GoogleClientId;
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });

            return string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email)
                ? null
                : new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogInformation(ex, "Rejected a Google ID token.");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Malformed tokens surface as JSON/format errors; failing to fetch Google's keys also lands here.
            logger.LogWarning(ex, "Could not validate a Google ID token.");
            return null;
        }
    }
}
