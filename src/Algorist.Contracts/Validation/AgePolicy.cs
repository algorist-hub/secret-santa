namespace Algorist.Contracts.Validation;

/// <summary>Minimum-age rule shared by the API and the client. The minimum comes from server configuration.</summary>
public static class AgePolicy
{
    /// <summary>
    /// True when the person's age in whole years on <paramref name="today"/> is at least <paramref name="minimumYears"/>.
    /// A 29 February birthday counts as reached on 1 March in non-leap years.
    /// </summary>
    public static bool IsOldEnough(DateOnly dateOfBirth, DateOnly today, int minimumYears) =>
        dateOfBirth <= today.AddYears(-minimumYears);

    /// <summary>The latest date of birth that still satisfies the minimum age on <paramref name="today"/>.</summary>
    public static DateOnly LatestAllowedDateOfBirth(DateOnly today, int minimumYears) => today.AddYears(-minimumYears);
}
