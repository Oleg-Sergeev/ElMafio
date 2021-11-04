namespace Infrastructure.Data.Models
{
    public class MafiaSettings
    {
        public int Id { get; set; }


        public int MafiaKoefficient { get; set; }

        public bool IsRatingGame { get; set; }

        public bool RenameUsers { get; set; }


        public ulong GuildSettingsId { get; set; }
        public GuildSettings GuildSettings { get; set; } = null!;
    }
}
