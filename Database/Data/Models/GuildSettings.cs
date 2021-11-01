using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Data.Models
{
    public class GuildSettings
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public string Prefix { get; set; }
    }
}
