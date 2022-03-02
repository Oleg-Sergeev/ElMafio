using Infrastructure.Data.Models.Guild;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class GuildSettingsConfiguration : IEntityTypeConfiguration<GuildSettings>
{
    public void Configure(EntityTypeBuilder<GuildSettings> builder)
    {
        builder.Property(g => g.DebugMode)
            .HasDefaultValue(DebugMode.Off);

        builder.Property(g => g.Prefix)
            .HasDefaultValue("/");
    }
}
