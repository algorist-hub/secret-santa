using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Algorist.App.UI.Auth;

/// <summary>Exposes <see cref="AuthSession"/> to Blazor authorization (<c>[Authorize]</c>, <c>AuthorizeView</c>).</summary>
public sealed class AppAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthSession _session;

    public AppAuthenticationStateProvider(AuthSession session)
    {
        _session = session;
        _session.Changed += OnChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(CreateState());

    public void Dispose() => _session.Changed -= OnChanged;

    private void OnChanged() => NotifyAuthenticationStateChanged(Task.FromResult(CreateState()));

    private AuthenticationState CreateState()
    {
        if (!_session.IsSignedIn)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var user = _session.User;
        List<Claim> claims = [];
        if (user is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            if (user.Name is not null)
            {
                claims.Add(new Claim(ClaimTypes.Name, user.Name));
            }

            if (user.Email is not null)
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "token")));
    }
}
