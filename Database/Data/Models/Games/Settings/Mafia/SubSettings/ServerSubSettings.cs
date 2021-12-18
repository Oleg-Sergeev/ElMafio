﻿namespace Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;

public record class ServerSubSettings
{
    public bool RemoveRolesFromUsers { get; init; }

    public bool RenameUsers { get; init; }

    public bool ReplyMessagesOnSetupError { get; init; }

    public bool AbortGameWhenError { get; init; }

    public bool SendWelcomeMessage { get; init; }

    public bool MentionPlayersOnGameStart { get; init; }



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