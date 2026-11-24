using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Algorist.Api.Application.Auth;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Configuration;
using Algorist.Contracts.Errors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Tests.Auth;

/// <summary>Builders shared by the endpoint tests.</summary>
internal static class AuthRequests
{
    public const string StrongPassword = "Gift#2026box";

    // Smallest valid file headers for each allowed type.
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];
    public static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();

    public static SignUpRequest EmailSignUp(string email, DateOnly? dateOfBirth = null) => new()
    {
        Email = email,
        Name = "Ana Lima",
        DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 10),
        Password = StrongPassword,
        PhoneNumber = " +55 (11) 98765-4321 ",
        Address = "Rua das Flores, 10",
    };

    public static GoogleIdentity GoogleUser(string subject, string email, bool verified = true) =>
        new(subject, email, verified, "Ana Google", "https://lh3.googleusercontent.com/a/ana");

    public static async Task<AuthTokenResponse> SignUpAsync(this HttpClient client, SignUpRequest request)
    {
        var response = await client.PostJsonAsync(ApiRoutes.SignUp, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<AuthTokenResponse>();
    }

    public static async Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoints.MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await response.ReadErrorCodeAsync());
    }
}

public class PublicSettingsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Defaults_report_minimum_age_18_and_no_google_client()
    {
        var settings = await (await factory.CreateClient().GetAsync("/" + ApiRoutes.PublicSettings, TestContext.Current.CancellationToken))
            .ReadJsonAsync<PublicSettingsResponse>();

        Assert.Equal(new PublicSettingsResponse(18, null), settings);
    }

    [Fact]
    public async Task Configured_values_are_reported_without_signing_in()
    {
        using var configured = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting(ConfigurationKeys.GoogleClientId, "client-123.apps.googleusercontent.com");
            b.UseSetting(ConfigurationKeys.RegistrationMinimumAgeYears, "21");
        });

        var settings = await (await configured.CreateClient().GetAsync("/" + ApiRoutes.PublicSettings, TestContext.Current.CancellationToken))
            .ReadJsonAsync<PublicSettingsResponse>();

        Assert.Equal(new PublicSettingsResponse(21, "client-123.apps.googleusercontent.com"), settings);
    }
}

