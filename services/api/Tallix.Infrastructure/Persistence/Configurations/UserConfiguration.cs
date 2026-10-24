using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id).HasName("pk_users");

        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.AuthSubject).HasColumnName("auth_subject").HasMaxLength(User.MaxAuthSubjectLength);
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(User.MaxEmailLength);
        builder.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(User.MaxDisplayNameLength);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        // Emails are lowercased by the domain, so a plain unique index is enough.
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");

        // One Tallix user per identity-provider subject. Also makes the
        // "two first requests at once" race fail safely instead of duplicating.
        builder.HasIndex(u => u.AuthSubject).IsUnique().HasDatabaseName("ux_users_auth_subject");
    }
}