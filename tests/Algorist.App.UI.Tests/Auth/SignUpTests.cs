using System.Net;
using System.Text.Json;
using Algorist.App.UI.Auth;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Pages;
using Algorist.App.UI.Tests.TestSupport;
using Algorist.App.UI.ViewModels;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Algorist.App.UI.Tests.Auth;

public class SignUpViewModelTests : AppTestContext
{
    private async Task<SignUpViewModel> CreateVmAsync(int minimumAge = 18)
    {
        RespondApi(minimumAge, other: _ => ApiResponses.Json(new AuthTokenResponse(TestTokens.For("new-user"), DateTimeOffset.UtcNow.AddDays(60))));
        var vm = Services.GetRequiredService<SignUpViewModel>();
        await vm.InitializeAsync();
        return vm;
    }

    private static void FillValid(SignUpViewModel vm)
    {
        vm.Form.Email = "ana@mail.com";
        vm.Form.Name = "Ana Lima";
        vm.Form.DateOfBirth = new DateOnly(1990, 5, 10);
        vm.Form.Password = "Gift#2026box";
        vm.Form.ConfirmPassword = "Gift#2026box";
    }

    [Fact]
    public async Task Valid_form_has_no_messages()
    {
        var vm = await CreateVmAsync();
        FillValid(vm);

        Assert.Empty(vm.Validate());
    }

    [Fact]
    public async Task Seventeen_year_old_sees_the_minimum_age_of_18()
    {
        using var _ = new CultureScope("en");
        var vm = await CreateVmAsync();
        FillValid(vm);
        vm.Form.DateOfBirth = Today.AddYears(-17);

        Assert.Equal("You must be at least 18 years old to sign up.", vm.Validate()[nameof(SignUpForm.DateOfBirthText)]);
    }

    [Fact]
    public async Task Configured_minimum_age_21_is_shown_to_a_20_year_old()
    {
        using var _ = new CultureScope("pt-BR");
        var vm = await CreateVmAsync(minimumAge: 21);
        FillValid(vm);
        vm.Form.DateOfBirth = Today.AddYears(-20);

        Assert.Equal(21, vm.MinimumAgeYears);
        Assert.Equal("É preciso ter pelo menos 21 anos para se cadastrar.", vm.Validate()[nameof(SignUpForm.DateOfBirthText)]);
    }

