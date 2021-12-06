using System.Collections.Generic;
using Core.Common;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public class MurdersGroup : RolesGroup<Murder>
{
    public MurdersGroup(IList<Murder> roles, IOptionsSnapshot<GameRoleData> options, int voteTime) : base(roles, options, voteTime)
    {
    }
}