public class PasswordSignInEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Correct_credentials_with_any_email_case_return_a_working_token()
    {
        var client = factory.CreateClient();
        await client.SignUpAsync(AuthRequests.EmailSignUp("signin-ok@mail.com"));

        var response = await client.PostJsonAsync(ApiRoutes.SignIn, new PasswordSignInRequest(" SignIn-OK@Mail.com ", AuthRequests.StrongPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.ReadJsonAsync<AuthTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, (await client.GetMeAsync(token.Token)).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_unknown_email_and_google_only_account_fail_identically()
    {
        var client = factory.CreateClient();
        await client.SignUpAsync(AuthRequests.EmailSignUp("signin-wrong@mail.com"));
        factory.Google.Add("google-only-token", AuthRequests.GoogleUser("google-only-sub", "google-only@gmail.com"));
        var google = await (await client.PostJsonAsync(ApiRoutes.GoogleSignIn, new GoogleSignInRequest("google-only-token")))
            .ReadJsonAsync<GoogleSignInResponse>();
        await client.SignUpAsync(new SignUpRequest { Name = "G", DateOfBirth = new DateOnly(1990, 1, 1), GoogleRegistrationTicket = google.RegistrationTicket });

        var bodies = new List<string>();
        foreach (var attempt in new[]
        {
            new PasswordSignInRequest("signin-wrong@mail.com", "Wrong#2026box"),
            new PasswordSignInRequest("nobody@mail.com", AuthRequests.StrongPassword),
            new PasswordSignInRequest("google-only@gmail.com", AuthRequests.StrongPassword),
        })
        {
            var response = await client.PostJsonAsync(ApiRoutes.SignIn, attempt);
            await response.AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!.AsObject();
            json.Remove("traceId");
            bodies.Add(json.ToJsonString());
        }

        Assert.Single(bodies.Distinct());
    }
}

public class SignUpEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private DateOnly Today => DateOnly.FromDateTime(factory.Time.GetUtcNow().UtcDateTime);

    [Fact]
    public async Task Valid_email_sign_up_creates_the_account_with_a_hashed_password()
    {
        var client = factory.CreateClient();

        var token = await client.SignUpAsync(AuthRequests.EmailSignUp(" New.User@Mail.com "));

        Assert.Equal(HttpStatusCode.OK, (await client.GetMeAsync(token.Token)).StatusCode);
        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == "new.user@mail.com", TestContext.Current.CancellationToken));
        Assert.Equal("Ana Lima", user.Name);
        Assert.Equal("+55 (11) 98765-4321", user.PhoneNumber);
        Assert.NotNull(user.PasswordHash);
        Assert.NotEqual(AuthRequests.StrongPassword, user.PasswordHash);
        Assert.DoesNotContain(AuthRequests.StrongPassword, user.PasswordHash);
        Assert.Null(user.GoogleSubject);
    }

    [Fact]
    public async Task Invalid_fields_are_rejected_with_validation_failed()
    {
        var client = factory.CreateClient();

        foreach (var request in new[]
        {
            AuthRequests.EmailSignUp("long-name@mail.com") with { Name = new string('a', 101) },
            AuthRequests.EmailSignUp("ana@"),
            AuthRequests.EmailSignUp("phone@mail.com") with { PhoneNumber = "12ab" },
            AuthRequests.EmailSignUp("future@mail.com", Today.AddDays(1)),
        })
        {
            await (await client.PostJsonAsync(ApiRoutes.SignUp, request)).AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
        }

        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == "long-name@mail.com", TestContext.Current.CancellationToken)));
    }

    [Theory]
    [InlineData("password1")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Weak_or_missing_password_is_rejected(string? password)
    {
        var response = await factory.CreateClient().PostJsonAsync(ApiRoutes.SignUp, AuthRequests.EmailSignUp("weak@mail.com") with { Password = password });

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.WeakPassword);
    }

    [Fact]
    public async Task Seventeen_year_old_is_rejected_as_underage()
    {
        var response = await factory.CreateClient().PostJsonAsync(ApiRoutes.SignUp, AuthRequests.EmailSignUp("young@mail.com", Today.AddYears(-17)));

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.Underage);
    }

    [Fact]
    public async Task Exactly_eighteen_today_is_accepted()
    {
        await factory.CreateClient().SignUpAsync(AuthRequests.EmailSignUp("eighteen@mail.com", Today.AddYears(-18)));
    }

    [Fact]
    public async Task Configured_minimum_age_of_21_rejects_a_20_year_old()
    {
        using var configured = factory.WithWebHostBuilder(b => b.UseSetting(ConfigurationKeys.RegistrationMinimumAgeYears, "21"));

        var response = await configured.CreateClient().PostJsonAsync(ApiRoutes.SignUp, AuthRequests.EmailSignUp("twenty@mail.com", Today.AddYears(-20)));

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.Underage);
    }

    [Fact]
    public async Task Email_in_use_with_different_case_is_rejected()
    {
        var client = factory.CreateClient();
        await client.SignUpAsync(AuthRequests.EmailSignUp("ana.dup@mail.com"));

        var response = await client.PostJsonAsync(ApiRoutes.SignUp, AuthRequests.EmailSignUp("Ana.Dup@Mail.com"));

        await response.AssertProblemAsync(HttpStatusCode.Conflict, ErrorCodes.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task Valid_png_avatar_is_stored()
    {
        var request = AuthRequests.EmailSignUp("avatar-ok@mail.com") with { Avatar = new AvatarUpload("image/png", Convert.ToBase64String(AuthRequests.Png)) };

        await factory.CreateClient().SignUpAsync(request);

        var avatar = await factory.WithDbAsync(db => db.UserAvatars.SingleAsync(
            a => db.Users.Any(u => u.Id == a.UserId && u.Email == "avatar-ok@mail.com"), TestContext.Current.CancellationToken));
        Assert.Equal("image/png", avatar.ContentType);
        Assert.Equal(AuthRequests.Png, avatar.Content);
    }

    [Fact]
    public async Task Pdf_oversized_or_mislabelled_avatars_are_rejected()
    {
        var oversized = new byte[(1024 * 1024) + 100_000];
        AuthRequests.Png.CopyTo(oversized, 0);
        var client = factory.CreateClient();

        foreach (var avatar in new[]
        {
            new AvatarUpload("application/pdf", Convert.ToBase64String(AuthRequests.Pdf)),
            new AvatarUpload("image/png", Convert.ToBase64String(AuthRequests.Pdf)),
            new AvatarUpload("image/png", Convert.ToBase64String(oversized)),
            new AvatarUpload("image/jpeg", "not base64!"),
        })
        {
            var response = await client.PostJsonAsync(ApiRoutes.SignUp, AuthRequests.EmailSignUp("avatar-bad@mail.com") with { Avatar = avatar });
            await response.AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.InvalidAvatar);
        }
    }
}

