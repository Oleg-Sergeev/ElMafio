namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public record class ServerSubSettings
{
    public bool RemoveRolesFromUsers { get; init; }

    public bool RenameUsers { get; init; }

    public bool ReplyMessagesOnSetupError { get; init; }

    public bool AbortGameWhenError { get; init; }

    public bool SendWelcomeMessage { get; init; }


    public ulong? CategoryChannelId { get; init; }


    public ulong? GeneralTextChannelId { get; init; }

    public ulong? GeneralVoiceChannelId { get; init; }


    public ulong? MurdersTextChannelId { get; init; }

    public ulong? MurdersVoiceChannelId { get; init; }


    public ulong? WatchersTextChannelId { get; init; }

    public ulong? WatchersVoiceChannelId { get; init; }


    public ulong? MafiaRoleId { get; init; }

    public ulong? WatcherRoleId { get; init; }


    public ServerSubSettings()
    {
        RemoveRolesFromUsers = true;

        RenameUsers = true;

        SendWelcomeMessage = true;

        ReplyMessagesOnSetupError = true;

        AbortGameWhenError = true;
    }
}