    [Fact]
    public async Task Exactly_the_minimum_age_today_is_accepted()
    {
        var vm = await CreateVmAsync();
        FillValid(vm);
        vm.Form.DateOfBirth = Today.AddYears(-18);

        Assert.False(vm.Validate().ContainsKey(nameof(SignUpForm.DateOfBirthText)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-47000)]
    public async Task Future_or_too_old_date_is_invalid(int daysFromToday)
    {
        using var _ = new CultureScope("en");
        var vm = await CreateVmAsync();
        FillValid(vm);
        vm.Form.DateOfBirth = Today.AddDays(daysFromToday);

        Assert.Equal("Enter a valid date of birth.", vm.Validate()[nameof(SignUpForm.DateOfBirthText)]);
    }

    [Fact]
    public async Task Required_fields_and_password_rules_are_checked()
    {
        using var _ = new CultureScope("en");
        var vm = await CreateVmAsync();
        vm.Form.Password = "password1";
        vm.Form.ConfirmPassword = "password2";

        var messages = vm.Validate();

        Assert.Equal("This field is required.", messages[nameof(SignUpForm.Email)]);
        Assert.Equal("This field is required.", messages[nameof(SignUpForm.Name)]);
        Assert.Equal("This field is required.", messages[nameof(SignUpForm.DateOfBirthText)]);
        Assert.Equal("The password does not meet all the requirements.", messages[nameof(SignUpForm.Password)]);
        Assert.Equal("Passwords do not match.", messages[nameof(SignUpForm.ConfirmPassword)]);
        Assert.False(messages.ContainsKey(nameof(SignUpForm.PhoneNumber)));
    }

    [Fact]
    public async Task Successful_sign_up_sends_the_form_signs_in_and_shows_home()
    {
        var vm = await CreateVmAsync();
        FillValid(vm);
        vm.Form.PhoneNumber = "  ";
        vm.Form.Address = " Rua A, 1 ";

        await vm.SubmitAsync();

        var sent = JsonSerializer.Deserialize<SignUpRequest>(Api.Bodies.Last(), JsonSerializerOptions.Web)!;
        Assert.Equal(("ana@mail.com", "Ana Lima", new DateOnly(1990, 5, 10), "Gift#2026box"), (sent.Email, sent.Name, sent.DateOfBirth, sent.Password));
        Assert.Null(sent.PhoneNumber);
        Assert.Equal("Rua A, 1", sent.Address);
        Assert.Null(sent.GoogleRegistrationTicket);
        Assert.True(Session.IsSignedIn);
        Assert.NotNull(TokenStorage.Token);
        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public async Task Email_already_registered_shows_its_message()
    {
        using var _ = new CultureScope("en");
        var vm = await CreateVmAsync();
        RespondApi(other: _ => ApiResponses.Problem(HttpStatusCode.Conflict, ErrorCodes.EmailAlreadyRegistered));
        FillValid(vm);

        await vm.SubmitAsync();

        Assert.Equal("This email is already registered. Try signing in.", vm.ErrorMessage);
        Assert.False(Session.IsSignedIn);
    }

    [Fact]
    public async Task Underage_from_the_server_states_the_minimum_age()
    {
        using var _ = new CultureScope("en");
        var vm = await CreateVmAsync();
        RespondApi(other: _ => ApiResponses.Problem(HttpStatusCode.BadRequest, ErrorCodes.Underage));
        FillValid(vm);

        await vm.SubmitAsync();

        Assert.Equal("You must be at least 18 years old to sign up.", vm.ErrorMessage);
    }
}

public class DateOfBirthInputTests : AppTestContext
{
    [Theory]
    [InlineData("pt-BR", "10/05/1990")]
    [InlineData("pt-BR", "10051990")]
    [InlineData("pt-BR", "10-05-1990")]
    [InlineData("pt-BR", "1/5/1990")]
    [InlineData("en", "10/05/1990")]
    public void Day_first_dates_are_read_in_both_languages(string culture, string typed)
    {
        using var _ = new CultureScope(culture, culture == "en" ? "en-GB" : culture);

        Assert.Equal(typed == "1/5/1990" ? new DateOnly(1990, 5, 1) : new DateOnly(1990, 5, 10), Algorist.App.UI.Localization.DateInput.Parse(typed));
    }

    [Theory]
    [InlineData("31/02/1990")]
    [InlineData("1990-05-10")]
    [InlineData("10/05/90")]
    [InlineData("abc")]
    public void Impossible_or_other_formats_are_not_dates(string typed)
    {
        using var _ = new CultureScope("pt-BR");

        Assert.Null(Algorist.App.UI.Localization.DateInput.Parse(typed));
    }

    [Fact]
    public async Task Unreadable_date_is_reported_as_invalid_and_typed_date_is_sent()
    {
        using var _ = new CultureScope("pt-BR");
        RespondApi();
        var vm = Services.GetRequiredService<SignUpViewModel>();
        await vm.InitializeAsync();

        vm.Form.DateOfBirthText = "31/02/1990";
        Assert.Equal("Informe uma data de nascimento válida.", vm.Validate()[nameof(SignUpForm.DateOfBirthText)]);

        vm.Form.DateOfBirthText = "10/05/1990";
        Assert.Equal(new DateOnly(1990, 5, 10), vm.Form.DateOfBirth);
        Assert.False(vm.Validate().ContainsKey(nameof(SignUpForm.DateOfBirthText)));
    }

    [Theory]
    [InlineData("pt-BR", "pt-BR", "dd/mm/aaaa", "Formato: dd/mm/aaaa")]
    [InlineData("en", "en-GB", "dd/mm/yyyy", "Format: dd/mm/yyyy")]
    public void Placeholder_and_hint_follow_the_app_language(string uiCulture, string culture, string placeholder, string hint)
    {
        using var _ = new CultureScope(uiCulture, culture);
        RespondApi();

        var cut = Render<SignUp>();

        cut.WaitForAssertion(() => cut.Find("#sign-up-dob"));
        Assert.Equal(placeholder, cut.Find("#sign-up-dob").GetAttribute("placeholder"));
        Assert.Equal(hint, cut.Find("#sign-up-dob-hint").TextContent);
    }
}

public class SignUpPageTests : AppTestContext
{
    public SignUpPageTests() => RespondApi();

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    [Fact]
    public void Email_form_shows_required_and_optional_fields_without_menu()
    {
        using var _ = new CultureScope("en");

        var cut = Render<SignUp>();

        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));
        Assert.Equal("Sign Up", cut.Find("h1").TextContent);
        foreach (var id in new[] { "sign-up-email", "sign-up-name", "sign-up-dob", "sign-up-password", "sign-up-confirm-password" })
        {
            Assert.NotNull(cut.Find($"label[for={id}] .required-mark"));
            Assert.Equal("true", cut.Find($"#{id}").GetAttribute("aria-required"));
        }

        Assert.Equal(3, cut.FindAll(".optional-section .optional-mark").Count);
        Assert.NotNull(cut.Find("#sign-up-phone"));
        Assert.NotNull(cut.Find("#sign-up-address"));
        Assert.NotNull(cut.Find("#sign-up-avatar"));
        Assert.Null(cut.Find("#sign-up-email").GetAttribute("readonly"));
        Assert.Equal("dd/mm/yyyy", cut.Find("#sign-up-dob").GetAttribute("placeholder"));
        Assert.Equal("text", cut.Find("#sign-up-dob").GetAttribute("type") ?? "text");
        Assert.Empty(cut.FindAll("nav"));
    }

