using Algorist.Contracts.Auth;

namespace Algorist.App.UI.Http;

/// <summary>Server settings readable without signing in (minimum age, Google client). Fetched once per app run.</summary>
public sealed class PublicSettings(AuthApiClient api)
{
    private PublicSettingsResponse? _cached;

    /// <summary>The settings, or null when the server could not be reached (retried on the next call).</summary>
    public async Task<PublicSettingsResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var result = await api.GetPublicSettingsAsync(cancellationToken);
        if (result.IsSuccess)
        {
            _cached = result.Value;
        }

        return _cached;
    }
}
