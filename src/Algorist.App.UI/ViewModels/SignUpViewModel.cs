using Algorist.App.UI.Auth;
using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Platform;
using Algorist.App.UI.Resources;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Algorist.Contracts.Validation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;

namespace Algorist.App.UI.ViewModels;

/// <summary>Values entered on the Sign Up screen.</summary>
public sealed class SignUpForm
{
    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Date of birth as typed, in the current language's day/month/year format (see <see cref="DateInput"/>).</summary>
    public string DateOfBirthText { get; set; } = string.Empty;

    /// <summary>The typed date of birth, or null when empty or not a real date.</summary>
    public DateOnly? DateOfBirth
    {
        get => DateInput.Parse(DateOfBirthText);
        set => DateOfBirthText = DateInput.Format(value);
    }

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
}

/// <summary>State and actions of the Sign Up screen (email registration, or finishing a Google registration).</summary>
public sealed class SignUpViewModel(
    AuthApiClient api,
    AuthSession session,
    NavigationManager navigation,
    ErrorMessageTranslator errors,
    IStringLocalizer<AppStrings> localizer,
    PublicSettings settings,
    PendingGoogleRegistration pendingGoogle,
    IImageResizer imageResizer,
    TimeProvider time)
{
    public SignUpForm Form { get; } = new();

    /// <summary>Google data when Sign Up was reached from Google sign-in; null for an email registration.</summary>
    public GoogleRegistration? Google { get; private set; }

    public bool IsGoogle => Google is not null;

    /// <summary>Minimum age from the server; null when the server could not be reached (the API still enforces it).</summary>
    public int? MinimumAgeYears { get; private set; }

    public DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    /// <summary>The uploaded image (already resized), if any.</summary>
    public byte[]? AvatarBytes { get; private set; }

    public string? AvatarContentType { get; private set; }

    /// <summary>Keep the Google profile picture as avatar (Google registration without an uploaded image).</summary>
    public bool UseGoogleAvatar { get; private set; }

    /// <summary>Image to preview: the uploaded image as a data URL, the Google picture, or null.</summary>
    public string? AvatarPreviewUrl =>
        AvatarBytes is not null ? $"data:{AvatarContentType};base64,{Convert.ToBase64String(AvatarBytes)}"
        : UseGoogleAvatar ? Google?.PictureUrl
        : null;

    public string? AvatarError { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsBusy { get; private set; }

    public async Task InitializeAsync()
    {
        Google = pendingGoogle.Take();
        if (Google is not null)
        {
            Form.Email = Google.Email;
            Form.Name = Google.Name ?? string.Empty;
            UseGoogleAvatar = !string.IsNullOrEmpty(Google.PictureUrl);
        }

        MinimumAgeYears = (await settings.GetAsync())?.MinimumAgeYears;
    }

    public bool IsPasswordRuleMet(PasswordRule rule) => PasswordPolicy.IsMet(rule, Form.Password);

    /// <summary>Translated messages for invalid fields, keyed by <see cref="SignUpForm"/> property name.</summary>
    public IReadOnlyDictionary<string, string> Validate()
    {
        var messages = new Dictionary<string, string>();
        void Add(string field, string? message)
        {
            if (message is not null)
            {
                messages[field] = message;
            }
        }

        if (!IsGoogle)
        {
            Add(nameof(SignUpForm.Email), localizer.Message(RegistrationRules.ValidateEmail(Form.Email)));
        }

        Add(nameof(SignUpForm.Name), localizer.Message(RegistrationRules.ValidateName(Form.Name)));
        Add(nameof(SignUpForm.DateOfBirthText), ValidateDateOfBirth());

        if (!IsGoogle)
        {
            Add(nameof(SignUpForm.Password), string.IsNullOrEmpty(Form.Password)
                ? localizer.Message(new FieldError(FieldErrors.Required))
                : PasswordPolicy.IsStrong(Form.Password) ? null : localizer["Validation_PasswordRules"].Value);
            Add(nameof(SignUpForm.ConfirmPassword), string.IsNullOrEmpty(Form.ConfirmPassword)
                ? localizer.Message(new FieldError(FieldErrors.Required))
                : Form.ConfirmPassword == Form.Password ? null : localizer["Validation_PasswordMismatch"].Value);
        }

        Add(nameof(SignUpForm.PhoneNumber), localizer.Message(RegistrationRules.ValidatePhone(Form.PhoneNumber)));
        Add(nameof(SignUpForm.Address), localizer.Message(RegistrationRules.ValidateAddress(Form.Address)));
        return messages;
    }

    /// <summary>Uses a chosen image as avatar after reducing it to the allowed size.</summary>
    public async Task SetAvatarAsync(IBrowserFile file)
    {
        AvatarError = null;
        if (!AvatarRules.IsAllowedContentType(file.ContentType))
        {
            AvatarError = localizer["Validation_UnsupportedImage"].Value;
            return;
        }

        var bytes = await imageResizer.ResizeAsync(file, AvatarRules.MaxDimension, AvatarRules.MaxDimension, AvatarRules.MaxBytes);
        if (bytes is null || bytes.Length == 0)
        {
            AvatarError = errors.Translate(ErrorCodes.InvalidAvatar);
            return;
        }

        AvatarBytes = bytes;
        AvatarContentType = file.ContentType.ToLowerInvariant();
        UseGoogleAvatar = false;
    }

    public void RemoveAvatar()
    {
        AvatarBytes = null;
        AvatarContentType = null;
        AvatarError = null;
        UseGoogleAvatar = false;
    }

    /// <summary>Creates the account (call only when <see cref="Validate"/> reports no messages).</summary>
    public async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await api.SignUpAsync(BuildRequest());
            if (result.IsSuccess)
            {
                await session.SignInAsync(result.Value!.Token);
                navigation.NavigateTo(AppRoutes.Relative(AppRoutes.Home), replace: true);
                return;
            }

            ErrorMessage = result.ErrorCode switch
            {
                ErrorCodes.GoogleSignInFailed when IsGoogle => localizer["SignUp_GoogleExpired"].Value,
                ErrorCodes.Underage when MinimumAgeYears is { } years => localizer["Validation_MinimumAge", years].Value,
                _ => errors.Translate(result.ErrorCode),
            };
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void GoToSignIn() => navigation.NavigateTo(AppRoutes.Relative(AppRoutes.SignIn));

    private string? ValidateDateOfBirth()
    {
        if (!string.IsNullOrWhiteSpace(Form.DateOfBirthText) && Form.DateOfBirth is null)
        {
            return localizer.Message(new FieldError(FieldErrors.InvalidDate));
        }

        var error = RegistrationRules.ValidateDateOfBirth(Form.DateOfBirth, Today);
        if (error is not null)
        {
            return localizer.Message(error);
        }

        return MinimumAgeYears is { } years && !AgePolicy.IsOldEnough(Form.DateOfBirth!.Value, Today, years)
            ? localizer["Validation_MinimumAge", years].Value
            : null;
    }

    private SignUpRequest BuildRequest() => new()
    {
        Email = Form.Email.Trim(),
        Name = Form.Name.Trim(),
        DateOfBirth = Form.DateOfBirth!.Value,
        Password = IsGoogle ? null : Form.Password,
        PhoneNumber = RegistrationRules.NormalizeOptional(Form.PhoneNumber),
        Address = RegistrationRules.NormalizeOptional(Form.Address),
        Avatar = AvatarBytes is null ? null : new AvatarUpload(AvatarContentType!, Convert.ToBase64String(AvatarBytes)),
        GoogleRegistrationTicket = Google?.Ticket,
        UseGoogleAvatar = IsGoogle && UseGoogleAvatar,
    };
}
