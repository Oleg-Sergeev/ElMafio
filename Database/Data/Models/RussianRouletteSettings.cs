namespace Infrastructure.Data.Models
{
    public class RussianRouletteSettings
    {
        public int Id { get; set; }

        public string UnicodeSmileKilled { get; set; } = null!;
        public string UnicodeSmileSurvived { get; set; } = null!;

        public string? CustomSmileKilled { get; set; }
        public string? CustomSmileSurvived { get; set; }

        public ulong GuildSettingsId { get; set; }
        public GuildSettings GuildSettings { get; set; } = null!;
    }
}
