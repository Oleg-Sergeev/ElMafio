using System;
using Core.Common.Data;
using Fergun.Interactive;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Services;

namespace Modules.Games.Mafia.Common.Data;

public class MafiaContext
{
    public MafiaGuildData GuildData { get; }

    public MafiaRolesData RolesData { get; }

    public MafiaData MafiaData { get; }

    public MafiaSettings Settings { get; }

    public MafiaSettingsTemplate SettingsTemplate { get; }

    public DbSocketCommandContext CommandContext { get; }

    public InteractiveService Interactive { get; }


    public int VoteTime { get; }


    public MafiaContext(MafiaGuildData guildData, MafiaData mafiaData, MafiaSettings settings,
        DbSocketCommandContext commandContext, InteractiveService interactive)
    {
        if (settings.CurrentTemplate is null)
            throw new InvalidOperationException($"Mafia settings template cannot be null when creating a {nameof(MafiaContext)}. Parameter: {nameof(settings.CurrentTemplate)}");

        RolesData = new();

        Settings = settings;
        SettingsTemplate = settings.CurrentTemplate;
        VoteTime = settings.CurrentTemplate.GameSubSettings.VoteTime;

        GuildData = guildData;
        MafiaData = mafiaData;
        CommandContext = commandContext;
        Interactive = interactive;
    }
}
