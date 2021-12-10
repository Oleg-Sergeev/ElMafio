using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;



public abstract class RolesGroup : GameRole
{
    protected RolesGroup(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int voteTime) : base(player, options, voteTime)
    {
    }
}

public abstract class RolesGroup<T> : RolesGroup where T : GameRole
{
    public IList<T> Roles { get; }

    public override bool IsAlive => Roles.Any(r => r.IsAlive);

    public override bool BlockedByHooker => Roles.All(r => r.BlockedByHooker);


    public RolesGroup(IList<T> roles, IOptionsSnapshot<GameRoleData> options, int voteTime) : base(roles[0].Player, options, voteTime)
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
