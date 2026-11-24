namespace Algorist.Api.Application.Registration;

/// <summary>Registration settings, bound from the <c>Registration</c> section.</summary>
public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    /// <summary>Minimum age, in whole years on the current date, required to register.</summary>
    public int MinimumAgeYears { get; set; } = 18;
}
