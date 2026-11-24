namespace Algorist.App.UI.Auth;

/// <summary>What Google returned for a person who still has to finish registration on Sign Up.</summary>
public sealed record GoogleRegistration(string Ticket, string Email, string? Name, string? PictureUrl);

/// <summary>
/// Carries Google data from Sign In to Sign Up in memory only, so it is gone after a reload
/// (Sign Up then shows the email form).
/// </summary>
public sealed class PendingGoogleRegistration
{
    public GoogleRegistration? Current { get; set; }

    /// <summary>Returns the pending registration and forgets it.</summary>
    public GoogleRegistration? Take()
    {
        var current = Current;
        Current = null;
        return current;
    }
}
