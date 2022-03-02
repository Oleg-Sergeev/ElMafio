using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Settings.Mafia;

namespace Infrastructure.Data.Models.Guild;

public class GuildSettings
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }


    public string Prefix { get; set; }

    public ulong? LogChannelId { get; set; }

    public DebugMode DebugMode { get; set; }

    public MafiaSettings MafiaSettings { get; set; } = null!;

    public RussianRouletteSettings RussianRouletteSettings { get; set; } = null!;


    public GuildSettings()
    {
        DebugMode = DebugMode.Off;

        Prefix = "/";
    }
}