using Infrastructure.Data.Entities.Games.Settings.Mafia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class MafiaSettingsTemplateConfiguration : IEntityTypeConfiguration<MafiaSettingsTemplate>
{
    public void Configure(EntityTypeBuilder<MafiaSettingsTemplate> builder)
    {
        builder.Property(x => x.Name)
            .HasDefaultValue(MafiaSettingsTemplate.DefaultTemplateName);

        builder.HasIndex(x => new { x.MafiaSettingsId, x.Name })
            .IsUnique();
    }
}
