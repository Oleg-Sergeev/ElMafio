using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Settings.Mafia;

namespace Infrastructure.Data.Models;

public class GuildSettings
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }


    public string Prefix { get; set; } = string.Empty;

    public ulong? RoleMuteId { get; set; } // TODO: Remove

    public ulong? LogChannelId { get; set; }


    public MafiaSettings MafiaSettings { get; set; } = null!;

    public RussianRouletteSettings RussianRouletteSettings { get; set; } = null!;
}