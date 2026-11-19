namespace Algorist.App.UI.Platform;

/// <summary>In-app purchases (e.g. "remove ads"). Not available on the web yet.</summary>
public interface IBillingService
{
    bool IsSupported { get; }

    Task<bool> IsRemoveAdsPurchasedAsync();

    Task<PurchaseResult> PurchaseRemoveAdsAsync();
}

public enum PurchaseResult
{
    Purchased,
    Cancelled,
    NotSupported,
    Failed,
}
