using System.Text.RegularExpressions;

namespace Algorist.Contracts.Validation;

/// <summary>Language-independent field error codes; the client translates them as <c>Validation_&lt;Code&gt;</c>.</summary>
public static class FieldErrors
{
    public const string Required = "Required";
    public const string InvalidEmail = "InvalidEmail";
    public const string TooLong = "TooLong";
    public const string InvalidPhone = "InvalidPhone";
    public const string InvalidDate = "InvalidDate";
}

/// <summary>A field error with an optional limit (e.g. the maximum length) for the message.</summary>
public sealed record FieldError(string Code, int? Limit = null);

/// <summary>Registration field rules shared by the API and the client.</summary>
public static partial class RegistrationRules
{
    public const int EmailMaxLength = 254;
    public const int NameMaxLength = 100;
    public const int PhoneMaxLength = 30;
    public const int PhoneMinDigits = 8;
    public const int PhoneMaxDigits = 15;
    public const int AddressMaxLength = 300;

    public static DateOnly EarliestDateOfBirth { get; } = new(1900, 1, 1);

    /// <summary>Lowercase and trimmed; the form in which emails are stored and compared.</summary>
    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Trimmed text, or null when empty (for optional fields).</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static FieldError? ValidateEmail(string? email)
    {
        var value = (email ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return new FieldError(FieldErrors.Required);
        }

        if (value.Length > EmailMaxLength)
        {
            return new FieldError(FieldErrors.TooLong, EmailMaxLength);
        }

        return EmailPattern().IsMatch(value) ? null : new FieldError(FieldErrors.InvalidEmail);
    }

    public static FieldError? ValidateName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return new FieldError(FieldErrors.Required);
        }

        return value.Length > NameMaxLength ? new FieldError(FieldErrors.TooLong, NameMaxLength) : null;
    }

    /// <summary>Optional: digits, spaces, '+', '(', ')' and '-' only, with 8 to 15 digits.</summary>
    public static FieldError? ValidatePhone(string? phone)
    {
        var value = NormalizeOptional(phone);
        if (value is null)
        {
            return null;
        }

        if (value.Length > PhoneMaxLength || !PhonePattern().IsMatch(value))
        {
            return new FieldError(FieldErrors.InvalidPhone);
        }

        var digits = value.Count(char.IsAsciiDigit);
        return digits is >= PhoneMinDigits and <= PhoneMaxDigits ? null : new FieldError(FieldErrors.InvalidPhone);
    }

    public static FieldError? ValidateAddress(string? address)
    {
        var value = NormalizeOptional(address);
        return value is { Length: > AddressMaxLength } ? new FieldError(FieldErrors.TooLong, AddressMaxLength) : null;
    }

    /// <summary>Not in the future and not before <see cref="EarliestDateOfBirth"/>.</summary>
    public static FieldError? ValidateDateOfBirth(DateOnly? dateOfBirth, DateOnly today)
    {
        if (dateOfBirth is not { } value)
        {
            return new FieldError(FieldErrors.Required);
        }

        return value > today || value < EarliestDateOfBirth ? new FieldError(FieldErrors.InvalidDate) : null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[0-9 +()\-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
