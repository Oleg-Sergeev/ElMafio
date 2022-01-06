using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;


public abstract class RolesGroup : GameRole
{
    public IReadOnlyList<GameRole> Roles { get; }

    public override bool IsAlive => Roles.Any(r => r.IsAlive);

    public override bool BlockedByHooker => Roles.All(r => r.BlockedByHooker);


    public RolesGroup(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles[0].Player, options)
    {
        Roles = roles;
    }


    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        foreach (var role in Roles)
            role.ProcessMove(selectedPlayer, isSkip);
    }

    public virtual async Task<VotingResult> VoteManyAsync(MafiaContext context, CancellationToken token, IUserMessage? voteMessage = null)
    {
        var votes = new Dictionary<IGuildUser, Vote>();

        foreach (var role in Roles)
        {
            var vote = await role.VoteAsync(context, token, voteMessage);

            votes[vote.VotedRole.Player] = vote;
        }

        var votingResult = new VotingResult(this, votes);

        return votingResult;
    }
}
