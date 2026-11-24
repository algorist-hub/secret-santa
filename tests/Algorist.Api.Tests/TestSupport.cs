using System.Net.Http.Json;
using System.Text.Json;
using Algorist.Contracts.Errors;

namespace Algorist.Api.Tests;

/// <summary>A clock that tests can set and move.</summary>
public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

internal static class HttpTestExtensions
{
    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty(ApiError.CodeProperty, out var code) ? code.GetString() : null;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url.StartsWith('/') ? url : "/" + url, body, TestContext.Current.CancellationToken);

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;
}
