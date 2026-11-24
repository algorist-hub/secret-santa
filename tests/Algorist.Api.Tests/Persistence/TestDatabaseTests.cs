using Algorist.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Tests.Persistence;

public class TestDatabaseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_user_with_avatar_can_be_inserted_and_read_back()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = User.Create("smoke@mail.com", "Smoke", new DateOnly(1990, 5, 1), "+55 11 98765-4321", null, DateTime.UtcNow);
        user.SetAvatar("image/png", [1, 2, 3], DateTime.UtcNow);

        await factory.WithDbAsync(async db =>
        {
            db.Users.Add(user);
            return await db.SaveChangesAsync(ct);
        });

        var stored = await factory.WithDbAsync(db => db.Users.Include(u => u.Avatar).SingleAsync(u => u.Email == "smoke@mail.com", ct));
        Assert.Equal(user.Id, stored.Id);
        Assert.Equal(new DateOnly(1990, 5, 1), stored.DateOfBirth);
        Assert.Equal([1, 2, 3], stored.Avatar!.Content);
    }
}
