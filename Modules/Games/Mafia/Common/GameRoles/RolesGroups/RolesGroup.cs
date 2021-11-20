using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public abstract class RolesGroup<T> : GameRole where T : GameRole
{
    public IList<T> Roles { get; }

    public override bool IsAlive => Roles.Any(r => r.IsAlive);


    public RolesGroup(IList<T> roles, IOptionsMonitor<GameRoleData> options, int voteTime) : base(roles[0].Player, options, voteTime)
    {
        Roles = roles;
    }


    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        foreach (var role in Roles)
            role.ProcessMove(selectedPlayer, isSkip);
    }
}
