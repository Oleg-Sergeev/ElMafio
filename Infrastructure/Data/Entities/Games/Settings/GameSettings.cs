using Infrastructure.Data.Entities.ServerInfo;

namespace Infrastructure.Data.Entities.Games.Settings;

public abstract class GameSettings
{
    public int Id { get; set; }

    public ulong ServerId { get; set; }

    public Server Server { get; set; } = new();
}