namespace Algorist.Api.Domain.Users;

/// <summary>An uploaded avatar image (stored apart from <see cref="User"/> so user queries never load image bytes).</summary>
public sealed class UserAvatar
{
    private UserAvatar()
    {
    }

    public UserAvatar(Guid userId, string contentType, byte[] content, DateTime updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);

        UserId = userId;
        ContentType = contentType;
        Content = content;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid UserId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public byte[] Content { get; private set; } = [];

    public DateTime UpdatedAtUtc { get; private set; }
}
