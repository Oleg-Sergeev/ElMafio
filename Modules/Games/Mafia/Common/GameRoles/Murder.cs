using Discord;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;


public class Murder : GameRole, IKiller
{
    public int KillsCount { get; set; }

    public int MovesCount { get; set; }

    public IGuildUser? KilledPlayer { get; protected set; }


    public Murder(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }


    public override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);

        if (IsNight)
            KilledPlayer = choice;
    }

    public override void UpdateStats(MafiaStats stats, Winner winner)
    {
        base.UpdateStats(stats, winner);

        stats.BlacksGamesCount++;

        if (winner.Role is MurdersGroup or Murder)
            stats.BlacksWinsCount++;
    }
}
