using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Algorist.App.UI.Http;
using Algorist.App.UI.Platform;
using Algorist.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace Algorist.App.UI.Auth;

/// <summary>
/// Restores the session at app start: renews a stored token with the server, removes it when rejected,
/// and keeps it when the server cannot be reached.
/// </summary>
public sealed class SessionBootstrapper(
    ISecureTokenStorage storage,
    AuthSession session,
    IHttpClientFactory httpClientFactory,
    ILogger<SessionBootstrapper> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var token = await ReadStoredAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Renew);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await httpClientFactory.CreateClient(ApiClient.Name).SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Server unreachable at start; keeping the stored session.");
            session.Restore(token);
            return;
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await session.SignOutAsync();
                return;
            }

            if (response.IsSuccessStatusCode && await ReadRenewedAsync(response, cancellationToken) is { } renewed)
            {
                await session.SignInAsync(renewed.Token);
                return;
            }

            logger.LogWarning("Session renewal returned {Status}; keeping the stored session.", (int)response.StatusCode);
            session.Restore(token);
        }
    }

    private async Task<AuthTokenResponse?> ReadRenewedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var renewed = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(cancellationToken);
            return string.IsNullOrWhiteSpace(renewed?.Token) ? null : renewed;
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Unreadable session renewal response.");
            return null;
        }
    }

    private async Task<string?> ReadStoredAsync()
    {
        try
        {
            return await storage.GetTokenAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the stored token.");
            return null;
        }
    }
}
