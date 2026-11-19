using Algorist.App.UI.Layout;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Pages;
using Algorist.App.UI.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.Shell;

public class NavigationHistoryTests : AppTestContext
{
    [Fact]
    public void Previous_tracks_the_last_in_app_location()
    {
        Navigation.NavigateTo("home");
        var history = Services.GetRequiredService<INavigationHistory>();
        Assert.Null(history.Previous);

        Navigation.NavigateTo("events");
        Assert.Equal("/home", history.Previous);

        Navigation.NavigateTo("select-language");
        Assert.Equal("/events", history.Previous);
    }

    [Fact]
    public void Reset_clears_history_on_next_navigation()
    {
        Navigation.NavigateTo("events");
        var history = Services.GetRequiredService<INavigationHistory>();
        Navigation.NavigateTo("friends");
        Assert.Equal("/events", history.Previous);

        history.ResetOnNextNavigation();
        Navigation.NavigateTo("select-language");

        Assert.Null(history.Previous);
    }
}

public class MainLayoutTests : AppTestContext
{
    private static readonly RenderFragment Body = b => b.AddContent(0, "content");

    [Fact]
    public void Menu_has_four_items_in_order_with_routes_and_no_select_language()
    {
        using var _ = new CultureScope("en");

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, Body));

        var links = cut.FindAll("nav.app-nav a");
        Assert.Equal(["Home", "Events", "Friends", "Profile"], links.Select(a => a.TextContent.Trim()));
        Assert.Equal(["home", "events", "friends", "profile"], links.Select(a => a.GetAttribute("href")));
        Assert.All(links, a => Assert.NotNull(a.QuerySelector("i.bi")));
        Assert.Empty(cut.FindAll("a[href*='select-language']"));
        Assert.Contains("app-nav-item-bottom", links[3].ClassName);
    }

    [Fact]
    public void Brand_and_menu_are_translated()
    {
        using var _ = new CultureScope("pt-BR");

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, Body));

        Assert.Equal("Amigo Presente", cut.Find(".app-brand-title").TextContent);
        Assert.Equal("🎁", cut.Find(".app-brandmark").TextContent);
        Assert.Equal(["Início", "Eventos", "Amigos", "Perfil"], cut.FindAll("nav.app-nav a").Select(a => a.TextContent.Trim()));
    }

    [Fact]
    public void Current_menu_item_is_active()
    {
        using var _ = new CultureScope("en");
        Navigation.NavigateTo("friends");

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, Body));

        var active = Assert.Single(cut.FindAll("nav.app-nav a.active"));
        Assert.Equal("Friends", active.TextContent.Trim());
    }
}

public class PageTests : AppTestContext
{
    public static TheoryData<Type, string, string> Pages => new()
    {
        { typeof(SignIn), "Entrar", "Sign In" },
        { typeof(Home), "Início", "Home" },
        { typeof(Events), "Eventos", "Events" },
        { typeof(Friends), "Amigos", "Friends" },
        { typeof(Profile), "Perfil", "Profile" },
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public void Placeholder_pages_show_only_their_translated_title(Type page, string portuguese, string english)
    {
        using (new CultureScope("pt-BR"))
        {
            var cut = Render(b => { b.OpenComponent(0, page); b.CloseComponent(); });
            Assert.Equal(portuguese, cut.Find("h1").TextContent);
            Assert.Single(cut.FindAll("h1"));
            Assert.Empty(cut.FindAll("p, form, button"));
        }

        using (new CultureScope("en"))
        {
            var cut = Render(b => { b.OpenComponent(0, page); b.CloseComponent(); });
            Assert.Equal(english, cut.Find("h1").TextContent);
        }
    }

    [Fact]
    public void Root_redirects_to_sign_in()
    {
        Render<Pages.Index>();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public void Not_found_shows_translated_message_and_link_to_sign_in()
    {
        using var _ = new CultureScope("en");

        var cut = Render<NotFound>();

        Assert.Equal("Page not found", cut.Find("h1").TextContent);
        Assert.Equal("The page you are looking for does not exist.", cut.Find(".not-found-message").TextContent);
        var link = cut.Find("a.link1");
        Assert.Equal("sign-in", link.GetAttribute("href"));
        Assert.Equal("Go to Sign In", link.TextContent);
    }

    [Fact]
    public void App_redirects_to_select_language_when_selection_is_required()
    {
        using var _ = new CultureScope("pt-BR");
        LanguageState.SelectionRequired = true;
        Navigation.NavigateTo("events");

        Render<AppRoot>();

        Assert.Equal(AppRoutes.SelectLanguage, CurrentPath);
        Assert.Null(Services.GetRequiredService<INavigationHistory>().Previous);
    }

    [Fact]
    public void App_does_not_redirect_when_language_is_configured()
    {
        using var _ = new CultureScope("pt-BR");
        Navigation.NavigateTo("events");

        Render<AppRoot>();

        Assert.Equal(AppRoutes.Events, CurrentPath);
    }
}
