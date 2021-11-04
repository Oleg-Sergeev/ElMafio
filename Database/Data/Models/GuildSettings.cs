using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models
{
    public class GuildSettings
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public string Prefix { get; set; } = null!;

        public ulong? RoleMuteId { get; set; }


        public MafiaSettings MafiaSettings { get; set; } = null!;

        public RussianRouletteSettings RussianRouletteSettings { get; set; } = null!;
    }
}
