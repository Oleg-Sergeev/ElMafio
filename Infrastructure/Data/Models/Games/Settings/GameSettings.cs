using Infrastructure.Data.Models.Guild;

namespace Infrastructure.Data.Models.Games.Settings;

public abstract class GameSettings
{
    public int Id { get; set; }

    public ulong GuildSettingsId { get; set; }

    public GuildSettings GuildSettings { get; set; } = null!;
}