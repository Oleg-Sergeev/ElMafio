using Infrastructure.Data.Models.Games.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RussianRouletteSettingsConfiguration : IEntityTypeConfiguration<RussianRouletteSettings>
{
    public void Configure(EntityTypeBuilder<RussianRouletteSettings> builder)
    {
        builder.Property(s => s.UnicodeSmileKilled)
                .HasDefaultValue("💀");

        builder.Property(s => s.UnicodeSmileSurvived)
                .HasDefaultValue("😎");
    }
}
