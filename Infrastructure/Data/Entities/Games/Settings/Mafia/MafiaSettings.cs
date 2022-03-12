using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Entities.Games.Settings.Mafia;

public class MafiaSettings : GameSettings
{
    public static readonly MafiaSettings Empty = new();


    [ForeignKey($"{nameof(MafiaSettingsTemplate)}Id")]
    public int? CurrentTemplateId { get; set; }


    public ulong? CategoryChannelId { get; set; }


    public ulong? GeneralTextChannelId { get; set; }

    public ulong? MurdersTextChannelId { get; set; }

    public ulong? SpectatorsTextChannelId { get; set; }


    public ulong? GeneralVoiceChannelId { get; set; }

    public ulong? MurdersVoiceChannelId { get; set; }

    public ulong? SpectatorsVoiceChannelId { get; set; }


    public ulong? MafiaRoleId { get; set; }

    public ulong? WatcherRoleId { get; set; }


    public bool ClearChannelsOnStart { get; set; }

    public bool DisbandPartyAfterGameEnd { get; set; }



    public List<MafiaSettingsTemplate> MafiaSettingsTemplates { get; private set; } = new();


    [ForeignKey(nameof(CurrentTemplateId))]
    public MafiaSettingsTemplate? CurrentTemplate { get; set; }
}