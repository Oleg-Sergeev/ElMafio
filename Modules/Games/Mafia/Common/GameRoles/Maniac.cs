using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Maniac : Neutral, IKiller
{
    public int KillsCount { get; set; }

    public int MovesCount { get; set; }

    public IGuildUser? KilledPlayer { get; protected set; }


    public Maniac(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }


    public override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);

        if (IsNight)
            KilledPlayer = choice;
    }
}
