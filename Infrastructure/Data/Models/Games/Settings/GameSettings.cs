using Infrastructure.Data.Models.ServerInfo;

namespace Infrastructure.Data.Models.Games.Settings;

public abstract class GameSettings
{
    public int Id { get; set; }

    public ulong ServerId { get; set; }

    public Server Server { get; set; } = new();
}