public class GoogleEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private async Task<GoogleSignInResponse> GoogleSignInAsync(HttpClient client, string idToken)
    {
        var response = await client.PostJsonAsync(ApiRoutes.GoogleSignIn, new GoogleSignInRequest(idToken));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<GoogleSignInResponse>();
    }

    [Fact]
    public async Task New_google_user_gets_a_registration_ticket_and_no_account()
    {
        factory.Google.Add("new-token", AuthRequests.GoogleUser("new-sub", "New.Google@gmail.com"));

        var result = await GoogleSignInAsync(factory.CreateClient(), "new-token");

        Assert.Equal(GoogleSignInStatus.RegistrationRequired, result.Status);
        Assert.Null(result.Token);
        Assert.False(string.IsNullOrEmpty(result.RegistrationTicket));
        Assert.Equal(("new.google@gmail.com", "Ana Google", "https://lh3.googleusercontent.com/a/ana"), (result.Email, result.Name, result.PictureUrl));
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == "new.google@gmail.com", _ct)));
    }

    [Fact]
    public async Task Google_registration_creates_a_linked_account_without_password_and_signs_in()
    {
        var client = factory.CreateClient();
        factory.Google.Add("reg-token", AuthRequests.GoogleUser("reg-sub", "reg@gmail.com"));
        var pending = await GoogleSignInAsync(client, "reg-token");

        var token = await client.SignUpAsync(new SignUpRequest
        {
            Email = "ignored@evil.com",
            Name = "Ana Edited",
            DateOfBirth = new DateOnly(1995, 3, 3),
            GoogleRegistrationTicket = pending.RegistrationTicket,
            UseGoogleAvatar = true,
        });

        Assert.Equal(HttpStatusCode.OK, (await client.GetMeAsync(token.Token)).StatusCode);
        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.GoogleSubject == "reg-sub", _ct));
        Assert.Equal(("reg@gmail.com", "Ana Edited"), (user.Email, user.Name));
        Assert.Null(user.PasswordHash);
        Assert.Equal("https://lh3.googleusercontent.com/a/ana", user.GoogleAvatarUrl);
        Assert.False(await factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == "ignored@evil.com", _ct)));

        var again = await GoogleSignInAsync(client, "reg-token");
        Assert.Equal(GoogleSignInStatus.SignedIn, again.Status);
        Assert.False(string.IsNullOrEmpty(again.Token));
    }

    [Fact]
    public async Task Google_registration_without_google_avatar_stores_none()
    {
        var client = factory.CreateClient();
        factory.Google.Add("noavatar-token", AuthRequests.GoogleUser("noavatar-sub", "noavatar@gmail.com"));
        var pending = await GoogleSignInAsync(client, "noavatar-token");

        await client.SignUpAsync(new SignUpRequest { Name = "Ana", DateOfBirth = new DateOnly(1995, 3, 3), GoogleRegistrationTicket = pending.RegistrationTicket });

        Assert.Null((await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.GoogleSubject == "noavatar-sub", _ct))).GoogleAvatarUrl);
    }

    [Fact]
    public async Task Email_account_is_linked_on_google_sign_in_and_keeps_its_password()
    {
        var client = factory.CreateClient();
        await client.SignUpAsync(AuthRequests.EmailSignUp("link@gmail.com"));
        factory.Google.Add("link-token", AuthRequests.GoogleUser("link-sub", "Link@gmail.com"));

        var result = await GoogleSignInAsync(client, "link-token");

        Assert.Equal(GoogleSignInStatus.SignedIn, result.Status);
        Assert.Equal("link-sub", (await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == "link@gmail.com", _ct))).GoogleSubject);
        Assert.Equal(HttpStatusCode.OK, (await client.PostJsonAsync(ApiRoutes.SignIn, new PasswordSignInRequest("link@gmail.com", AuthRequests.StrongPassword))).StatusCode);
    }

    [Fact]
    public async Task Invalid_token_or_unverified_email_fails()
    {
        var client = factory.CreateClient();
        factory.Google.Add("unverified-token", AuthRequests.GoogleUser("unverified-sub", "unverified@gmail.com", verified: false));

        await (await client.PostJsonAsync(ApiRoutes.GoogleSignIn, new GoogleSignInRequest("unknown-token")))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed);
        await (await client.PostJsonAsync(ApiRoutes.GoogleSignIn, new GoogleSignInRequest("unverified-token")))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed);
    }

    [Fact]
    public async Task Tampered_or_expired_ticket_is_rejected()
    {
        var client = factory.CreateClient();
        factory.Google.Add("ticket-token", AuthRequests.GoogleUser("ticket-sub", "ticket@gmail.com"));
        var pending = await GoogleSignInAsync(client, "ticket-token");
        var request = new SignUpRequest { Name = "Ana", DateOfBirth = new DateOnly(1995, 3, 3) };

        await (await client.PostJsonAsync(ApiRoutes.SignUp, request with { GoogleRegistrationTicket = pending.RegistrationTicket + "x" }))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed);

        factory.Time.Advance(TimeSpan.FromMinutes(31));
        try
        {
            await (await client.PostJsonAsync(ApiRoutes.SignUp, request with { GoogleRegistrationTicket = pending.RegistrationTicket }))
                .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed);
        }
        finally
        {
            factory.Time.Advance(TimeSpan.FromMinutes(-31));
        }
    }

    [Fact]
    public async Task Password_is_not_allowed_with_a_google_ticket()
    {
        var client = factory.CreateClient();
        factory.Google.Add("pwd-token", AuthRequests.GoogleUser("pwd-sub", "pwd@gmail.com"));
        var pending = await GoogleSignInAsync(client, "pwd-token");

        var response = await client.PostJsonAsync(ApiRoutes.SignUp, new SignUpRequest
        {
            Name = "Ana",
            DateOfBirth = new DateOnly(1995, 3, 3),
            Password = AuthRequests.StrongPassword,
            GoogleRegistrationTicket = pending.RegistrationTicket,
        });

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed);
    }
}

