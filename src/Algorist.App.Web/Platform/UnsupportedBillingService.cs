using Algorist.App.UI.Platform;

namespace Algorist.App.Web.Platform;

/// <summary>In-app purchases are not available on the web yet.</summary>
internal sealed class UnsupportedBillingService : IBillingService
{
    public bool IsSupported => false;

    public Task<bool> IsRemoveAdsPurchasedAsync() => Task.FromResult(false);

    public Task<PurchaseResult> PurchaseRemoveAdsAsync() => Task.FromResult(PurchaseResult.NotSupported);
}
