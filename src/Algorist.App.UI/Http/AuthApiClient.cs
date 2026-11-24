using System.Net.Http.Json;
using System.Text.Json;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;

namespace Algorist.App.UI.Http;

/// <summary>Error codes produced by the client itself (translated like API codes, as <c>Error_&lt;CODE&gt;</c>).</summary>
public static class ClientErrorCodes
{
    /// <summary>The server could not be reached.</summary>
    public const string Network = "NETWORK";
}

/// <summary>A response value, or the error code the API (or the client) reported.</summary>
public sealed record ApiResult<T>(T? Value, string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;

    public static ApiResult<T> Ok(T value) => new(value, null);

    public static ApiResult<T> Fail(string code) => new(default, code);
}

/// <summary>Calls the authentication and registration API.</summary>
public sealed class AuthApiClient(IHttpClientFactory httpClientFactory)
{
    public Task<ApiResult<AuthTokenResponse>> SignInAsync(PasswordSignInRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PasswordSignInRequest, AuthTokenResponse>(ApiRoutes.SignIn, request, cancellationToken);

    public Task<ApiResult<GoogleSignInResponse>> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<GoogleSignInRequest, GoogleSignInResponse>(ApiRoutes.GoogleSignIn, request, cancellationToken);

    public Task<ApiResult<AuthTokenResponse>> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SignUpRequest, AuthTokenResponse>(ApiRoutes.SignUp, request, cancellationToken);

    public Task<ApiResult<PublicSettingsResponse>> GetPublicSettingsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<PublicSettingsResponse>(client => client.GetAsync(ApiRoutes.PublicSettings, cancellationToken), cancellationToken);

    private Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string route, TRequest body, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(client => client.PostAsJsonAsync(route, body, cancellationToken), cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpClient, Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send(httpClientFactory.CreateClient(ApiClient.Name));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken) is { } value
                    ? ApiResult<T>.Ok(value)
                    : ApiResult<T>.Fail(ErrorCodes.UnexpectedError);
            }

            return ApiResult<T>.Fail(await ReadErrorCodeAsync(response, cancellationToken) ?? ErrorCodes.UnexpectedError);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Fail(ClientErrorCodes.Network);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail(ErrorCodes.UnexpectedError);
        }
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return problem.RootElement.ValueKind == JsonValueKind.Object
                   && problem.RootElement.TryGetProperty(ApiError.CodeProperty, out var code)
                   && code.ValueKind == JsonValueKind.String
                ? code.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
