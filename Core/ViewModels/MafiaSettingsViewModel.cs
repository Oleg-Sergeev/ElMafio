namespace Core.ViewModels;

public class MafiaSettingsViewModel
{
    public ulong? CategoryChannelId { get; init; }


    public ulong? GeneralTextChannelId { get; init; }

    public ulong? MurdersTextChannelId { get; init; }

    public ulong? WatchersTextChannelId { get; init; }


    public ulong? GeneralVoiceChannelId { get; init; }

    public ulong? MurdersVoiceChannelId { get; init; }

    public ulong? WatchersVoiceChannelId { get; init; }


    public ulong? MafiaRoleId { get; init; }

    public ulong? WatcherRoleId { get; init; }


    public bool? ClearChannelsOnStart { get; init; }
}
