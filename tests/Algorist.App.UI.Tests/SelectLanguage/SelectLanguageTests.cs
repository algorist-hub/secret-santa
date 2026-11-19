using System.Globalization;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.App.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SelectLanguagePage = Algorist.App.UI.Pages.SelectLanguage;

namespace Algorist.App.UI.Tests.SelectLanguage;

public class SelectLanguageViewModelTests : AppTestContext
{
    private SelectLanguageViewModel CreateViewModel()
    {
        _ = Services.GetRequiredService<INavigationHistory>(); // start tracking before navigating
        return Services.GetRequiredService<SelectLanguageViewModel>();
    }

    [Fact]
    public void Current_language_is_preselected()
    {
        using var _ = new CultureScope("pt-BR");
        LanguageState.Apply(SupportedLanguages.English);

        Assert.Equal("en", CreateViewModel().SelectedCode);
    }

    [Fact]
    public void Without_stored_language_pt_BR_is_preselected()
    {
        Assert.Equal("pt-BR", CreateViewModel().SelectedCode);
        Assert.Equal(["pt-BR", "en"], CreateViewModel().Options.Select(o => o.Code));
    }

    [Fact]
    public async Task Apply_stores_selection_and_activates_with_previous_screen()
    {
        _ = Services.GetRequiredService<INavigationHistory>();
        Navigation.NavigateTo("events");
        Navigation.NavigateTo("select-language");
        var vm = Services.GetRequiredService<SelectLanguageViewModel>();

        vm.Select("en");
        await vm.ApplyAsync();

        Assert.Equal("en", Storage.Values[LanguageState.StorageKey]);
        Assert.Equal(["/events"], Activator.Activations);
    }

    [Fact]
    public async Task Apply_without_previous_screen_goes_to_initial_screen()
    {
        Navigation.NavigateTo("select-language");
        var vm = CreateViewModel();

        await vm.ApplyAsync();

        Assert.Equal("pt-BR", Storage.Values[LanguageState.StorageKey]);
        Assert.Equal([AppRoutes.SignIn], Activator.Activations);
    }

    [Fact]
    public void Cancel_discards_selection_and_returns_to_previous_screen()
    {
        _ = Services.GetRequiredService<INavigationHistory>();
        Navigation.NavigateTo("friends");
        Navigation.NavigateTo("select-language");
        var vm = Services.GetRequiredService<SelectLanguageViewModel>();

        vm.Select("en");
        vm.Cancel();

        Assert.Empty(Storage.Values);
        Assert.Empty(Activator.Activations);
        Assert.Equal(AppRoutes.Friends, CurrentPath);
        Assert.Same(SupportedLanguages.PortugueseBrazil, LanguageState.Current);
    }

    [Fact]
    public void Cancel_without_previous_screen_goes_to_sign_in()
    {
        Navigation.NavigateTo("select-language");
        var vm = CreateViewModel();

        vm.Cancel();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public async Task After_first_run_fallback_apply_and_cancel_go_to_sign_in()
    {
        var history = Services.GetRequiredService<INavigationHistory>();
        Navigation.NavigateTo("events");
        LanguageState.SelectionRequired = true;
        history.ResetOnNextNavigation();
        Navigation.NavigateTo("select-language", replace: true);

        var vm = Services.GetRequiredService<SelectLanguageViewModel>();
        Assert.Equal(AppRoutes.SignIn, vm.ReturnUrl);

        vm.Cancel();
        Assert.False(LanguageState.SelectionRequired);
        Assert.Empty(Storage.Values);
        Assert.Equal(AppRoutes.SignIn, CurrentPath);

        await vm.ApplyAsync();
        Assert.Equal([AppRoutes.SignIn], Activator.Activations);
    }

    [Fact]
    public void Unsupported_code_is_ignored()
    {
        var vm = CreateViewModel();

        vm.Select("xx");

        Assert.Equal("pt-BR", vm.SelectedCode);
    }
}

public class SelectLanguagePageTests : AppTestContext
{
    [Fact]
    public void Shows_languages_as_radios_with_flags_and_current_language_checked()
    {
        using var _ = new CultureScope("en");
        LanguageState.Apply(SupportedLanguages.English);

        var cut = Render<SelectLanguagePage>();

        Assert.Equal("Select Language", cut.Find("h1").TextContent);
        var radios = cut.FindAll("input[type=radio][name=language]");
        Assert.Equal(["pt-BR", "en"], radios.Select(r => r.GetAttribute("value")));
        Assert.False(radios[0].HasAttribute("checked"));
        Assert.True(radios[1].HasAttribute("checked"));
        Assert.Equal(["Português do Brasil", "English"], cut.FindAll(".language-option label span").Select(s => s.TextContent));
        Assert.Equal(
            ["_content/Algorist.App.UI/images/flags/br.svg", "_content/Algorist.App.UI/images/flags/gb.svg"],
            cut.FindAll("img.language-flag").Select(i => i.GetAttribute("src")));
        Assert.Equal("Apply", cut.Find("#apply-language").TextContent.Trim());
        Assert.Equal("Cancel", cut.Find("#cancel-language").TextContent.Trim());
    }

    [Fact]
    public void Changing_selection_does_not_change_culture_or_storage()
    {
        using var _ = new CultureScope("pt-BR");

        var cut = Render<SelectLanguagePage>();
        cut.Find("#language-en").Change("en");

        Assert.True(cut.Find("#language-en").HasAttribute("checked"));
        Assert.Equal("pt-BR", CultureInfo.CurrentUICulture.Name);
        Assert.Empty(Storage.Values);
        Assert.Equal("Selecionar idioma", cut.Find("h1").TextContent);
    }

    [Fact]
    public void Apply_button_stores_and_activates_selected_language()
    {
        using var _ = new CultureScope("pt-BR");
        var cut = Render<SelectLanguagePage>();

        cut.Find("#language-en").Change("en");
        cut.Find("#apply-language").Click();

        Assert.Equal("en", Storage.Values[LanguageState.StorageKey]);
        Assert.Single(Activator.Activations);
    }

    [Fact]
    public void Cancel_button_navigates_without_storing()
    {
        using var _ = new CultureScope("pt-BR");
        var cut = Render<SelectLanguagePage>();

        cut.Find("#language-en").Change("en");
        cut.Find("#cancel-language").Click();

        Assert.Empty(Storage.Values);
        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }
}
