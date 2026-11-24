namespace Algorist.Api.Domain.Users;

/// <summary>A registered person. Email accounts have a password hash; Google accounts have a Google subject.</summary>
public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Lowercase, trimmed and unique.</summary>
    public string Email { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateOnly DateOfBirth { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Salted one-way hash; null for accounts that sign in only with Google.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Google account identifier (<c>sub</c>) when linked to Google.</summary>
    public string? GoogleSubject { get; private set; }

    /// <summary>Google profile picture used as avatar when no image was uploaded.</summary>
    public string? GoogleAvatarUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Uploaded avatar image, if any.</summary>
    public UserAvatar? Avatar { get; private set; }

    public static User Create(
        string email,
        string name,
        DateOnly dateOfBirth,
        string? phoneNumber,
        string? address,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            Name = name,
            DateOfBirth = dateOfBirth,
            PhoneNumber = phoneNumber,
            Address = address,
            CreatedAtUtc = createdAtUtc,
        };
    }

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void LinkGoogle(string googleSubject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleSubject);
        GoogleSubject = googleSubject;
    }

    public void UseGoogleAvatar(string? pictureUrl) => GoogleAvatarUrl = string.IsNullOrWhiteSpace(pictureUrl) ? null : pictureUrl;

    public void SetAvatar(string contentType, byte[] content, DateTime updatedAtUtc) =>
        Avatar = new UserAvatar(Id, contentType, content, updatedAtUtc);
}
