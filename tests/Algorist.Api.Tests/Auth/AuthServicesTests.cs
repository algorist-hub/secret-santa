using System.Net;
using System.Net.Http.Headers;
using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Users;
using Algorist.Api.Domain.Users;
using Algorist.Api.Infra.Auth;
using Algorist.Contracts.Errors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Tests.Auth;

public class UserRepositoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static User NewUser(string email, string? googleSubject = null)
    {
        var user = User.Create(email, "Ana", new DateOnly(1990, 1, 1), null, null, DateTime.UtcNow);
        if (googleSubject is not null)
        {
            user.LinkGoogle(googleSubject);
        }

        return user;
    }

    private async Task<T> WithRepositoryAsync<T>(Func<IUserRepository, IAvatarStore, Task<T>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IUserRepository>(), scope.ServiceProvider.GetRequiredService<IAvatarStore>());
    }

    [Fact]
    public async Task Users_are_found_by_email_and_google_subject()
    {
        var user = NewUser("repo-find@mail.com", "google-find");
        Assert.Equal(AddUserResult.Added, await WithRepositoryAsync((r, _) => r.AddAsync(user, _ct)));

        Assert.Equal(user.Id, (await WithRepositoryAsync((r, _) => r.FindByEmailAsync("repo-find@mail.com", _ct)))!.Id);
        Assert.Equal(user.Id, (await WithRepositoryAsync((r, _) => r.FindByGoogleSubjectAsync("google-find", _ct)))!.Id);
        Assert.Equal(user.Id, (await WithRepositoryAsync((r, _) => r.FindByIdAsync(user.Id, _ct)))!.Id);
        Assert.Null(await WithRepositoryAsync((r, _) => r.FindByEmailAsync("nobody@mail.com", _ct)));
    }

    [Fact]
    public async Task Duplicate_email_and_google_account_are_reported_not_thrown()
    {
        await WithRepositoryAsync((r, _) => r.AddAsync(NewUser("repo-dup@mail.com", "google-dup"), _ct));

        Assert.Equal(AddUserResult.DuplicateEmail, await WithRepositoryAsync((r, _) => r.AddAsync(NewUser("repo-dup@mail.com"), _ct)));
        Assert.Equal(AddUserResult.DuplicateGoogleAccount, await WithRepositoryAsync((r, _) => r.AddAsync(NewUser("other-dup@mail.com", "google-dup"), _ct)));
    }

    [Fact]
    public async Task Avatar_is_saved_with_the_user_and_readable_from_the_store()
    {
        var user = NewUser("repo-avatar@mail.com");
        user.SetAvatar("image/png", [9, 8, 7], DateTime.UtcNow);
        await WithRepositoryAsync((r, _) => r.AddAsync(user, _ct));

        var avatar = await WithRepositoryAsync((_, s) => s.GetAsync(user.Id, _ct));

        Assert.Equal("image/png", avatar!.ContentType);
        Assert.Equal([9, 8, 7], avatar.Content);
    }
}

public class PasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_salted_and_verifies_only_the_right_password()
    {
        var hash = _hasher.Hash("Gift#2026box");

        Assert.NotEqual("Gift#2026box", hash);
        Assert.DoesNotContain("Gift#2026box", hash);
        Assert.NotEqual(hash, _hasher.Hash("Gift#2026box"));
        Assert.True(_hasher.Verify(hash, "Gift#2026box"));
        Assert.False(_hasher.Verify(hash, "gift#2026box"));
    }
}

