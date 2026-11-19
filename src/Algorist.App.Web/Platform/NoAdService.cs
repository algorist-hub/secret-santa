using Algorist.App.UI.Platform;

namespace Algorist.App.Web.Platform;

/// <summary>Ads are not integrated yet; this implementation does nothing.</summary>
internal sealed class NoAdService : IAdService
{
    public bool IsSupported => false;

    public Task InitializeAsync() => Task.CompletedTask;
}
