using Infrastructure.Data.Models.Guild;
using Infrastructure.Data.Models.ServerInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ServerSettingsConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.Property(g => g.DebugMode)
            .HasDefaultValue(DebugMode.Off);
        
        builder.Property(g => g.BlockBehaviour)
            .HasDefaultValue(BlockBehaviour.SendToDM);

        builder.Property(g => g.Prefix)
            .HasDefaultValue("/");

        builder.Property(g => g.BlockMessage)
            .HasDefaultValue("Вам заблокирован доступ к командам. Пожалуйста, обратитесь к администраторам сервера для разблокировки");

        builder.Property(g => g.SendInterval)
            .HasDefaultValue(60);

        builder.HasCheckConstraint($"CK_{nameof(Server)}s_{nameof(Server.SendInterval)}", $"[{nameof(Server.SendInterval)}] BETWEEN 1 AND 600");
    }
}
