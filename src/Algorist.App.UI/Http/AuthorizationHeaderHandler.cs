using System.Net;
using System.Net.Http.Headers;
using Algorist.App.UI.Auth;
using Algorist.Contracts.Auth;

namespace Algorist.App.UI.Http;

/// <summary>
/// Sends the current token with every API request. When the API rejects the token on a
/// non-<c>/api/auth/</c> request, the session ends (the app then shows Sign In).
/// </summary>
public sealed class AuthorizationHeaderHandler(AuthSession session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (session.Token is { } token && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && session.IsSignedIn && !IsAuthRequest(request))
        {
            await session.SignOutAsync();
        }

        return response;
    }

    private static bool IsAuthRequest(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/" + ApiRoutes.AuthPrefix, StringComparison.OrdinalIgnoreCase) == true;
}
