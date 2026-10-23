using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tallix.Domain.Contacts;
using Tallix.Domain.Ledger;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(e => e.Id).HasName("pk_ledger_entries");

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ContactId).HasColumnName("contact_id");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30);

        // Money is one C# value but two columns: amount + currency.
        builder.ComplexProperty(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(19, 4);

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(LedgerEntry.MaxDescriptionLength);

        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");
        builder.Property(e => e.DueDate).HasColumnName("due_date");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        // Computed in C#, never stored.
        builder.Ignore(e => e.SignedAmount);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("fk_ledger_entries_users")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contact>()
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .HasConstraintName("fk_ledger_entries_contacts")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_ledger_entries_user_id");
        builder.HasIndex(e => e.ContactId).HasDatabaseName("ix_ledger_entries_contact_id");
        builder.HasIndex(e => e.OccurredAt).HasDatabaseName("ix_ledger_entries_occurred_at");
        builder.HasIndex(e => e.DueDate).HasDatabaseName("ix_ledger_entries_due_date");
    }
}