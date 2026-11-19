namespace Algorist.App.UI.Platform;

/// <summary>Advertisement provider. Monetization is planned; implementations are no-ops for now.</summary>
public interface IAdService
{
    /// <summary>True when the platform can show ads.</summary>
    bool IsSupported { get; }

    Task InitializeAsync();
}
