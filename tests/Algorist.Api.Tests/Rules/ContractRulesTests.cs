using System.Text.Json;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Validation;

namespace Algorist.Api.Tests.Rules;

public class PasswordPolicyTests
{
    [Fact]
    public void Weak_password_reports_missing_uppercase_and_symbol()
    {
        Assert.Equal([PasswordRule.Uppercase, PasswordRule.Symbol], PasswordPolicy.UnmetRules("password1"));
    }

    [Fact]
    public void Strong_password_meets_every_rule()
    {
        Assert.True(PasswordPolicy.IsStrong("Gift#2026box"));
        Assert.Empty(PasswordPolicy.UnmetRules("Gift#2026box"));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(129)]
    public void Length_outside_8_to_128_fails(int length)
    {
        var password = "Aa1#" + new string('x', length - 4);

        Assert.Equal([PasswordRule.Length], PasswordPolicy.UnmetRules(password));
    }

    [Fact]
    public void Empty_or_null_password_fails_every_rule()
    {
        Assert.Equal(PasswordPolicy.Rules, PasswordPolicy.UnmetRules(null));
    }
}

public class RegistrationRulesTests
{
    [Theory]
    [InlineData("ana@", FieldErrors.InvalidEmail)]
    [InlineData("ana mail.com", FieldErrors.InvalidEmail)]
    [InlineData("  ", FieldErrors.Required)]
    [InlineData(null, FieldErrors.Required)]
    public void Invalid_emails_are_rejected(string? email, string expected)
    {
        Assert.Equal(expected, RegistrationRules.ValidateEmail(email)?.Code);
    }

    [Fact]
    public void Too_long_email_is_rejected_with_its_limit()
    {
        var email = new string('a', 250) + "@x.com";

        Assert.Equal(new FieldError(FieldErrors.TooLong, 254), RegistrationRules.ValidateEmail(email));
    }

    [Fact]
    public void Email_is_normalized_to_lowercase_and_trimmed()
    {
        Assert.Equal("ana@mail.com", RegistrationRules.NormalizeEmail(" Ana@Mail.com "));
        Assert.Null(RegistrationRules.ValidateEmail(" Ana@Mail.com "));
    }

    [Theory]
    [InlineData("", FieldErrors.Required)]
    [InlineData("   ", FieldErrors.Required)]
    public void Name_is_required(string name, string expected)
    {
        Assert.Equal(expected, RegistrationRules.ValidateName(name)?.Code);
    }

    [Fact]
    public void Name_longer_than_100_characters_is_rejected()
    {
        Assert.Null(RegistrationRules.ValidateName(new string('a', 100)));
        Assert.Equal(new FieldError(FieldErrors.TooLong, 100), RegistrationRules.ValidateName(new string('a', 101)));
    }

    [Theory]
    [InlineData("12ab")]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    [InlineData("+55 11 98765.4321")]
    public void Invalid_phones_are_rejected(string phone)
    {
        Assert.Equal(FieldErrors.InvalidPhone, RegistrationRules.ValidatePhone(phone)?.Code);
    }

    [Theory]
    [InlineData("+55 (11) 98765-4321")]
    [InlineData("12345678")]
    [InlineData(null)]
    [InlineData("  ")]
    public void Valid_or_empty_phones_are_accepted(string? phone)
    {
        Assert.Null(RegistrationRules.ValidatePhone(phone));
    }

    [Fact]
    public void Address_is_optional_up_to_300_characters()
    {
        Assert.Null(RegistrationRules.ValidateAddress(null));
        Assert.Null(RegistrationRules.ValidateAddress(new string('a', 300)));
        Assert.Equal(new FieldError(FieldErrors.TooLong, 300), RegistrationRules.ValidateAddress(new string('a', 301)));
    }

    [Fact]
    public void Date_of_birth_must_be_between_1900_and_today()
    {
        var today = new DateOnly(2026, 11, 24);

        Assert.Null(RegistrationRules.ValidateDateOfBirth(today, today));
        Assert.Null(RegistrationRules.ValidateDateOfBirth(new DateOnly(1900, 1, 1), today));
        Assert.Equal(FieldErrors.InvalidDate, RegistrationRules.ValidateDateOfBirth(today.AddDays(1), today)?.Code);
        Assert.Equal(FieldErrors.InvalidDate, RegistrationRules.ValidateDateOfBirth(new DateOnly(1899, 12, 31), today)?.Code);
        Assert.Equal(FieldErrors.Required, RegistrationRules.ValidateDateOfBirth(null, today)?.Code);
    }
}

public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Google_sign_in_status_is_serialized_as_a_string()
    {
        var json = JsonSerializer.Serialize(new GoogleSignInResponse(GoogleSignInStatus.RegistrationRequired, RegistrationTicket: "t"), Web);

        Assert.Contains("\"status\":\"RegistrationRequired\"", json);
        var back = JsonSerializer.Deserialize<GoogleSignInResponse>(json, Web);
        Assert.Equal(GoogleSignInStatus.RegistrationRequired, back!.Status);
        Assert.Equal("t", back.RegistrationTicket);
    }

    [Fact]
    public void Sign_up_request_round_trips_with_date_of_birth()
    {
        var request = new SignUpRequest { Email = "ana@mail.com", Name = "Ana", DateOfBirth = new DateOnly(2000, 2, 29), Avatar = new AvatarUpload("image/png", "AAAA") };

        var back = JsonSerializer.Deserialize<SignUpRequest>(JsonSerializer.Serialize(request, Web), Web);

        Assert.Equal(request, back);
    }
}
