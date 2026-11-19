using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Algorist.App.UI.Navigation;

public sealed class NavigationHistory : INavigationHistory, IDisposable
{
    private readonly NavigationManager _navigation;
    private string _current;
    private bool _resetPending;

    public NavigationHistory(NavigationManager navigation)
    {
        _navigation = navigation;
        _current = ToPath(navigation.Uri);
        _navigation.LocationChanged += OnLocationChanged;
    }

    public string? Previous { get; private set; }

    public void ResetOnNextNavigation() => _resetPending = true;

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var next = ToPath(e.Location);
        if (_resetPending)
        {
            _resetPending = false;
            Previous = null;
        }
        else if (!string.Equals(next, _current, StringComparison.OrdinalIgnoreCase))
        {
            Previous = _current;
        }

        _current = next;
    }

    private string ToPath(string uri) => "/" + _navigation.ToBaseRelativePath(uri);
}
