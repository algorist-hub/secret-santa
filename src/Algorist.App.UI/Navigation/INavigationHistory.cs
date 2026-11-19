namespace Algorist.App.UI.Navigation;

/// <summary>Tracks the previous in-app location (base-relative path starting with "/").</summary>
public interface INavigationHistory
{
    string? Previous { get; }

    /// <summary>Forgets the history when the next navigation completes, so that location has no previous screen.</summary>
    void ResetOnNextNavigation();
}
