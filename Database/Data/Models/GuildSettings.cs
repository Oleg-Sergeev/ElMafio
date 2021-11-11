using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Data.Models.Games.Settings;

namespace Infrastructure.Data.Models;

public class GuildSettings
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public string Prefix { get; set; } = null!;

    public ulong? RoleMuteId { get; set; }

    public ulong? LogChannelId { get; set; }

    public MafiaSettings MafiaSettings { get; set; } = null!;

    public RussianRouletteSettings RussianRouletteSettings { get; set; } = null!;
}