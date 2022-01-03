using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public class MafiaSettings : GameSettings
{
    public const string DefaultTemplateName = "_Current";


    public static readonly MafiaSettings Empty = new();


    public string CurrentTemplateName { get; set; }


    public ulong? CategoryChannelId { get; set; }


    public ulong? GeneralTextChannelId { get; set; }

    public ulong? GeneralVoiceChannelId { get; set; }


    public ulong? MurdersTextChannelId { get; set; }

    public ulong? MurdersVoiceChannelId { get; set; }


    public ulong? WatchersTextChannelId { get; set; }

    public ulong? WatchersVoiceChannelId { get; set; }


    public ulong? MafiaRoleId { get; set; }

    public ulong? WatcherRoleId { get; set; }


    public bool ClearChannelsOnStart { get; set; }



    public List<SettingsTemplate> SettingsTemplates { get; private set; } = null!;


    [NotMapped]
    public SettingsTemplate Current { get; set; }


    public MafiaSettings()
    {
        CurrentTemplateName = "_Current";

        Current = new(CurrentTemplateName);
    }
}