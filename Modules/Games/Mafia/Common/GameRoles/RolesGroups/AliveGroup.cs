using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public class AliveGroup : RolesGroup<GameRole>
{
    public AliveGroup(IList<GameRole> roles, IOptionsMonitor<GameRoleData> options, int voteTime) : base(roles, options, voteTime)
    {

    }
}
