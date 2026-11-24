namespace Algorist.Contracts.Validation;

public enum PasswordRule
{
    /// <summary>Between <see cref="PasswordPolicy.MinLength"/> and <see cref="PasswordPolicy.MaxLength"/> characters.</summary>
    Length,
    Uppercase,
    Lowercase,
    Digit,

    /// <summary>A character that is not a letter, digit or white space.</summary>
    Symbol,
}

/// <summary>Strong-password rules shared by the API and the client.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IReadOnlyList<PasswordRule> Rules { get; } =
        [PasswordRule.Length, PasswordRule.Uppercase, PasswordRule.Lowercase, PasswordRule.Digit, PasswordRule.Symbol];

    public static bool IsMet(PasswordRule rule, string? password)
    {
        password ??= string.Empty;
        return rule switch
        {
            PasswordRule.Length => password.Length is >= MinLength and <= MaxLength,
            PasswordRule.Uppercase => password.Any(char.IsUpper),
            PasswordRule.Lowercase => password.Any(char.IsLower),
            PasswordRule.Digit => password.Any(char.IsDigit),
            PasswordRule.Symbol => password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)),
            _ => false,
        };
    }

    /// <summary>The rules the password does not meet (empty when strong).</summary>
    public static IReadOnlyList<PasswordRule> UnmetRules(string? password) =>
        Rules.Where(r => !IsMet(r, password)).ToList();

    public static bool IsStrong(string? password) => UnmetRules(password).Count == 0;
}