public class TokenTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly AuthOptions Options = new() { SigningKey = ApiFactory.TestSigningKey, TokenLifetimeDays = 60 };
    private static readonly User Ana = User.Create("ana@mail.com", "Ana", new DateOnly(1990, 1, 1), null, null, DateTime.UtcNow);

    private async Task<HttpResponseMessage> GetMeAsync(string? token)
    {
        using var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.GetAsync(TestEndpoints.MePath, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Issued_token_authorizes_and_identifies_the_user()
    {
        var issued = factory.Services.GetRequiredService<ITokenIssuer>().Issue(Ana);

        var response = await GetMeAsync(issued.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Ana.Id.ToString(), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.InRange(issued.ExpiresAtUtc - DateTimeOffset.UtcNow, TimeSpan.FromDays(59.9), TimeSpan.FromDays(60));
    }

    [Fact]
    public async Task Expired_token_is_rejected_with_unauthorized_code()
    {
        var past = new ManualTimeProvider(DateTimeOffset.UtcNow.AddDays(-61));
        var expired = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(Options), past).Issue(Ana);

        var response = await GetMeAsync(expired.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.Unauthorized, await response.ReadErrorCodeAsync());
    }

    [Theory]
    [InlineData("garbled.token.value")]
    [InlineData(null)]
    public async Task Missing_or_garbled_token_is_rejected_with_unauthorized_code(string? token)
    {
        var response = await GetMeAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ErrorCodes.Unauthorized, await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        var other = new AuthOptions { SigningKey = "another-signing-key-0123456789-abcdefghijkl", TokenLifetimeDays = 60 };
        var forged = new JwtTokenIssuer(Microsoft.Extensions.Options.Options.Create(other), TimeProvider.System).Issue(Ana);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(forged.Token)).StatusCode);
    }

    [Fact]
    public async Task Google_registration_ticket_is_not_accepted_as_an_auth_token()
    {
        var ticket = factory.Services.GetRequiredService<IGoogleRegistrationTicketService>()
            .Create(new GoogleRegistrationTicketData("sub", "ana@mail.com", "Ana", null));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(ticket)).StatusCode);
    }
}

public class GoogleRegistrationTicketTests
{
    private static readonly GoogleRegistrationTicketData Data = new("google-123", "ana@gmail.com", "Ana Lima", "https://lh3.googleusercontent.com/a/pic");
    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));

    private GoogleRegistrationTicketService CreateService(string key = ApiFactory.TestSigningKey) =>
        new(Options.Create(new AuthOptions { SigningKey = key }), _time);

    [Fact]
    public async Task Valid_ticket_round_trips_its_data()
    {
        var service = CreateService();

        Assert.Equal(Data, await service.ReadAsync(service.Create(Data)));
    }

    [Fact]
    public async Task Ticket_expires_after_30_minutes()
    {
        var service = CreateService();
        var ticket = service.Create(Data);

        _time.Advance(TimeSpan.FromMinutes(29));
        Assert.NotNull(await service.ReadAsync(ticket));

        _time.Advance(TimeSpan.FromMinutes(2));
        Assert.Null(await service.ReadAsync(ticket));
    }

    [Fact]
    public async Task Tampered_ticket_is_rejected()
    {
        var service = CreateService();
        var ticket = service.Create(Data);
        var forged = CreateService("attacker-signing-key-0123456789-abcdefghijk").Create(Data with { Email = "ceo@company.com" });
        var parts = ticket.Split('.');
        var swappedPayload = string.Join('.', parts[0], forged.Split('.')[1], parts[2]);

        Assert.Null(await service.ReadAsync(forged));
        Assert.Null(await service.ReadAsync(swappedPayload));
        Assert.Null(await service.ReadAsync("not-a-ticket"));
    }

    [Fact]
    public async Task Auth_token_is_not_accepted_as_a_ticket()
    {
        var options = Options.Create(new AuthOptions { SigningKey = ApiFactory.TestSigningKey, TokenLifetimeDays = 60 });
        var authToken = new JwtTokenIssuer(options, _time).Issue(User.Create("ana@gmail.com", "Ana", new DateOnly(1990, 1, 1), null, null, DateTime.UtcNow));

        Assert.Null(await CreateService().ReadAsync(authToken.Token));
    }
}

public class GoogleIdTokenValidatorTests
{
    private static GoogleIdTokenValidator Create(string? clientId) =>
        new(Options.Create(new AuthOptions { GoogleClientId = clientId }), NullLogger<GoogleIdTokenValidator>.Instance);

    [Fact]
    public async Task Garbage_token_is_rejected()
    {
        Assert.Null(await Create("client.apps.googleusercontent.com").ValidateAsync("not.a.jwt", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Nothing_is_accepted_when_google_is_not_configured()
    {
        Assert.Null(await Create(null).ValidateAsync("anything", TestContext.Current.CancellationToken));
    }
}
