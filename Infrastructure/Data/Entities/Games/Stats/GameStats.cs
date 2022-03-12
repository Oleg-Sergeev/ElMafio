using Core.Interfaces;
using Infrastructure.Data.Entities.ServerInfo;

namespace Infrastructure.Data.Entities.Games.Stats;

public abstract class GameStats : IResetable
{
    public int GamesCount { get; set; }

    public int WinsCount { get; set; }

    public float WinRate { get; protected set; }


    public float Rating { get; protected set; }


    public ulong UserId { get; set; }
    public User User { get; set; } = null!;

    public ulong ServerId { get; set; }
    public Server Server { get; set; } = null!;


    public virtual void Reset()
    {
        GamesCount = 0;
        WinsCount = 0;
        Rating = 0;
    }
}