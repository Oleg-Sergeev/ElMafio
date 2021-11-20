using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Innocent : GameRole
{
    public Innocent(IGuildUser player, IOptionsMonitor<GameRoleData> options, int voteTime, bool canDoMove) : base(player, options, voteTime)
    {
        CanDoMove = canDoMove;
    }


    public override IEnumerable<IGuildUser> GetExceptList()
    {
        var except = new List<IGuildUser>();

        if (LastMove is not null)
            except.Add(LastMove);

        return except;
    }
}
