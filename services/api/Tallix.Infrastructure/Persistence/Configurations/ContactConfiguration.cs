using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tallix.Domain.Contacts;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence.Configurations;

internal sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(c => c.Id).HasName("pk_contacts");

        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(Contact.MaxNameLength);
        builder.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(Contact.MaxPhoneLength);
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(Contact.MaxEmailLength);
        builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(Contact.MaxNotesLength);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasConstraintName("fk_contacts_users")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.UserId).HasDatabaseName("ix_contacts_user_id");
    }
}