    [Theory]
    [InlineData("en", "Enter a valid phone number (8 to 15 digits).", "The password does not meet all the requirements.")]
    [InlineData("pt-BR", "Informe um telefone válido (de 8 a 15 dígitos).", "A senha não atende a todos os requisitos.")]
    public void Invalid_phone_and_weak_password_show_translated_messages(string culture, string phoneMessage, string passwordMessage)
    {
        using var _ = new CultureScope(culture);
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));

        cut.Find("#sign-up-phone").Change("12ab");
        cut.Find("#sign-up-password").Input("password1");
        cut.Find("form").Submit();

        var messages = cut.FindAll(".invalid-feedback").Select(m => m.TextContent).ToList();
        Assert.Contains(phoneMessage, messages);
        Assert.Contains(passwordMessage, messages);
        Assert.Contains("is-invalid", cut.Find("#sign-up-phone").ClassName);
        Assert.DoesNotContain(Api.Requests, r => r.RequestUri!.AbsolutePath == "/" + ApiRoutes.SignUp);
    }

    [Fact]
    public void Password_checklist_updates_while_typing()
    {
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));

        cut.Find("#sign-up-password").Input("password1");

        var met = cut.FindAll(".password-rules li.met").Select(li => li.GetAttribute("data-rule")).ToList();
        Assert.Equal(["Length", "Lowercase", "Digit"], met);

        cut.Find("#sign-up-password").Input("Gift#2026box");
        Assert.Equal(5, cut.FindAll(".password-rules li.met").Count);
    }

    [Fact]
    public void Back_link_opens_sign_in()
    {
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("#go-sign-in"));

        cut.Find("#go-sign-in").Click();

        Assert.Equal(AppRoutes.SignIn, CurrentPath);
    }

    [Fact]
    public async Task Signed_in_user_is_sent_home()
    {
        await SignInAsync();

        Render<SignUp>();

        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public void Chosen_image_is_resized_previewed_and_removable()
    {
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Jpeg, "photo.jpg", contentType: "image/jpeg"));

        Assert.Equal((512, 512), Resizer.Calls.Single());
        Assert.StartsWith("data:image/jpeg;base64,", cut.Find(".avatar-preview img").GetAttribute("src"));

        cut.Find("#sign-up-avatar-remove").Click();
        Assert.Empty(cut.FindAll(".avatar-preview img"));
    }

    [Fact]
    public void Pdf_is_rejected_as_unsupported_image()
    {
        using var _ = new CultureScope("en");
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary("%PDF"u8.ToArray(), "doc.pdf", contentType: "application/pdf"));

        Assert.Equal("Unsupported image. Use JPEG, PNG or WebP.", cut.Find("#sign-up-avatar-error").TextContent);
        Assert.Empty(cut.FindAll(".avatar-preview img"));
        Assert.Empty(Resizer.Calls);
    }

    [Fact]
    public void Uploaded_avatar_is_sent_with_sign_up()
    {
        RespondApi(other: _ => ApiResponses.Json(new AuthTokenResponse(TestTokens.For("u"), DateTimeOffset.UtcNow.AddDays(60))));
        var cut = Render<SignUp>();
        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));
        cut.Find("#sign-up-email").Change("ana@mail.com");
        cut.Find("#sign-up-name").Change("Ana");
        cut.Find("#sign-up-dob").Change("10/05/1990");
        cut.Find("#sign-up-password").Input("Gift#2026box");
        cut.Find("#sign-up-confirm-password").Change("Gift#2026box");
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(Jpeg, "photo.jpg", contentType: "image/jpeg"));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(AppRoutes.Home, CurrentPath));
        var sent = JsonSerializer.Deserialize<SignUpRequest>(Api.Bodies.Last(), JsonSerializerOptions.Web)!;
        Assert.Equal(new AvatarUpload("image/jpeg", Convert.ToBase64String(Jpeg)), sent.Avatar);
    }
}

