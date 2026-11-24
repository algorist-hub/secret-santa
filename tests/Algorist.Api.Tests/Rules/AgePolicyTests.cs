using Algorist.Api.Domain.Users;
using Algorist.Contracts.Validation;

namespace Algorist.Api.Tests.Rules;

public class AgePolicyTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);

    [Fact]
    public void Birthday_exactly_n_years_ago_is_old_enough()
    {
        Assert.True(AgePolicy.IsOldEnough(new DateOnly(2008, 11, 24), Today, 18));
    }

    [Fact]
    public void One_day_short_of_n_years_is_not_old_enough()
    {
        Assert.False(AgePolicy.IsOldEnough(new DateOnly(2008, 11, 25), Today, 18));
    }

    [Fact]
    public void Seventeen_year_old_is_not_an_adult()
    {
        Assert.False(AgePolicy.IsOldEnough(new DateOnly(2009, 6, 1), Today, 18));
    }

    [Fact]
    public void Leap_day_birth_reaches_age_on_1_march_of_non_leap_years()
    {
        var birth = new DateOnly(2008, 2, 29);

        Assert.False(AgePolicy.IsOldEnough(birth, new DateOnly(2026, 2, 28), 18));
        Assert.True(AgePolicy.IsOldEnough(birth, new DateOnly(2026, 3, 1), 18));
    }

    [Fact]
    public void Configured_minimum_of_21_refuses_a_20_year_old()
    {
        var twentyYearOld = new DateOnly(2006, 1, 1);

        Assert.True(AgePolicy.IsOldEnough(twentyYearOld, Today, 18));
        Assert.False(AgePolicy.IsOldEnough(twentyYearOld, Today, 21));
        Assert.Equal(new DateOnly(2005, 11, 24), AgePolicy.LatestAllowedDateOfBirth(Today, 21));
    }
}

public class UserTests
{
    [Fact]
    public void Created_user_has_an_id_and_no_credentials_yet()
    {
        var user = User.Create("ana@mail.com", "Ana", new DateOnly(2000, 1, 1), null, null, DateTime.UtcNow);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Null(user.PasswordHash);
        Assert.Null(user.GoogleSubject);
        Assert.Null(user.Avatar);
    }

    [Fact]
    public void Avatar_belongs_to_the_user()
    {
        var user = User.Create("ana@mail.com", "Ana", new DateOnly(2000, 1, 1), null, null, DateTime.UtcNow);

        user.SetAvatar("image/png", [1, 2, 3], DateTime.UtcNow);

        Assert.Equal(user.Id, user.Avatar!.UserId);
    }
}
