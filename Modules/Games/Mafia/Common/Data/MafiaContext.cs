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

    public DbSocketCommandContext CommandContext { get; }

    public InteractiveService Interactive { get; }


    public int VoteTime { get; }


    public MafiaContext(MafiaGuildData guildData, MafiaData mafiaData, MafiaSettings settings,
        DbSocketCommandContext commandContext, InteractiveService interactive)
    {
        RolesData = new();

        VoteTime = settings.Current.GameSubSettings.VoteTime;

        GuildData = guildData;
        MafiaData = mafiaData;
        Settings = settings;
        CommandContext = commandContext;
        Interactive = interactive;
    }
}
