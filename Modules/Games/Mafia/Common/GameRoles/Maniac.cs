using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Maniac : Neutral, IKiller
{
    public IGuildUser? KilledPlayer { get; protected set; }


    public Maniac(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }

    public override void SetPhase(bool isNight)
    {
        base.SetPhase(isNight);

        if (isNight)
            KilledPlayer = null;
    }
    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        if (IsNight)
            KilledPlayer = !isSkip ? selectedPlayer : null;
    }
}
