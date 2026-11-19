using System.Globalization;
using Algorist.App.UI.Platform;

namespace Algorist.App.UI.Tests.TestSupport;

internal sealed class FakeLocalStorage : ILocalStorage
{
    public Dictionary<string, string> Values { get; } = [];

    public bool ThrowOnRead { get; set; }

    public int Writes { get; private set; }

    public Task<string?> GetAsync(string key)
    {
        if (ThrowOnRead)
        {
            throw new InvalidOperationException("storage unavailable");
        }

        return Task.FromResult(Values.TryGetValue(key, out var v) ? v : null);
    }

    public Task SetAsync(string key, string value)
    {
        Writes++;
        Values[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Values.Remove(key);
        return Task.CompletedTask;
    }
}

internal sealed class FakeLanguageDetector(string? result = null, bool fail = false) : IClientLanguageDetector
{
    public Task<string?> DetectAsync() =>
        fail ? throw new InvalidOperationException("detection failed") : Task.FromResult(result);
}

internal sealed class FakeLanguageActivator : ILanguageActivator
{
    public List<string> Activations { get; } = [];

    public Task ActivateAsync(string returnUrl)
    {
        Activations.Add(returnUrl);
        return Task.CompletedTask;
    }
}

/// <summary>Sets the current (and default) cultures for a test and restores them afterwards.</summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    public CultureScope(string uiCulture, string? culture = null)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture ?? uiCulture);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
    }
}
