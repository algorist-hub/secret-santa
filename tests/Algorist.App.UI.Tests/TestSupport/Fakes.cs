using System.Buffers.Text;
using System.Globalization;
using System.Net;
using System.Text;
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

internal sealed class FakeTokenStorage : ISecureTokenStorage
{
    public string? Token { get; set; }

    public bool ThrowOnRead { get; set; }

    public Task<string?> GetTokenAsync() =>
        ThrowOnRead ? throw new InvalidOperationException("storage unavailable") : Task.FromResult(Token);

    public Task SetTokenAsync(string token)
    {
        Token = token;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Token = null;
        return Task.CompletedTask;
    }
}

/// <summary>Unsigned JWT-shaped tokens for client tests (the client only decodes the payload).</summary>
internal static class TestTokens
{
    public static string For(string userId, string name = "Ana", string email = "ana@mail.com", string tag = "1")
    {
        static string Encode(string json) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));
        return $"{Encode("{\"alg\":\"HS256\"}")}.{Encode($"{{\"sub\":\"{userId}\",\"name\":\"{name}\",\"email\":\"{email}\"}}")}.sig{tag}";
    }
}

/// <summary>HTTP handler answering with a function of the request; records requests.</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => RespondAsync = r => Task.FromResult(respond(r));

    /// <summary>The response for the next requests (may be replaced during a test).</summary>
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> RespondAsync { get; set; }

    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Request bodies, read when the request was sent.</summary>
    public List<string> Bodies { get; } = [];

    public static StubHttpHandler Status(HttpStatusCode status) => new(_ => new HttpResponseMessage(status));

    public void Respond(Func<HttpRequestMessage, HttpResponseMessage> respond) => RespondAsync = r => Task.FromResult(respond(r));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        return await RespondAsync(request);
    }
}

internal static class ApiResponses
{
    public static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = System.Net.Http.Json.JsonContent.Create(value) };

    public static HttpResponseMessage Problem(HttpStatusCode status, string code) =>
        new(status) { Content = new StringContent($"{{\"status\":{(int)status},\"code\":\"{code}\"}}", Encoding.UTF8, "application/problem+json") };
}

internal sealed class FakeGoogleSignInProvider : Algorist.App.UI.Platform.IGoogleSignInProvider
{
    public bool Available { get; set; } = true;

    public List<(string ClientId, string Language)> Renders { get; } = [];

    /// <summary>The callback of the last rendered button (simulates the person completing Google sign-in).</summary>
    public Func<string, Task>? OnIdToken { get; private set; }

    public Task<bool> RenderButtonAsync(Microsoft.AspNetCore.Components.ElementReference container, string clientId, string languageCode, Func<string, Task> onIdToken)
    {
        Renders.Add((clientId, languageCode));
        OnIdToken = onIdToken;
        return Task.FromResult(Available);
    }
}

/// <summary>A fixed clock in UTC.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>Returns the uploaded bytes unchanged (or null when told to fail).</summary>
internal sealed class FakeImageResizer : Algorist.App.UI.Platform.IImageResizer
{
    public bool Fail { get; set; }

    public List<(int MaxWidth, int MaxHeight)> Calls { get; } = [];

    public async Task<byte[]?> ResizeAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file, int maxWidth, int maxHeight, long maxBytes, CancellationToken cancellationToken = default)
    {
        Calls.Add((maxWidth, maxHeight));
        if (Fail)
        {
            return null;
        }

        await using var stream = file.OpenReadStream(maxBytes * 10, cancellationToken);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
