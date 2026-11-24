namespace Algorist.App.UI.Navigation;

public static class AppRoutes
{
    public const string Root = "/";
    public const string SignIn = "/sign-in";
    public const string SignUp = "/sign-up";
    public const string Home = "/home";
    public const string Events = "/events";
    public const string Friends = "/friends";
    public const string Profile = "/profile";
    public const string SelectLanguage = "/select-language";
    public const string NotFound = "/not-found";

    /// <summary>The screen shown when the app starts: Home when signed in, otherwise Sign In.</summary>
    public static string InitialFor(bool signedIn) => signedIn ? Home : SignIn;

    /// <summary>Base-relative link (no leading slash) so the app also works under a non-root base path.</summary>
    public static string Relative(string route) => route.TrimStart('/');
}
