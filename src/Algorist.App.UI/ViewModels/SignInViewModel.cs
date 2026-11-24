using Algorist.App.UI.Auth;
using Algorist.App.UI.Errors;
using Algorist.App.UI.Http;
using Algorist.App.UI.Localization;
using Algorist.App.UI.Navigation;
using Algorist.App.UI.Resources;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Algorist.Contracts.Validation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Algorist.App.UI.ViewModels;

/// <summary>State and actions of the Sign In screen.</summary>
public sealed class SignInViewModel(
    AuthApiClient api,
    AuthSession session,
    NavigationManager navigation,
    ErrorMessageTranslator errors,
    IStringLocalizer<AppStrings> localizer,
    PendingGoogleRegistration pendingGoogle)
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? EmailError { get; private set; }

    public string? PasswordError { get; private set; }

    /// <summary>Translated message for a failed sign-in (shown above the form).</summary>
    public string? ErrorMessage { get; private set; }

    public bool IsBusy { get; private set; }

    /// <summary>Checks the fields locally; returns false (with messages) when the server should not be called.</summary>
    public bool Validate()
    {
        EmailError = localizer.Message(RegistrationRules.ValidateEmail(Email));
        PasswordError = string.IsNullOrEmpty(Password) ? localizer.Message(new FieldError(FieldErrors.Required)) : null;
        return EmailError is null && PasswordError is null;
    }

    public async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        if (!Validate())
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await api.SignInAsync(new PasswordSignInRequest(Email.Trim(), Password));
            if (result.IsSuccess)
            {
                await CompleteSignInAsync(result.Value!.Token);
            }
            else
            {
                ErrorMessage = errors.Translate(result.ErrorCode);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Completes Google sign-in: a registered user is signed in; a new person goes to Sign Up with the
    /// Google data pre-filled (no account is created yet).
    /// </summary>
    public async Task GoogleSignInAsync(string idToken)
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await api.GoogleSignInAsync(new GoogleSignInRequest(idToken));
            if (!result.IsSuccess)
            {
                ErrorMessage = errors.Translate(result.ErrorCode);
                return;
            }

            var response = result.Value!;
            if (response.Status == GoogleSignInStatus.SignedIn && !string.IsNullOrEmpty(response.Token))
            {
                await CompleteSignInAsync(response.Token);
            }
            else if (response.Status == GoogleSignInStatus.RegistrationRequired
                     && !string.IsNullOrEmpty(response.RegistrationTicket)
                     && !string.IsNullOrEmpty(response.Email))
            {
                pendingGoogle.Current = new GoogleRegistration(response.RegistrationTicket, response.Email, response.Name, response.PictureUrl);
                navigation.NavigateTo(AppRoutes.Relative(AppRoutes.SignUp));
            }
            else
            {
                ErrorMessage = errors.Translate(ErrorCodes.GoogleSignInFailed);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void GoToSignUp() => navigation.NavigateTo(AppRoutes.Relative(AppRoutes.SignUp));

    public void GoToSelectLanguage() => navigation.NavigateTo(AppRoutes.Relative(AppRoutes.SelectLanguage));

    private async Task CompleteSignInAsync(string token)
    {
        await session.SignInAsync(token);
        navigation.NavigateTo(AppRoutes.Relative(AppRoutes.Home), replace: true);
    }
}
