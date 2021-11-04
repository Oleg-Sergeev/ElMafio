namespace Infrastructure.Data.Models
{
    public abstract class GameStats : IResetableStat
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

        public ulong GuildId { get; set; }
        public GuildSettings Guild { get; set; } = null!;


        public virtual void Reset()
        {
            GamesCount = 0;
            WinsCount = 0;
        }
    }
}
