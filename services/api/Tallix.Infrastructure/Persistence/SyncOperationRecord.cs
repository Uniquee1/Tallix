using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tallix.Domain.Users;

namespace Tallix.Infrastructure.Persistence;

// One row per operation the server has handled. This is how a retry is recognized.
internal sealed class SyncOperationRecord
{
    public Guid UserId { get; set; }
    public Guid Id { get; set; }
    public string OperationType { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Status { get; set; } = "";
    public string? Code { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

internal sealed class SyncOperationConfiguration : IEntityTypeConfiguration<SyncOperationRecord>
{
    public void Configure(EntityTypeBuilder<SyncOperationRecord> builder)
    {
        builder.ToTable("sync_operations");

        // The id is the phone's, so it is unique per user, not globally.
        builder.HasKey(o => new { o.UserId, o.Id }).HasName("pk_sync_operations");

        builder.Property(o => o.UserId).HasColumnName("user_id");
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(o => o.OperationType).HasColumnName("operation_type").HasMaxLength(20);
        builder.Property(o => o.EntityType).HasColumnName("entity_type").HasMaxLength(20);
        builder.Property(o => o.EntityId).HasColumnName("entity_id");
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(o => o.Code).HasColumnName("code").HasMaxLength(20);
        builder.Property(o => o.Message).HasColumnName("message").HasMaxLength(500);
        builder.Property(o => o.ReceivedAt)
            .HasColumnName("received_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .HasConstraintName("fk_sync_operations_users")
            .OnDelete(DeleteBehavior.Restrict);
    }
}