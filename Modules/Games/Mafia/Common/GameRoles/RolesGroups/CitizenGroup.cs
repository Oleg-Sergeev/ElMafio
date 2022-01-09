using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public class CitizenGroup : RolesGroup
{
    public CitizenGroup(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles, options)
    {
    }


    public override async Task<VoteGroup> VoteManyAsync(MafiaContext context, CancellationToken token, IUserMessage? voteMessage = null)
    {
        var votes = new Dictionary<IGuildUser, Vote>();

        foreach (var role in Roles)
        {
            var vote = await role.VoteAsync(context, token, voteMessage);

            votes[vote.VotedRole.Player] = vote;
        }

        var voteGroup = new VoteGroup(this, votes);

        return voteGroup;
    }
}