public class RenewEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Renewed_token_expires_later_and_works()
    {
        var client = factory.CreateClient();
        var first = await client.SignUpAsync(AuthRequests.EmailSignUp("renew@mail.com"));
        factory.Time.Advance(TimeSpan.FromDays(1));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/" + ApiRoutes.Renew);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.Token);
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var renewed = await response.ReadJsonAsync<AuthTokenResponse>();
            Assert.Equal(first.ExpiresAtUtc.AddDays(1), renewed.ExpiresAtUtc);
            Assert.Equal(HttpStatusCode.OK, (await client.GetMeAsync(renewed.Token)).StatusCode);
        }
        finally
        {
            factory.Time.Advance(TimeSpan.FromDays(-1));
        }
    }

    [Fact]
    public async Task Missing_token_or_deleted_user_is_unauthorized()
    {
        var client = factory.CreateClient();
        await (await client.PostAsync("/" + ApiRoutes.Renew, null, TestContext.Current.CancellationToken))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized);

        var token = await client.SignUpAsync(AuthRequests.EmailSignUp("deleted@mail.com"));
        await factory.WithDbAsync(db => db.Users.Where(u => u.Email == "deleted@mail.com").ExecuteDeleteAsync(TestContext.Current.CancellationToken));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/" + ApiRoutes.Renew);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        await (await client.SendAsync(request, TestContext.Current.CancellationToken))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized);
    }
}

public class RateLimitTests
{
    [Fact]
    public async Task Eleventh_sign_in_attempt_within_a_minute_is_rejected()
    {
        await using var factory = new ApiFactory();
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("Auth:AttemptsPerMinute", "10"));
        var client = limited.CreateClient();
        var attempt = new PasswordSignInRequest("nobody@mail.com", AuthRequests.StrongPassword);

        for (var i = 0; i < 10; i++)
        {
            await (await client.PostJsonAsync(ApiRoutes.SignIn, attempt)).AssertProblemAsync(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        }

        await (await client.PostJsonAsync(ApiRoutes.SignIn, attempt)).AssertProblemAsync(HttpStatusCode.TooManyRequests, ErrorCodes.TooManyAttempts);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/" + ApiRoutes.PublicSettings, TestContext.Current.CancellationToken)).StatusCode);
    }
}
