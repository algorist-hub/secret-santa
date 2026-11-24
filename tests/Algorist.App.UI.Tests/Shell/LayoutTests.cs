using Algorist.App.UI.Layout;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Tests.TestSupport;
using Microsoft.AspNetCore.Components;

namespace Algorist.App.UI.Tests.Shell;

public class LayoutTests : AppTestContext
{
    private static readonly RenderFragment Body = b => b.AddMarkupContent(0, "<p class=\"body\">content</p>");

    [Fact]
    public void Auth_layout_shows_brand_and_body_without_menu()
    {
        using var _ = new CultureScope("en");

        var cut = Render<AuthLayout>(p => p.Add(x => x.Body, Body));

        Assert.Equal("Amigo Presente", cut.Find(".app-brand-title").TextContent);
        Assert.Equal("content", cut.Find(".auth-card .body").TextContent);
        Assert.Empty(cut.FindAll("nav"));
    }

    [Fact]
    public async Task Auth_layout_never_shows_the_menu_even_when_signed_in()
    {
        await SignInAsync();

        var cut = Render<AuthLayout>(p => p.Add(x => x.Body, Body));

        Assert.Empty(cut.FindAll("nav"));
    }

    [Fact]
    public void Main_layout_shows_the_auth_shell_when_signed_out()
    {
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, Body));

        Assert.Empty(cut.FindAll("nav"));
        Assert.Single(cut.FindAll(".auth-card .body"));
        Assert.Single(cut.FindAll(".app-brand"));
    }

    [Fact]
    public async Task Main_layout_switches_to_the_menu_after_sign_in()
    {
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, Body));

        await SignInAsync();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("nav.app-nav")));
        Assert.Empty(cut.FindAll(".auth-card"));
    }

    [Theory]
    [InlineData("select-language")]
    [InlineData("does-not-exist")]
    public void Select_language_and_not_found_have_no_menu_when_signed_out(string route)
    {
        using var _ = new CultureScope("en");
        Navigation.NavigateTo(route);

        var cut = Render<AppRoot>();

        Assert.Empty(cut.FindAll("nav.app-nav"));
        Assert.NotEmpty(cut.FindAll("h1"));
    }

    [Theory]
    [InlineData("select-language")]
    [InlineData("does-not-exist")]
    public async Task Select_language_and_not_found_have_the_menu_when_signed_in(string route)
    {
        using var _ = new CultureScope("en");
        await SignInAsync();
        Navigation.NavigateTo(route);

        var cut = Render<AppRoot>();

        Assert.Single(cut.FindAll("nav.app-nav"));
        Assert.Equal("/" + route, CurrentPath);
    }

    [Fact]
    public void Not_found_links_to_sign_in_when_signed_out()
    {
        using var _ = new CultureScope("en");
        Navigation.NavigateTo("does-not-exist");

        var cut = Render<AppRoot>();

        Assert.Equal(AppRoutes.Relative(AppRoutes.SignIn), cut.Find("a.link1").GetAttribute("href"));
    }
}
