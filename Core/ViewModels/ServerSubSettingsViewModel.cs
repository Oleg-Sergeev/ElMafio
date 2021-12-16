namespace Core.ViewModels;

public class ServerSubSettingsViewModel
{
    public bool? RemoveRolesFromUsers { get; init; }

    public bool? RenameUsers { get; init; }

    public bool? ReplyMessagesOnSetupError { get; init; }

    public bool? AbortGameWhenError { get; init; }

    public bool? SendWelcomeMessage { get; init; }

    public bool? MentionPlayersOnGameStart { get; init; }
}
