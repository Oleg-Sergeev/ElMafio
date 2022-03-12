using Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class AccessLevelConfiguration : IEntityTypeConfiguration<AccessLevel>
{
    public virtual void Configure(EntityTypeBuilder<AccessLevel> builder)
    {
        builder.HasIndex(x => new { x.ServerId, x.Name })
            .IsUnique();

        builder.Property(x => x.Name)
            .IsUnicode(true)
            .HasDefaultValue("Developer");

        builder.Property(x => x.Priority)
            .HasDefaultValue(int.MaxValue);
    }
}
