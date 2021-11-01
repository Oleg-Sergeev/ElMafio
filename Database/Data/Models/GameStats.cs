namespace Database.Data.Models
{
    public abstract class GameStats
    {
        public int GamesCount { get; set; }
        public int WinsCount { get; set; }

        public float WinRate
        {
            get => GamesCount != 0 ? (float)WinsCount / GamesCount : 0;
            private set { }
        }

        public ulong UserId { get; set; }
        public User User { get; set; }

        public ulong GuildId { get; set; }
        public GuildSettings Guild { get; set; }
    }
}
