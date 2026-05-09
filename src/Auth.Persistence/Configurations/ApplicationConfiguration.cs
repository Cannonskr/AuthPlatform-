using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Domain.Entities.Application>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.ApiKey)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(a => a.ApiKey).IsUnique();

        builder.Property(a => a.IsActive)
            .HasDefaultValue(true);
    }
}
