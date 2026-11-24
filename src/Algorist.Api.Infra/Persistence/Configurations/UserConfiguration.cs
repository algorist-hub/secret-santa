using Algorist.Api.Domain.Users;
using Algorist.Contracts.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Algorist.Api.Infra.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string EmailIndexName = "IX_Users_Email";
    public const string GoogleSubjectIndexName = "IX_Users_GoogleSubject";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(RegistrationRules.EmailMaxLength).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(RegistrationRules.NameMaxLength).IsRequired();
        builder.Property(u => u.DateOfBirth).HasColumnType("date");
        builder.Property(u => u.PhoneNumber).HasMaxLength(RegistrationRules.PhoneMaxLength);
        builder.Property(u => u.Address).HasMaxLength(RegistrationRules.AddressMaxLength);
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.GoogleSubject).HasMaxLength(255);
        builder.Property(u => u.GoogleAvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.CreatedAtUtc).HasColumnType("datetime2");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName(EmailIndexName);
        builder.HasIndex(u => u.GoogleSubject)
            .IsUnique()
            .HasFilter("[GoogleSubject] IS NOT NULL")
            .HasDatabaseName(GoogleSubjectIndexName);

        builder.HasOne(u => u.Avatar)
            .WithOne()
            .HasForeignKey<UserAvatar>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("UserAvatars");
        builder.HasKey(a => a.UserId);
        builder.Property(a => a.ContentType).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(a => a.Content).IsRequired();
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("datetime2");
    }
}
