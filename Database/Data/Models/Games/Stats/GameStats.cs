using Core.Interfaces;

namespace Infrastructure.Data.Models.Games.Stats;

public abstract class GameStats : IResetable
{
    public int GamesCount { get; set; }
    public int WinsCount { get; set; }

    public float WinRate
    {
        get => GamesCount != 0 ? (float)WinsCount / GamesCount : 0;
        private set { }
    }

    public ulong UserId { get; set; }
    public User User { get; set; } = null!;

    public ulong GuildSettingsId { get; set; }
    public GuildSettings GuildSettings { get; set; } = null!;


    public virtual void Reset()
    {
        GamesCount = 0;
        WinsCount = 0;
    }
}