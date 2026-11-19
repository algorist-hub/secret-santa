using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Platform;
using Algorist.App.UI.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.TestSupport;

/// <summary>bUnit context with the shared UI services and fake platform services.</summary>
public abstract class AppTestContext : BunitContext
{
    internal FakeLocalStorage Storage { get; } = new();

    internal FakeLanguageActivator Activator { get; } = new();

    internal LanguageState LanguageState { get; } = new();

    protected AppTestContext()
    {
        Services.AddLocalization();
        Services.AddSingleton(LanguageState);
        Services.AddSingleton<ILocalStorage>(Storage);
        Services.AddSingleton<ILanguageActivator>(Activator);
        Services.AddScoped<INavigationHistory, NavigationHistory>();
        Services.AddTransient<SelectLanguageViewModel>();
    }

    protected NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    protected string CurrentPath => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
}
