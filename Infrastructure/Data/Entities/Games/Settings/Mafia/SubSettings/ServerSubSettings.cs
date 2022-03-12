namespace Infrastructure.Data.Entities.Games.Settings.Mafia.SubSettings;

public record ServerSubSettings
{
    public int Id { get; set; }

    public int MafiaSettingsTemplateId { get; set; }


    public bool RemoveRolesFromUsers { get; set; }

    public bool RenameUsers { get; set; }

    public bool ReplyMessagesOnSetupError { get; set; }

    public bool AbortGameWhenError { get; set; }

    public bool SendWelcomeMessage { get; set; }

    public bool MentionPlayersOnGameStart { get; set; }



    public ServerSubSettings()
    {
        RemoveRolesFromUsers = true;

        RenameUsers = true;

        SendWelcomeMessage = true;

        ReplyMessagesOnSetupError = true;

        AbortGameWhenError = true;

        MentionPlayersOnGameStart = true;
    }
}