public class GoogleSignUpTests : AppTestContext
{
    private static readonly GoogleRegistration Ana = new("ticket-1", "ana@gmail.com", "Ana Google", "https://lh3.googleusercontent.com/a/ana");

    [Fact]
    public void Google_data_is_prefilled_email_read_only_and_no_password_fields()
    {
        using var _ = new CultureScope("en");
        RespondApi();
        PendingGoogle.Current = Ana;

        var cut = Render<SignUp>();

        cut.WaitForAssertion(() => cut.Find("form.sign-up-form"));
        Assert.Equal("ana@gmail.com", cut.Find("p#sign-up-email").TextContent.Trim());
        Assert.Empty(cut.FindAll("input[type=email]"));
        Assert.NotNull(cut.Find("label[for=sign-up-dob] .required-mark"));
        Assert.Equal("Ana Google", cut.Find("#sign-up-name").GetAttribute("value"));
        Assert.Null(cut.Find("#sign-up-name").GetAttribute("readonly"));
        Assert.Equal(Ana.PictureUrl, cut.Find(".avatar-preview img").GetAttribute("src"));
        Assert.Empty(cut.FindAll("#sign-up-password, #sign-up-confirm-password"));
        Assert.Equal("Finish signing up with the details from your Google account.", cut.Find(".auth-subtitle").TextContent);
        Assert.Null(PendingGoogle.Current);
    }

    [Fact]
    public async Task Google_sign_up_sends_the_ticket_and_keeps_the_google_picture()
    {
        RespondApi(other: _ => ApiResponses.Json(new AuthTokenResponse(TestTokens.For("g"), DateTimeOffset.UtcNow.AddDays(60))));
        PendingGoogle.Current = Ana;
        var vm = Services.GetRequiredService<SignUpViewModel>();
        await vm.InitializeAsync();
        vm.Form.DateOfBirth = new DateOnly(1995, 3, 3);

        Assert.Empty(vm.Validate());
        await vm.SubmitAsync();

        var sent = JsonSerializer.Deserialize<SignUpRequest>(Api.Bodies.Last(), JsonSerializerOptions.Web)!;
        Assert.Equal(("ticket-1", true, (string?)null), (sent.GoogleRegistrationTicket, sent.UseGoogleAvatar, sent.Password));
        Assert.Equal(AppRoutes.Home, CurrentPath);
    }

    [Fact]
    public async Task Removing_the_google_picture_sends_no_avatar()
    {
        RespondApi(other: _ => ApiResponses.Json(new AuthTokenResponse(TestTokens.For("g"), DateTimeOffset.UtcNow.AddDays(60))));
        PendingGoogle.Current = Ana;
        var vm = Services.GetRequiredService<SignUpViewModel>();
        await vm.InitializeAsync();
        vm.Form.DateOfBirth = new DateOnly(1995, 3, 3);

        vm.RemoveAvatar();
        await vm.SubmitAsync();

        var sent = JsonSerializer.Deserialize<SignUpRequest>(Api.Bodies.Last(), JsonSerializerOptions.Web)!;
        Assert.False(sent.UseGoogleAvatar);
        Assert.Null(sent.Avatar);
    }

    [Fact]
    public async Task Expired_google_data_asks_to_sign_in_with_google_again()
    {
        using var _ = new CultureScope("en");
        RespondApi(other: _ => ApiResponses.Problem(HttpStatusCode.Unauthorized, ErrorCodes.GoogleSignInFailed));
        PendingGoogle.Current = Ana;
        var vm = Services.GetRequiredService<SignUpViewModel>();
        await vm.InitializeAsync();
        vm.Form.DateOfBirth = new DateOnly(1995, 3, 3);

        await vm.SubmitAsync();

        Assert.Equal("Your Google sign-in has expired. Please sign in with Google again.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Without_pending_google_data_the_email_form_is_shown()
    {
        RespondApi();
        PendingGoogle.Current = Ana;
        var first = Services.GetRequiredService<SignUpViewModel>();
        await first.InitializeAsync();

        // A reload creates a fresh screen; the Google data was used once and is gone.
        var reloaded = Services.GetRequiredService<SignUpViewModel>();
        await reloaded.InitializeAsync();

        Assert.True(first.IsGoogle);
        Assert.False(reloaded.IsGoogle);
        Assert.Equal(string.Empty, reloaded.Form.Email);
    }
}
