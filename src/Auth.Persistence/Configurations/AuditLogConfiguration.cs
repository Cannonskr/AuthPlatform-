using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(al => al.OldValues)
            .HasColumnType("JSON");

        builder.Property(al => al.NewValues)
            .HasColumnType("JSON");

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.Action);
        builder.HasIndex(al => al.Timestamp);
        builder.HasIndex(al => new { al.EntityType, al.EntityId });
    }
}
