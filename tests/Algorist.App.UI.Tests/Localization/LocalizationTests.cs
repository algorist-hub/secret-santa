using System.Collections;
using System.Globalization;
using System.Resources;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Resources;
using Algorist.App.UI.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Algorist.App.UI.Tests.Localization;

public class ResourceTests
{
    private static HashSet<string> Keys(CultureInfo culture)
    {
        var manager = new ResourceManager(typeof(AppStrings));
        var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"No resources for '{culture.Name}'.");
        return set.Cast<DictionaryEntry>().Select(e => (string)e.Key).ToHashSet();
    }

    [Fact]
    public void Portuguese_and_English_resources_have_identical_keys()
    {
        var portuguese = Keys(CultureInfo.InvariantCulture);
        var english = Keys(CultureInfo.GetCultureInfo("en"));

        Assert.NotEmpty(portuguese);
        Assert.Empty(portuguese.Except(english));
        Assert.Empty(english.Except(portuguese));
    }

    [Fact]
    public void Every_supported_language_name_and_error_code_has_a_resource()
    {
        var keys = Keys(CultureInfo.InvariantCulture);

        Assert.All(SupportedLanguages.All, l => Assert.Contains(l.NameKey, keys));
        Assert.All(Contracts.Errors.ErrorCodes.All, c => Assert.Contains($"Error_{c}", keys));
    }
}

public class SupportedLanguagesTests
{
    [Fact]
    public void Languages_are_pt_BR_then_en_with_expected_cultures()
    {
        Assert.Collection(
            SupportedLanguages.All,
            pt => Assert.Equal(("pt-BR", "pt-BR", "br.svg"), (pt.Code, pt.CultureName, pt.FlagImage)),
            en => Assert.Equal(("en", "en-GB", "gb.svg"), (en.Code, en.CultureName, en.FlagImage)));
        Assert.Same(SupportedLanguages.PortugueseBrazil, SupportedLanguages.Default);
    }
}

public class LanguageResolverTests
{
    [Theory]
    [InlineData("pt")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    [InlineData("PT_br")]
    public void Portuguese_variants_resolve_to_pt_BR(string tag) =>
        Assert.Same(SupportedLanguages.PortugueseBrazil, LanguageResolver.Resolve(tag));

    [Theory]
    [InlineData("en-US")]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("ja")]
    public void Other_known_languages_resolve_to_en(string tag) =>
        Assert.Same(SupportedLanguages.English, LanguageResolver.Resolve(tag));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xx-invalid!!")]
    [InlineData("!!invalid")]
    [InlineData("qqq")]
    [InlineData("12-34")]
    public void Missing_malformed_or_unknown_tags_are_unresolved(string? tag) =>
        Assert.Null(LanguageResolver.Resolve(tag));
}

public class LanguageStateTests
{
    [Fact]
    public void Applying_en_sets_UI_culture_en_and_formatting_culture_en_GB()
    {
        using var _ = new CultureScope("pt-BR");
        var state = new LanguageState();

        state.Apply(SupportedLanguages.English);

        Assert.Equal("en", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("en-GB", CultureInfo.CurrentCulture.Name);
        Assert.Equal("en-GB", CultureInfo.DefaultThreadCurrentCulture?.Name);
        Assert.Equal("19/11/2026", new DateTime(2026, 11, 19).ToString("d"));
        Assert.Same(SupportedLanguages.English, state.Current);
    }

    [Fact]
    public void Applying_pt_BR_sets_both_cultures_to_pt_BR()
    {
        using var _ = new CultureScope("en", "en-GB");
        var state = new LanguageState();

        state.Apply(SupportedLanguages.PortugueseBrazil);

        Assert.Equal("pt-BR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
        Assert.Equal("pt-BR", CultureInfo.CurrentCulture.Name);
        Assert.Equal("1.234,5", 1234.5.ToString("N1"));
    }
}

public class LanguageBootstrapperTests
{
    private static async Task<(LanguageState State, FakeLocalStorage Storage)> RunAsync(
        FakeLocalStorage storage, FakeLanguageDetector detector)
    {
        var state = new LanguageState();
        await new LanguageBootstrapper(storage, detector, state, NullLogger<LanguageBootstrapper>.Instance).InitializeAsync();
        return (state, storage);
    }

    [Fact]
    public async Task Stored_language_is_applied_without_detection()
    {
        using var _ = new CultureScope("pt-BR");
        var storage = new FakeLocalStorage { Values = { [LanguageState.StorageKey] = "en" } };

        var (state, _) = await RunAsync(storage, new FakeLanguageDetector(fail: true));

        Assert.Same(SupportedLanguages.English, state.Current);
        Assert.False(state.SelectionRequired);
        Assert.Equal(0, storage.Writes);
        Assert.Equal("en", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Theory]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("fr-FR", "en")]
    public async Task Without_stored_language_detected_language_is_applied_and_stored(string detected, string expected)
    {
        using var _ = new CultureScope("pt-BR");
        var storage = new FakeLocalStorage();

        var (state, _) = await RunAsync(storage, new FakeLanguageDetector(detected));

        Assert.Equal(expected, state.Current.Code);
        Assert.Equal(expected, storage.Values[LanguageState.StorageKey]);
        Assert.False(state.SelectionRequired);
    }

    [Fact]
    public async Task Unsupported_stored_value_runs_detection()
    {
        using var _ = new CultureScope("pt-BR");
        var storage = new FakeLocalStorage { Values = { [LanguageState.StorageKey] = "klingon" } };

        var (state, _) = await RunAsync(storage, new FakeLanguageDetector("en-US"));

        Assert.Same(SupportedLanguages.English, state.Current);
        Assert.Equal("en", storage.Values[LanguageState.StorageKey]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("!!invalid", false)]
    [InlineData(null, true)]
    public async Task Failed_detection_uses_pt_BR_in_memory_and_requires_selection(string? detected, bool throws)
    {
        using var _ = new CultureScope("en");
        var storage = new FakeLocalStorage();

        var (state, _) = await RunAsync(storage, new FakeLanguageDetector(detected, throws));

        Assert.Same(SupportedLanguages.PortugueseBrazil, state.Current);
        Assert.True(state.SelectionRequired);
        Assert.Empty(storage.Values);
        Assert.Equal("pt-BR", CultureInfo.DefaultThreadCurrentUICulture?.Name);
    }

    [Fact]
    public async Task Storage_read_error_is_treated_as_not_configured()
    {
        using var _ = new CultureScope("pt-BR");
        var storage = new FakeLocalStorage { ThrowOnRead = true };

        var (state, _) = await RunAsync(storage, new FakeLanguageDetector("de"));

        Assert.Same(SupportedLanguages.English, state.Current);
    }
}
