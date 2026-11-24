using Algorist.Api.Domain.Users;
using Algorist.Api.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Algorist.Api.Tests.Persistence;

public class ModelTests
{
    private static MainDbContext CreateSqlServerModelContext() =>
        new(new DbContextOptionsBuilder<MainDbContext>().UseSqlServer(ApiFactory.DummyConnectionString).Options);

    [Fact]
    public void Users_have_unique_email_and_filtered_unique_google_subject_indexes()
    {
        using var db = CreateSqlServerModelContext();
        var user = db.Model.FindEntityType(typeof(User))!;

        var email = Assert.Single(user.GetIndexes(), i => i.Properties.Single().Name == nameof(User.Email));
        Assert.True(email.IsUnique);
        Assert.Null(email.GetFilter());

        var google = Assert.Single(user.GetIndexes(), i => i.Properties.Single().Name == nameof(User.GoogleSubject));
        Assert.True(google.IsUnique);
        Assert.Equal("[GoogleSubject] IS NOT NULL", google.GetFilter());
    }

    [Fact]
    public void Avatar_is_a_separate_table_deleted_with_its_user()
    {
        using var db = CreateSqlServerModelContext();
        var avatar = db.Model.FindEntityType(typeof(UserAvatar))!;

        Assert.Equal("UserAvatars", avatar.GetTableName());
        var fk = Assert.Single(avatar.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
        Assert.Equal(typeof(User), fk.PrincipalEntityType.ClrType);
    }
}
