using Algorist.Api.Application.Common;
using Algorist.Api.Application.Users;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Algorist.Contracts.Validation;

namespace Algorist.Api.Application.Auth;

/// <summary>
/// Signs in with a Google ID token. Matches the user by Google account, then by verified email (linking it);
/// otherwise returns a registration ticket and the Google profile without creating an account.
/// </summary>
public sealed class GoogleSignIn(
    IGoogleIdTokenValidator validator,
    IUserRepository users,
    ITokenIssuer tokens,
    IGoogleRegistrationTicketService tickets)
{
    public async Task<Result<GoogleSignInResponse>> ExecuteAsync(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var identity = string.IsNullOrWhiteSpace(request.IdToken)
            ? null
            : await validator.ValidateAsync(request.IdToken, cancellationToken);
        if (identity is null || !identity.EmailVerified)
        {
            return Result<GoogleSignInResponse>.Fail(ErrorCodes.GoogleSignInFailed);
        }

        var user = await users.FindByGoogleSubjectAsync(identity.Subject, cancellationToken);
        var email = RegistrationRules.NormalizeEmail(identity.Email);
        if (user is null)
        {
            user = await users.FindByEmailAsync(email, cancellationToken);
            if (user is not null)
            {
                if (user.GoogleSubject is not null)
                {
                    // The email's account is already linked to a different Google account.
                    return Result<GoogleSignInResponse>.Fail(ErrorCodes.GoogleSignInFailed);
                }

                user.LinkGoogle(identity.Subject);
                await users.SaveChangesAsync(cancellationToken);
            }
        }

        if (user is not null)
        {
            var issued = tokens.Issue(user);
            return new GoogleSignInResponse(GoogleSignInStatus.SignedIn, issued.Token, issued.ExpiresAtUtc);
        }

        var ticket = tickets.Create(new GoogleRegistrationTicketData(identity.Subject, email, identity.Name, identity.PictureUrl));
        return new GoogleSignInResponse(
            GoogleSignInStatus.RegistrationRequired,
            RegistrationTicket: ticket,
            Email: email,
            Name: identity.Name,
            PictureUrl: identity.PictureUrl);
    }
}
