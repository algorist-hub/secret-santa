using Algorist.Api.Application.Auth;
using Algorist.Api.Application.Common;
using Algorist.Api.Application.Users;
using Algorist.Api.Domain.Users;
using Algorist.Contracts.Auth;
using Algorist.Contracts.Errors;
using Algorist.Contracts.Validation;
using Microsoft.Extensions.Options;

namespace Algorist.Api.Application.Registration;

/// <summary>Creates an account (email/password or Google ticket) and signs the new user in.</summary>
public sealed class RegisterUser(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenIssuer tokens,
    IGoogleRegistrationTicketService tickets,
    IOptions<RegistrationOptions> options,
    TimeProvider time)
{
    public async Task<Result<IssuedToken>> ExecuteAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        GoogleRegistrationTicketData? google = null;
        if (!string.IsNullOrWhiteSpace(request.GoogleRegistrationTicket))
        {
            google = await tickets.ReadAsync(request.GoogleRegistrationTicket);
            if (google is null)
            {
                return Result<IssuedToken>.Fail(ErrorCodes.GoogleSignInFailed);
            }
        }

        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var email = RegistrationRules.NormalizeEmail(google?.Email ?? request.Email);

        if (RegistrationRules.ValidateEmail(email) is not null
            || RegistrationRules.ValidateName(request.Name) is not null
            || RegistrationRules.ValidatePhone(request.PhoneNumber) is not null
            || RegistrationRules.ValidateAddress(request.Address) is not null
            || RegistrationRules.ValidateDateOfBirth(request.DateOfBirth, today) is not null
            || (google is not null && !string.IsNullOrEmpty(request.Password)))
        {
            return Result<IssuedToken>.Fail(ErrorCodes.ValidationFailed);
        }

        if (google is null && !PasswordPolicy.IsStrong(request.Password))
        {
            return Result<IssuedToken>.Fail(ErrorCodes.WeakPassword);
        }

        if (!AgePolicy.IsOldEnough(request.DateOfBirth, today, options.Value.MinimumAgeYears))
        {
            return Result<IssuedToken>.Fail(ErrorCodes.Underage);
        }

        byte[]? avatar = null;
        if (request.Avatar is not null && !AvatarImage.TryDecode(request.Avatar, out avatar))
        {
            return Result<IssuedToken>.Fail(ErrorCodes.InvalidAvatar);
        }

        if (await users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<IssuedToken>.Fail(ErrorCodes.EmailAlreadyRegistered);
        }

        var user = User.Create(
            email,
            request.Name.Trim(),
            request.DateOfBirth,
            RegistrationRules.NormalizeOptional(request.PhoneNumber),
            RegistrationRules.NormalizeOptional(request.Address),
            now);

        if (google is null)
        {
            user.SetPasswordHash(hasher.Hash(request.Password!));
        }
        else
        {
            user.LinkGoogle(google.Subject);
            if (avatar is null && request.UseGoogleAvatar)
            {
                user.UseGoogleAvatar(google.PictureUrl);
            }
        }

        if (avatar is not null)
        {
            user.SetAvatar(request.Avatar!.ContentType, avatar, now);
        }

        return await users.AddAsync(user, cancellationToken) switch
        {
            AddUserResult.Added => tokens.Issue(user),
            _ => Result<IssuedToken>.Fail(ErrorCodes.EmailAlreadyRegistered),
        };
    }
}
