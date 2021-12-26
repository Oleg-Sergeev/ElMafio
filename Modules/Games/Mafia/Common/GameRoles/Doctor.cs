using System.Collections.Generic;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Doctor : Innocent, IHealer
{
    public IGuildUser? HealedPlayer { get; protected set; }

    protected int SelfHealsCount { get; set; }


    public Doctor(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int selfHealsCount) : base(player, options)
    {
        SelfHealsCount = selfHealsCount;
    }


    public override IEnumerable<IGuildUser> GetExceptList()
    {
        var except = new List<IGuildUser>();

        if (HealedPlayer is not null)
            except.Add(HealedPlayer);

        if (SelfHealsCount == 0)
            except.Add(Player);

        return except;
    }

    public override void SetPhase(bool isNight)
    {
        base.SetPhase(isNight);

        if (isNight)
            HealedPlayer = null;
    }

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);


        if (IsNight)
        {
            HealedPlayer = !isSkip ? selectedPlayer : null;

            if (HealedPlayer == Player)
                SelfHealsCount--;
        }
    }
}
